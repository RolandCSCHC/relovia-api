using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Relovia.Api.Common.Security;

namespace Relovia.Api.Features.Identity;

public sealed class SessionAuthenticationHandler : SignInAuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuthSessionStore _sessions;
    private readonly IHostEnvironment _environment;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthSessionStore sessions,
        IHostEnvironment environment)
        : base(options, logger, encoder)
    {
        _sessions = sessions;
        _environment = environment;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionAuthenticationDefaults.CookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var userId = await _sessions.AuthenticateAsync(token, Context.RequestAborted);
        if (userId is null)
        {
            Response.Cookies.Delete(SessionAuthenticationDefaults.CookieName, SessionCookieOptions(expires: null));
            return AuthenticateResult.Fail("Session is not valid.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
            Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var userId = RequireUserId(user);
        var issued = await _sessions.IssueAsync(userId, Context.RequestAborted);
        Response.Cookies.Append(
            SessionAuthenticationDefaults.CookieName,
            issued.Token,
            SessionCookieOptions(issued.AbsoluteExpiresAt));
    }

    protected override async Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        if (Request.Cookies.TryGetValue(SessionAuthenticationDefaults.CookieName, out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            await _sessions.RevokeAsync(token, Context.RequestAborted);
        }

        Response.Cookies.Delete(SessionAuthenticationDefaults.CookieName, SessionCookieOptions(expires: null));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    private CookieOptions SessionCookieOptions(DateTimeOffset? expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = CookieSecurity.IsSecure(_environment, Request),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            Expires = expires,
        };
    }

    private static Guid RequireUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException("The session principal must include a user id.");
        }

        return userId;
    }
}
