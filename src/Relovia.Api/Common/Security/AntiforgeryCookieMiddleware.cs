using Microsoft.AspNetCore.Antiforgery;

namespace Relovia.Api.Common.Security;

public sealed class AntiforgeryCookieMiddleware(
    RequestDelegate next,
    IAntiforgery antiforgery,
    IHostEnvironment environment,
    WebSettings web)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldIssue(context.Request))
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            if (!string.IsNullOrEmpty(tokens.RequestToken))
            {
                context.Response.Cookies.Append(
                    AntiforgeryCookies.RequestTokenCookieName,
                    tokens.RequestToken,
                    new CookieOptions
                    {
                        HttpOnly = false,
                        Secure = CookieSecurity.IsSecure(environment, context.Request),
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        IsEssential = true,
                        Domain = web.RequestTokenCookieDomain,
                    });
            }
        }

        await next(context);
    }

    private static bool ShouldIssue(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        return !request.Path.StartsWithSegments("/health");
    }
}
