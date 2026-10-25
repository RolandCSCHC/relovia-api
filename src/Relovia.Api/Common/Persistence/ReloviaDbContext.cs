using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.Notifications;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence;

public sealed partial class ReloviaDbContext(DbContextOptions<ReloviaDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<Relocation> Relocations => Set<Relocation>();

    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();

    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();

    public DbSet<TaskTemplateDependency> TaskTemplateDependencies => Set<TaskTemplateDependency>();

    public DbSet<RelocationTask> RelocationTasks => Set<RelocationTask>();

    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();

    public DbSet<BudgetEstimate> BudgetEstimates => Set<BudgetEstimate>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<InventoryListTemplate> InventoryListTemplates => Set<InventoryListTemplate>();

    public DbSet<InventoryTemplateItem> InventoryTemplateItems => Set<InventoryTemplateItem>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationEmail> NotificationEmails => Set<NotificationEmail>();

    public DbSet<DocumentTaskLink> DocumentTaskLinks => Set<DocumentTaskLink>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectPublishedTemplateEdits();
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await RejectPublishedTemplateEditsAsync(cancellationToken);
        ApplyTimestamps();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
