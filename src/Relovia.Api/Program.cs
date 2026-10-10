using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Errors;
using Relovia.Api.Common.Health;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Common.Security;
using Relovia.Api.Features.Identity;
using Scalar.AspNetCore;

// Some container hosts, including Render, inject PORT. The image default is 8080.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", port);
}

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
    });
}

var connectionString = builder.Configuration.GetConnectionString("Relovia")
    ?? throw new InvalidOperationException("Connection string 'Relovia' is not configured.");

var webSettings = builder.Configuration.GetSection(WebSettings.SectionName).Get<WebSettings>() ?? new WebSettings();
var origins = webSettings.Origins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.Ordinal)
    .ToArray();
if (origins.Length == 0)
{
    throw new InvalidOperationException("Web:Origins must list the Angular origin.");
}

var requestTokenCookieDomain = string.IsNullOrWhiteSpace(webSettings.RequestTokenCookieDomain)
    ? null
    : webSettings.RequestTokenCookieDomain.Trim();
builder.Services.AddSingleton(new WebSettings
{
    Origins = origins,
    RequestTokenCookieDomain = requestTokenCookieDomain,
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<ReloviaDbContext>(options =>
    options
        .UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(18, 0))
        .UseSnakeCaseNamingConvention());

// Protects the antiforgery cookie. The session cookie is a random token; Postgres stores only its hash.
builder.Services.AddDataProtection()
    .SetApplicationName("Relovia")
    .PersistKeysToDbContext<ReloviaDbContext>();

builder.Services.AddOptions<SessionSettings>()
    .Bind(builder.Configuration.GetSection(SessionSettings.SectionName))
    .Validate(settings => settings.SlidingLifetime > TimeSpan.Zero, "Session:SlidingLifetime must be positive.")
    .Validate(
        settings => settings.AbsoluteLifetime >= settings.SlidingLifetime,
        "Session:AbsoluteLifetime must cover the sliding lifetime.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAuthSessionStore, AuthSessionStore>();

var securePolicy = CookieSecurity.SecurePolicy(builder.Environment);
builder.Services.AddAuthentication(options =>
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
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AntiforgeryCookies.HeaderName;
    options.Cookie.Name = AntiforgeryCookies.SecretCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = securePolicy;
    options.Cookie.IsEssential = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebSettings.CorsPolicy, policy =>
    {
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReloviaDbContext>("database");

builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new AuthorizeFilter());
        options.Filters.Add<ValidateAntiforgeryFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiValidation.InvalidModelState;
    });

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Relovia";
            document.Info.Version = "v1";
            return Task.CompletedTask;
        });
    });
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
}

app.UseRouting();
app.UseCors(WebSettings.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryCookieMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
