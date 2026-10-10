using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Relovia.Api.Common.Security;

namespace Relovia.Api.UnitTests;

public sealed class AntiforgeryCookieTests
{
    [Fact]
    public async Task Get_sets_a_readable_request_token_and_an_httponly_secret()
    {
        var (middleware, context) = CreateMiddleware(new WebSettings { Origins = ["http://localhost:4200"] });
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.Scheme = "http";

        await middleware.InvokeAsync(context);

        var cookies = context.Response.Headers.SetCookie.Select(value => value!).ToArray();
        var requestToken = Assert.Single(cookies, value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        var secret = Assert.Single(cookies, value => value.StartsWith("relovia_antiforgery=", StringComparison.Ordinal));
        Assert.Contains("samesite=lax", requestToken, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", requestToken, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", requestToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", secret, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Health_check_does_not_receive_antiforgery_cookies()
    {
        var (middleware, context) = CreateMiddleware(new WebSettings { Origins = ["http://localhost:4200"] });
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/health";

        await middleware.InvokeAsync(context);

        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Request_token_domain_is_used_when_configured()
    {
        var web = new WebSettings
        {
            Origins = ["https://app.example.com"],
            RequestTokenCookieDomain = ".example.com",
        };
        var (middleware, context) = CreateMiddleware(web);
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";

        await middleware.InvokeAsync(context);

        var requestToken = Assert.Single(
            context.Response.Headers.SetCookie,
            value => value!.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))!;
        Assert.Contains("domain=.example.com", requestToken, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Header_matching_the_request_token_validates()
    {
        var provider = CreateServices();
        var antiforgery = provider.GetRequiredService<IAntiforgery>();
        var issued = IssueTokens(provider, antiforgery);

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Cookie = issued.SecretCookie;
        context.Request.Headers[AntiforgeryCookies.HeaderName] = issued.RequestToken;

        await antiforgery.ValidateRequestAsync(context);
    }

    [Fact]
    public async Task Missing_header_is_rejected()
    {
        var provider = CreateServices();
        var antiforgery = provider.GetRequiredService<IAntiforgery>();
        var issued = IssueTokens(provider, antiforgery);

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Cookie = issued.SecretCookie;

        await Assert.ThrowsAsync<AntiforgeryValidationException>(() => antiforgery.ValidateRequestAsync(context));
    }

    private static IssuedTokens IssueTokens(ServiceProvider provider, IAntiforgery antiforgery)
    {
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Get;
        var tokens = antiforgery.GetAndStoreTokens(context);
        return new IssuedTokens(SecretCookie(context), tokens.RequestToken!);
    }

    private sealed record IssuedTokens(string SecretCookie, string RequestToken);

    private static string SecretCookie(HttpContext context)
    {
        var header = Assert.Single(
            context.Response.Headers.SetCookie,
            value => value!.StartsWith(AntiforgeryCookies.SecretCookieName + "=", StringComparison.Ordinal))!;
        return header.Split(';', 2)[0];
    }

    private static (AntiforgeryCookieMiddleware Middleware, DefaultHttpContext Context) CreateMiddleware(WebSettings web)
    {
        var provider = CreateServices();
        var middleware = new AntiforgeryCookieMiddleware(
            _ => Task.CompletedTask,
            provider.GetRequiredService<IAntiforgery>(),
            provider.GetRequiredService<IHostEnvironment>(),
            web);
        var context = new DefaultHttpContext { RequestServices = provider };
        return (middleware, context);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryCookies.HeaderName;
            options.Cookie.Name = AntiforgeryCookies.SecretCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
        services.AddSingleton<IHostEnvironment>(new TestEnvironment());
        return services.BuildServiceProvider();
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Relovia.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
