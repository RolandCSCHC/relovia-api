using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Relovia.Api.Common.Security;

namespace Relovia.Api.Features.Identity;

public static class GoogleSignIn
{
    public static async Task OnTicketReceived(TicketReceivedContext context)
    {
        if (context.Properties is not null)
        {
            DiscardProviderTokens(context.Properties);
        }
        var origin = AppOrigin(context.HttpContext);
        var read = GoogleIdentityReader.Read(context.Principal);
        if (!read.Succeeded || read.Identity is null)
        {
            context.HandleResponse();
            context.Response.Redirect(SignInRedirect.Failure(origin, read.ErrorCode));
            return;
        }

        var accounts = context.HttpContext.RequestServices.GetRequiredService<ExternalAccountService>();
        var result = await accounts.LinkGoogleAsync(read.Identity, context.HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            context.HandleResponse();
            context.Response.Redirect(SignInRedirect.Failure(origin, result.ErrorCode ?? SignInErrors.GoogleFailed));
            return;
        }

        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString())],
            SessionAuthenticationDefaults.Scheme));
        context.ReturnUri = SignInRedirect.Success(origin);
    }

    public static Task OnRemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();
        var error = context.Failure?.Data.Contains("error") == true
            ? context.Failure.Data["error"]?.ToString()
            : null;
        var code = string.Equals(error, "access_denied", StringComparison.Ordinal)
            ? SignInErrors.GoogleDenied
            : SignInErrors.GoogleFailed;
        context.Response.Redirect(SignInRedirect.Failure(AppOrigin(context.HttpContext), code));
        return Task.CompletedTask;
    }

    public static void DiscardProviderTokens(AuthenticationProperties properties)
    {
        properties.StoreTokens([]);
    }

    private static string AppOrigin(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<WebSettings>().Origins[0];
    }
}
