using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

// Some container hosts, including Render, inject PORT. The image default is 8080.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", port);
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Relovia")
    ?? throw new InvalidOperationException("Connection string 'Relovia' is not configured.");

builder.Services.AddDbContext<ReloviaDbContext>(options =>
    options
        .UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(18, 0))
        .UseSnakeCaseNamingConvention());

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
