using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Relovia.Api.Common.Persistence;

public sealed class ReloviaDbContextFactory : IDesignTimeDbContextFactory<ReloviaDbContext>
{
    public ReloviaDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(FindContentRoot())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Relovia")
            ?? throw new InvalidOperationException("Connection string 'Relovia' is not configured.");

        var options = new DbContextOptionsBuilder<ReloviaDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(18, 0))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ReloviaDbContext(options);
    }

    private static string FindContentRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var hasSettings = File.Exists(Path.Combine(directory.FullName, "appsettings.json"));
            var hasProject = File.Exists(Path.Combine(directory.FullName, "Relovia.Api.csproj"));
            if (hasSettings && hasProject)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
