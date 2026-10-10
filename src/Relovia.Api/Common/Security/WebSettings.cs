namespace Relovia.Api.Common.Security;

public sealed class WebSettings
{
    public const string SectionName = "Web";

    public const string CorsPolicy = "web";

    public string[] Origins { get; set; } = [];

    /// <summary>
    /// Host of the request-token cookie. Leave empty so the cookie stays on the API host.
    /// Localhost ignores ports, so the Angular app on another port can read it.
    /// Set a parent domain, such as <c>.example.com</c>, only when the app and API are sibling subdomains.
    /// </summary>
    public string? RequestTokenCookieDomain { get; set; }
}
