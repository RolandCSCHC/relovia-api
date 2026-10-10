namespace Relovia.Api.Common.Security;

public static class AntiforgeryCookies
{
    public const string HeaderName = "X-XSRF-TOKEN";

    public const string RequestTokenCookieName = "XSRF-TOKEN";

    public const string SecretCookieName = "relovia_antiforgery";
}
