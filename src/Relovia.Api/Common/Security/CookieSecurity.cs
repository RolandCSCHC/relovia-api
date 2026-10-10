namespace Relovia.Api.Common.Security;

public static class CookieSecurity
{
    public static CookieSecurePolicy SecurePolicy(IHostEnvironment environment)
    {
        return environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    }

    public static bool IsSecure(IHostEnvironment environment, HttpRequest request)
    {
        return !environment.IsDevelopment() || request.IsHttps;
    }
}
