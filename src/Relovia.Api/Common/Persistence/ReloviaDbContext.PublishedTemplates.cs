using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence;

public sealed partial class ReloviaDbContext
{
    private void RejectPublishedTemplateEdits()
    {
        ChangeTracker.DetectChanges();
        RejectChangedPublishedTemplates();
        RejectChangedPublishedInventoryLists();
        var templateIds = ChangedChecklistTemplateIds();
        if (templateIds.Count > 0 && PublishedTemplateIds(templateIds).Count > 0)
        {
            throw new PublishedTemplateChangeException();
        }

        var listIds = ChangedInventoryListIds();
        if (listIds.Count > 0 && PublishedInventoryListIds(listIds).Count > 0)
        {
            throw new PublishedTemplateChangeException();
        }
    }

    private async Task RejectPublishedTemplateEditsAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        RejectChangedPublishedTemplates();
        RejectChangedPublishedInventoryLists();
        var templateIds = ChangedChecklistTemplateIds();
        if (templateIds.Count > 0 && (await PublishedTemplateIdsAsync(templateIds, cancellationToken)).Count > 0)
        {
            throw new PublishedTemplateChangeException();
        }

        var listIds = ChangedInventoryListIds();
        if (listIds.Count > 0 && (await PublishedInventoryListIdsAsync(listIds, cancellationToken)).Count > 0)
        {
            throw new PublishedTemplateChangeException();
        }
    }

    private void RejectChangedPublishedTemplates()
    {
        foreach (var entry in ChangeTracker.Entries<ChecklistTemplate>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Property(template => template.PublishedAt).OriginalValue is not null)
            {
                throw new PublishedTemplateChangeException();
            }
        }
    }

    private List<Guid> ChangedChecklistTemplateIds()
    {
        var templateIds = new HashSet<Guid>();
        var taskIds = new HashSet<Guid>();

        foreach (var entry in ChangeTracker.Entries<TaskTemplate>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                templateIds.Add(entry.Entity.ChecklistTemplateId);
            }
        }

        foreach (var entry in ChangeTracker.Entries<TaskTemplateDependency>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Entity.TaskTemplate is not null)
            {
                templateIds.Add(entry.Entity.TaskTemplate.ChecklistTemplateId);
                continue;
            }

            var trackedTask = ChangeTracker.Entries<TaskTemplate>()
                .SingleOrDefault(task => task.Entity.Id == entry.Entity.TaskTemplateId);
            if (trackedTask is not null)
            {
                templateIds.Add(trackedTask.Entity.ChecklistTemplateId);
                continue;
            }

            taskIds.Add(entry.Entity.TaskTemplateId);
        }

        if (taskIds.Count > 0)
        {
            var fromTasks = TaskTemplates.AsNoTracking()
                .Where(task => taskIds.Contains(task.Id))
                .Select(task => task.ChecklistTemplateId);
            foreach (var templateId in fromTasks)
            {
                templateIds.Add(templateId);
            }
        }

        return templateIds.ToList();
    }

    private List<Guid> PublishedTemplateIds(List<Guid> templateIds)
    {
        return ChecklistTemplates.AsNoTracking()
            .Where(template => templateIds.Contains(template.Id) && template.PublishedAt != null)
            .Select(template => template.Id)
            .ToList();
    }

    private Task<List<Guid>> PublishedTemplateIdsAsync(
        List<Guid> templateIds,
        CancellationToken cancellationToken)
    {
        return ChecklistTemplates.AsNoTracking()
            .Where(template => templateIds.Contains(template.Id) && template.PublishedAt != null)
            .Select(template => template.Id)
            .ToListAsync(cancellationToken);
    }

    private void RejectChangedPublishedInventoryLists()
    {
        foreach (var entry in ChangeTracker.Entries<InventoryListTemplate>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Property(template => template.PublishedAt).OriginalValue is not null)
            {
                throw new PublishedTemplateChangeException();
            }
        }
    }

    private List<Guid> ChangedInventoryListIds()
    {
        var templateIds = new HashSet<Guid>();
        foreach (var entry in ChangeTracker.Entries<InventoryTemplateItem>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                templateIds.Add(entry.Entity.InventoryListTemplateId);
            }
        }

        return templateIds.ToList();
    }

    private List<Guid> PublishedInventoryListIds(List<Guid> templateIds)
    {
        return InventoryListTemplates.AsNoTracking()
            .Where(template => templateIds.Contains(template.Id) && template.PublishedAt != null)
            .Select(template => template.Id)
            .ToList();
    }

    private Task<List<Guid>> PublishedInventoryListIdsAsync(
        List<Guid> templateIds,
        CancellationToken cancellationToken)
    {
        return InventoryListTemplates.AsNoTracking()
            .Where(template => templateIds.Contains(template.Id) && template.PublishedAt != null)
            .Select(template => template.Id)
            .ToListAsync(cancellationToken);
    }
}
