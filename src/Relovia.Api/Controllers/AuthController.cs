using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Common.Security;
using Relovia.Api.Features.Access;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ReloviaDbContext db,
    WebSettings web,
    AccessPolicy access,
    IAuthenticationSchemeProvider schemes) : ControllerBase
{
    [HttpGet("google")]
    [AllowAnonymous]
    [EndpointName("signInWithGoogle")]
    [EndpointSummary("Starts Google sign-in.")]
    public async Task<IActionResult> SignInWithGoogle()
    {
        var scheme = await schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme);
        if (scheme is null)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Google sign-in is not configured.");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = SignInRedirect.Success(web.Origins[0]),
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("me")]
    // Signed-in people without access still need to learn that, so the global approval
    // requirement is skipped here. The action answers 401 itself when nobody is signed in.
    [AllowAnonymous]
    [EndpointName("getCurrentUser")]
    [EndpointSummary("Returns the signed-in user and whether they can use the app yet.")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            return Unauthorized();
        }

        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.PreferredCurrency,
            user.PreferredLanguage,
            access.HasAccess(user.Email)));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EndpointName("logout")]
    [EndpointSummary("Revokes the current session and clears the session cookie.")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(SessionAuthenticationDefaults.Scheme);
        return NoContent();
    }
}

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string PreferredCurrency,
    string PreferredLanguage,
    bool HasAccess);
