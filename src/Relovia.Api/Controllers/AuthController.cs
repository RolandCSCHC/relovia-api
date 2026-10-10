using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("logout")]
    [AllowAnonymous]
    [EndpointSummary("Revokes the current session and clears the session cookie.")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(SessionAuthenticationDefaults.Scheme);
        return NoContent();
    }
}
