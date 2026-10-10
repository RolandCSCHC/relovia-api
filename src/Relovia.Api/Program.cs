using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Errors;
using Relovia.Api.Common.Health;
using Relovia.Api.Common.Persistence;

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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReloviaDbContext>("database");

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiValidation.InvalidModelState;
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.Run();
