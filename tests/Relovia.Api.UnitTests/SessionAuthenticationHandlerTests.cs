using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.UnitTests;

public sealed class SessionAuthenticationHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("0199c8e0-7b2a-7c3d-8e4f-1a2b3c4d5e6f");

    [Fact]
    public async Task Missing_cookie_is_anonymous()
    {
        var (auth, context, _) = CreateAuth(Environments.Development);

        var result = await auth.AuthenticateAsync(context, SessionAuthenticationDefaults.Scheme);

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task Valid_cookie_authenticates_the_user()
    {
        var (auth, context, store) = CreateAuth(Environments.Development);
        store.UserId = UserId;
        context.Request.Headers.Cookie = $"{SessionAuthenticationDefaults.CookieName}=good";

        var result = await auth.AuthenticateAsync(context, SessionAuthenticationDefaults.Scheme);

        Assert.True(result.Succeeded);
        Assert.Equal(UserId.ToString(), result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public async Task Invalid_cookie_fails_and_is_cleared()
    {
        var (auth, context, _) = CreateAuth(Environments.Development);
        context.Request.Headers.Cookie = $"{SessionAuthenticationDefaults.CookieName}=bad";

        var result = await auth.AuthenticateAsync(context, SessionAuthenticationDefaults.Scheme);

        Assert.False(result.Succeeded);
        var header = CookieHeader(context, SessionAuthenticationDefaults.CookieName);
        Assert.Contains("expires=", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sign_in_sets_a_host_only_httponly_lax_cookie()
    {
        var (auth, context, store) = CreateAuth(Environments.Development);
        context.Request.Scheme = "http";
        store.Next = new IssuedSession("issued-token", new DateTimeOffset(2026, 11, 9, 15, 0, 0, TimeSpan.Zero));

        await auth.SignInAsync(context, SessionAuthenticationDefaults.Scheme, Principal(), properties: null);

        Assert.Equal(UserId, store.IssuedUserId);
        var header = CookieHeader(context, SessionAuthenticationDefaults.CookieName);
        Assert.Contains("issued-token", header, StringComparison.Ordinal);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Production_sign_in_marks_the_cookie_secure()
    {
        var (auth, context, store) = CreateAuth(Environments.Production);
        context.Request.Scheme = "http";
        store.Next = new IssuedSession("issued-token", DateTimeOffset.UtcNow.AddDays(30));

        await auth.SignInAsync(context, SessionAuthenticationDefaults.Scheme, Principal(), properties: null);

        var header = CookieHeader(context, SessionAuthenticationDefaults.CookieName);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sign_out_revokes_the_session_and_clears_the_cookie()
    {
        var (auth, context, store) = CreateAuth(Environments.Development);
        context.Request.Headers.Cookie = $"{SessionAuthenticationDefaults.CookieName}=good";

        await auth.SignOutAsync(context, SessionAuthenticationDefaults.Scheme, properties: null);

        Assert.Equal(["good"], store.Revoked);
        var header = CookieHeader(context, SessionAuthenticationDefaults.CookieName);
        Assert.Contains("expires=", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sign_in_without_a_user_id_is_rejected()
    {
        var (auth, context, _) = CreateAuth(Environments.Development);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            auth.SignInAsync(context, SessionAuthenticationDefaults.Scheme, principal, properties: null));
    }

    [Fact]
    public async Task Challenge_returns_401_and_forbidden_returns_403()
    {
        var (auth, context, _) = CreateAuth(Environments.Development);

        await auth.ChallengeAsync(context, SessionAuthenticationDefaults.Scheme, properties: null);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        context.Response.StatusCode = StatusCodes.Status200OK;
        await auth.ForbidAsync(context, SessionAuthenticationDefaults.Scheme, properties: null);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private static ClaimsPrincipal Principal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            SessionAuthenticationDefaults.Scheme));
    }

    private static string CookieHeader(HttpContext context, string name)
    {
        return Assert.Single(
            context.Response.Headers.SetCookie,
            value => value!.StartsWith(name + "=", StringComparison.Ordinal))!;
    }

    private static (IAuthenticationService Auth, DefaultHttpContext Context, FakeSessions Store) CreateAuth(
        string environmentName)
    {
        var store = new FakeSessions();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestEnvironment(environmentName));
        services.AddSingleton<IAuthSessionStore>(store);
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = SessionAuthenticationDefaults.Scheme;
                options.DefaultAuthenticateScheme = SessionAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = SessionAuthenticationDefaults.Scheme;
                options.DefaultSignInScheme = SessionAuthenticationDefaults.Scheme;
                options.DefaultSignOutScheme = SessionAuthenticationDefaults.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
                SessionAuthenticationDefaults.Scheme,
                _ => { });

        var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 5080);
        return (provider.GetRequiredService<IAuthenticationService>(), context, store);
    }

    private sealed class FakeSessions : IAuthSessionStore
    {
        public IssuedSession Next { get; set; } = new("issued-token", DateTimeOffset.UtcNow.AddDays(30));

        public Guid? UserId { get; set; }

        public Guid IssuedUserId { get; private set; }

        public List<string> Revoked { get; } = [];

        public Task<IssuedSession> IssueAsync(Guid userId, CancellationToken cancellationToken)
        {
            IssuedUserId = userId;
            return Task.FromResult(Next);
        }

        public Task<Guid?> AuthenticateAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(token == "good" ? UserId : null);
        }

        public Task RevokeAsync(string token, CancellationToken cancellationToken)
        {
            Revoked.Add(token);
            return Task.CompletedTask;
        }
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Relovia.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
