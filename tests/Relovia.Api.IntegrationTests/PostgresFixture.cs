using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Common.Persistence;
using Testcontainers.PostgreSql;

namespace Relovia.Api.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    // PostgreSQL 18 matches local Compose and Neon. Leave the image data directory alone:
    // this major version stores data under /var/lib/postgresql, not /var/lib/postgresql/data.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("relovia")
        .WithUsername("relovia")
        .WithPassword("relovia")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    public ReloviaDbContext CreateContext()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            SslMode = SslMode.Disable,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<ReloviaDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(18, 0))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ReloviaDbContext(options);
    }
}
