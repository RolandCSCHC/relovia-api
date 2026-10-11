using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Common.Persistence;

public sealed class ReloviaDbContext(DbContextOptions<ReloviaDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReloviaDbContext).Assembly);
    }

    private void ApplyTimestamps()
    {
        // timestamptz stores microseconds. Write that precision so a reload matches the saved value.
        var utcNow = TruncateToMicroseconds(DateTimeOffset.UtcNow);

        foreach (var entry in ChangeTracker.Entries<ICreationTimestamp>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IUpdateTimestamp>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTimeOffset timestamp)
                {
                    property.CurrentValue = TruncateToMicroseconds(timestamp);
                }
            }
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        return value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
