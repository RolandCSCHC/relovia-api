using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Errors;
using Relovia.Api.Common.Health;
using Relovia.Api.Common.OpenApi;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Common.Security;
using Amazon.S3;
using Relovia.Api.Features.Access;
using Relovia.Api.Features.Applications;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.ExchangeRates;
using Relovia.Api.Features.Export;
using Relovia.Api.Features.Housing;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Notifications;
using Relovia.Api.Features.Places;
using Relovia.Api.Features.Relocations;
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
builder.Services.AddScoped<ExternalAccountService>();
builder.Services.AddOptions<AccessSettings>().Bind(builder.Configuration.GetSection(AccessSettings.SectionName));
builder.Services.AddOptions<EmailSettings>().Bind(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddSingleton<AccessPolicy>();
builder.Services.AddHttpClient<IInterestNotifier, ResendInterestNotifier>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Relovia/1.0 (+https://relovia.rolandschlosser.dev)");
});
builder.Services.AddHttpClient<INotificationEmailSender, ResendNotificationEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Relovia/1.0 (+https://relovia.rolandschlosser.dev)");
});
builder.Services.AddScoped<InterestService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GeoNamesCityListSource>(client =>
{
    client.BaseAddress = new Uri("https://download.geonames.org/");
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Relovia/1.0 (+https://relovia.rolandschlosser.dev)");
});
builder.Services.AddHttpClient<OpenErApiRateProvider>(client =>
{
    client.BaseAddress = new Uri("https://open.er-api.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Relovia/1.0 (+https://relovia.rolandschlosser.dev)");
});
builder.Services.AddScoped<IExchangeRateProvider>(provider => provider.GetRequiredService<OpenErApiRateProvider>());
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<ICityListSource>(provider => provider.GetRequiredService<GeoNamesCityListSource>());
builder.Services.AddScoped<CityCatalog>();
builder.Services.AddScoped<RelocationService>();
builder.Services.AddScoped<ChecklistService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<InventoryListService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<HousingService>();
builder.Services.AddScoped<JobApplicationService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReminderService>();
builder.Services.AddScoped<RelocationExportService>();
builder.Services.AddOptions<ReminderSettings>().Bind(builder.Configuration.GetSection(ReminderSettings.SectionName));
builder.Services.AddHostedService<ReminderHostedService>();
AddDocumentStorage(builder);

var securePolicy = CookieSecurity.SecurePolicy(builder.Environment);
var authentication = builder.Services.AddAuthentication(options =>
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

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/auth/google/callback";
        options.SignInScheme = SessionAuthenticationDefaults.Scheme;
        options.SaveTokens = false;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = securePolicy;
        options.CorrelationCookie.HttpOnly = true;
        options.ClaimActions.MapJsonKey(GoogleIdentityReader.SubjectClaim, "sub");
        options.ClaimActions.MapJsonKey(GoogleIdentityReader.EmailVerifiedClaim, "email_verified");
        options.ClaimActions.MapJsonKey(GoogleIdentityReader.PictureClaim, "picture");
        options.Events.OnTicketReceived = GoogleSignIn.OnTicketReceived;
        options.Events.OnRemoteFailure = GoogleSignIn.OnRemoteFailure;
    });
}
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AccessPolicies.Approved, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new ApprovedUserRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, ApprovedUserHandler>();
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
        // Every endpoint needs an approved user unless it says [AllowAnonymous].
        options.Filters.Add(new AuthorizeFilter(AccessPolicies.Approved));
        options.Filters.Add<ValidateAntiforgeryFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
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
        options.AddSchemaTransformer(StringEnumSchemaTransformer.TransformAsync);
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

if (!app.Environment.IsDevelopment() && app.Services.GetRequiredService<IObjectStorage>() is LocalObjectStorage)
{
    app.Logger.LogWarning(
        "Document storage is a local directory. Files there disappear when the host is replaced. Set Storage:Provider to S3 to keep them.");
}

app.Run();

static void AddDocumentStorage(WebApplicationBuilder builder)
{
    var storageSettings = builder.Configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>()
        ?? new StorageSettings();
    var provider = string.IsNullOrWhiteSpace(storageSettings.Provider) ? "Local" : storageSettings.Provider.Trim();
    if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
    {
        var s3 = storageSettings.S3;
        var missing = new List<string>();
        if (!Uri.TryCreate(s3.ServiceUrl, UriKind.Absolute, out var serviceUrl) || serviceUrl.Scheme != Uri.UriSchemeHttps)
        {
            missing.Add("Storage:S3:ServiceUrl");
        }

        if (string.IsNullOrWhiteSpace(s3.Region))
        {
            missing.Add("Storage:S3:Region");
        }

        if (string.IsNullOrWhiteSpace(s3.Bucket))
        {
            missing.Add("Storage:S3:Bucket");
        }

        if (string.IsNullOrWhiteSpace(s3.AccessKey))
        {
            missing.Add("Storage:S3:AccessKey");
        }

        if (string.IsNullOrWhiteSpace(s3.SecretKey))
        {
            missing.Add("Storage:S3:SecretKey");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Storage:Provider is S3 but these settings are missing or not an https URL: "
                + string.Join(", ", missing)
                + ".");
        }

        var configured = new S3StorageSettings
        {
            ServiceUrl = s3.ServiceUrl.Trim(),
            Region = s3.Region.Trim(),
            Bucket = s3.Bucket.Trim(),
            AccessKey = s3.AccessKey,
            SecretKey = s3.SecretKey,
        };
        builder.Services.AddSingleton(configured);
        builder.Services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = configured.ServiceUrl,
                AuthenticationRegion = configured.Region,
                ForcePathStyle = true,
            };
            return new AmazonS3Client(configured.AccessKey, configured.SecretKey, config);
        });
        builder.Services.AddSingleton<IObjectStorage, S3ObjectStorage>();
    }
    else if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
    {
        var root = storageSettings.Local.Root;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(builder.Environment.ContentRootPath, "data", "files");
        }
        else if (!Path.IsPathRooted(root))
        {
            root = Path.Combine(builder.Environment.ContentRootPath, root);
        }

        Directory.CreateDirectory(root);
        builder.Services.AddSingleton<IObjectStorage>(new LocalObjectStorage(root));
    }
    else
    {
        throw new InvalidOperationException("Storage:Provider must be Local or S3.");
    }

    builder.Services.AddScoped<DocumentService>();
}

public partial class Program;
