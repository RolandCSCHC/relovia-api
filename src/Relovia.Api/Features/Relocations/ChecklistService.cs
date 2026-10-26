using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Relocations;

public sealed class ChecklistService(ReloviaDbContext db)
{
    // A swap or a close-up writes a temporary position first, so the unique
    // (relocation, position) index stays valid until the final numbers are stored.
    private const int PositionOffset = 1_000_000;
    public async Task<ChecklistSuggestionResponse?> SuggestAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return null;
        }

        var selection = ChecklistSelector.Select(relocation, await PublishedTemplatesAsync(cancellationToken));
        return ChecklistSuggestionResponse.From(relocation, selection);
    }

    public async Task<ChecklistConfirmResult> ConfirmAsync(
        Guid userId,
        Guid relocationId,
        ConfirmChecklistRequest request,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new ChecklistConfirmResult.NotFound();
        }

        var alreadyConfirmed = await db.RelocationTasks
            .AnyAsync(task => task.RelocationId == relocation.Id, cancellationToken);
        if (alreadyConfirmed)
        {
            return new ChecklistConfirmResult.AlreadyConfirmed();
        }

        var selection = ChecklistSelector.Select(relocation, await PublishedTemplatesAsync(cancellationToken));
        var suggested = selection.Tasks.ToDictionary(task => task.Code, StringComparer.Ordinal);
        var errors = RelocationTaskRules.ValidateConfirmation(request.Tasks, suggested);
        if (errors.Count > 0)
        {
            return new ChecklistConfirmResult.Invalid(errors);
        }

        var tasks = new List<RelocationTask>();
        var byCode = new Dictionary<string, RelocationTask>(StringComparer.Ordinal);
        for (var index = 0; index < request.Tasks.Count; index++)
        {
            var requested = request.Tasks[index];
            var code = ChecklistTemplateRules.NormalizeCode(requested.Code);
            var source = suggested[code];
            var task = new RelocationTask
            {
                RelocationId = relocation.Id,
                Code = code,
                Title = requested.Title.Trim(),
                DaysBeforeMove = requested.DaysBeforeMove,
                DueDate = ChecklistDueDate.On(relocation.TargetDate, requested.DaysBeforeMove),
                Phase = requested.Phase,
                Priority = requested.Priority,
                Status = RelocationTaskStatus.Todo,
                Position = index,
                SourceTemplateTaskId = source.SourceTemplateTaskId,
            };
            tasks.Add(task);
            byCode.Add(code, task);
        }

        var dependencies = new List<TaskDependency>();
        foreach (var task in tasks)
        {
            foreach (var dependencyCode in suggested[task.Code].DependsOnCodes)
            {
                if (!byCode.TryGetValue(dependencyCode, out var prerequisite))
                {
                    continue;
                }

                dependencies.Add(new TaskDependency
                {
                    RelocationId = relocation.Id,
                    TaskId = task.Id,
                    DependsOnTaskId = prerequisite.Id,
                });
            }
        }

        var dependencyError = TaskDependencyRules.Validate(dependencies
            .Select(dependency => new TaskDependencyRules.Link(
                dependency.RelocationId,
                dependency.TaskId,
                dependency.RelocationId,
                dependency.DependsOnTaskId))
            .ToArray());
        if (dependencyError is not null)
        {
            return new ChecklistConfirmResult.Invalid(new Dictionary<string, string[]>
            {
                ["tasks"] = [dependencyError],
            });
        }

        db.RelocationTasks.AddRange(tasks);
        db.TaskDependencies.AddRange(dependencies);
        await db.SaveChangesAsync(cancellationToken);

        var response = await ListAsync(userId, relocationId, cancellationToken);
        return new ChecklistConfirmResult.Confirmed(response!);
    }

    public async Task<RelocationTasksResponse?> ListAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return null;
        }

        var tasks = await db.RelocationTasks.AsNoTracking()
            .Include(task => task.Dependencies)
            .Where(task => task.RelocationId == relocation.Id)
            .ToListAsync(cancellationToken);

        return ToResponse(tasks, await DocumentsByTaskAsync(relocation.Id, cancellationToken));
    }

    public async Task<RelocationTaskCreateResult> CreateAsync(
        Guid userId,
        Guid relocationId,
        CreateRelocationTaskRequest request,
        CancellationToken cancellationToken)
    {
        var errors = RelocationTaskRules.Validate(request);
        if (errors.Count > 0)
        {
            return new RelocationTaskCreateResult.Invalid(errors);
        }

        var relocation = await db.Relocations.AsNoTracking()
            .Where(candidate => candidate.Id == relocationId && candidate.UserId == userId)
            .Select(candidate => new { candidate.Id, candidate.TargetDate })
            .SingleOrDefaultAsync(cancellationToken);
        if (relocation is null)
        {
            return new RelocationTaskCreateResult.RelocationNotFound();
        }

        var existing = await db.RelocationTasks.AsNoTracking()
            .Where(task => task.RelocationId == relocation.Id)
            .Select(task => new { task.Code, task.Position })
            .ToListAsync(cancellationToken);
        var codes = existing.Select(row => row.Code).ToHashSet(StringComparer.Ordinal);
        var task = new RelocationTask
        {
            RelocationId = relocation.Id,
            Code = TaskCodes.Create(request.Title, codes),
            Title = request.Title.Trim(),
            Description = RelocationTaskRules.NormalizeOptional(request.Description),
            Url = RelocationTaskRules.NormalizeOptional(request.Url),
            DaysBeforeMove = request.DueDate is DateOnly due && relocation.TargetDate is DateOnly target
                ? ChecklistDueDate.DaysBefore(target, due)
                : 0,
            DueDate = request.DueDate,
            Phase = request.Phase,
            Priority = request.Priority,
            Status = RelocationTaskStatus.Todo,
            Position = existing.Count == 0 ? 0 : existing.Max(row => row.Position) + 1,
        };
        db.RelocationTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        var response = ToResponse([task]).Tasks.Single();
        return new RelocationTaskCreateResult.Created(response);
    }

    public async Task<RelocationTaskDeleteResult> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new RelocationTaskDeleteResult.RelocationNotFound();
        }

        var task = await db.RelocationTasks.SingleOrDefaultAsync(
            candidate => candidate.Id == taskId && candidate.RelocationId == relocation.Id,
            cancellationToken);
        if (task is null)
        {
            return new RelocationTaskDeleteResult.TaskNotFound();
        }

        var links = await db.TaskDependencies
            .Where(dependency => dependency.RelocationId == relocation.Id
                && (dependency.TaskId == task.Id || dependency.DependsOnTaskId == task.Id))
            .ToListAsync(cancellationToken);
        var removed = task.Position;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.TaskDependencies.RemoveRange(links);
        db.RelocationTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        await CloseGapAsync(relocation.Id, removed, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new RelocationTaskDeleteResult.Deleted();
    }

    public Task<RelocationTaskMoveResult> MoveEarlierAsync(
        Guid userId,
        Guid relocationId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return MoveAsync(userId, relocationId, taskId, -1, cancellationToken);
    }

    public Task<RelocationTaskMoveResult> MoveLaterAsync(
        Guid userId,
        Guid relocationId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return MoveAsync(userId, relocationId, taskId, 1, cancellationToken);
    }

    private async Task<RelocationTaskMoveResult> MoveAsync(
        Guid userId,
        Guid relocationId,
        Guid taskId,
        int direction,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new RelocationTaskMoveResult.RelocationNotFound();
        }

        var tasks = await db.RelocationTasks
            .Where(task => task.RelocationId == relocation.Id)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);
        var index = tasks.FindIndex(task => task.Id == taskId);
        if (index < 0)
        {
            return new RelocationTaskMoveResult.TaskNotFound();
        }

        var neighborIndex = index + direction;
        if (neighborIndex < 0 || neighborIndex >= tasks.Count)
        {
            var message = direction < 0
                ? RelocationTaskRules.AlreadyFirstMessage
                : RelocationTaskRules.AlreadyLastMessage;
            return new RelocationTaskMoveResult.NotAllowed(message);
        }

        var task = tasks[index];
        var neighbor = tasks[neighborIndex];
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var taskPosition = task.Position;
        var neighborPosition = neighbor.Position;
        task.Position = taskPosition + PositionOffset;
        neighbor.Position = neighborPosition + PositionOffset;
        await db.SaveChangesAsync(cancellationToken);
        task.Position = neighborPosition;
        neighbor.Position = taskPosition;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = await ListAsync(userId, relocationId, cancellationToken);
        return new RelocationTaskMoveResult.Moved(response!);
    }

    public async Task<RelocationTaskUpdateResult> UpdateAsync(
        Guid userId,
        Guid relocationId,
        Guid taskId,
        UpdateRelocationTaskRequest request,
        CancellationToken cancellationToken)
    {
        var errors = RelocationTaskRules.Validate(request);
        if (errors.Count > 0)
        {
            return new RelocationTaskUpdateResult.Invalid(errors);
        }

        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new RelocationTaskUpdateResult.RelocationNotFound();
        }

        var task = await db.RelocationTasks.SingleOrDefaultAsync(
            candidate => candidate.Id == taskId && candidate.RelocationId == relocation.Id,
            cancellationToken);
        if (task is null)
        {
            return new RelocationTaskUpdateResult.TaskNotFound();
        }

        task.Title = request.Title.Trim();
        task.Description = RelocationTaskRules.NormalizeOptional(request.Description);
        task.Url = RelocationTaskRules.NormalizeOptional(request.Url);
        task.DueDate = request.DueDate;
        task.Phase = request.Phase;
        task.Priority = request.Priority;
        task.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);

        var tasks = await db.RelocationTasks.AsNoTracking()
            .Include(candidate => candidate.Dependencies)
            .Where(candidate => candidate.RelocationId == relocation.Id)
            .ToListAsync(cancellationToken);
        var response = ToResponse(tasks, await DocumentsByTaskAsync(relocation.Id, cancellationToken))
            .Tasks.Single(candidate => candidate.Id == task.Id);
        return new RelocationTaskUpdateResult.Updated(response);
    }

    private async Task<Relocation?> FindRelocationAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AsNoTracking()
            .SingleOrDefaultAsync(
                relocation => relocation.Id == relocationId && relocation.UserId == userId,
                cancellationToken);
    }

    private async Task<List<ChecklistTemplate>> PublishedTemplatesAsync(CancellationToken cancellationToken)
    {
        return await db.ChecklistTemplates.AsNoTracking()
            .Where(template => template.PublishedAt != null)
            .Include(template => template.Tasks)
            .ThenInclude(task => task.Dependencies)
            .ToListAsync(cancellationToken);
    }

    private async Task CloseGapAsync(Guid relocationId, int removed, CancellationToken cancellationToken)
    {
        var later = await db.RelocationTasks
            .Where(task => task.RelocationId == relocationId && task.Position > removed)
            .ToListAsync(cancellationToken);
        if (later.Count == 0)
        {
            return;
        }

        foreach (var task in later)
        {
            task.Position += PositionOffset;
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var task in later)
        {
            task.Position -= PositionOffset + 1;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, Guid[]>> DocumentsByTaskAsync(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        var links = await db.DocumentTaskLinks.AsNoTracking()
            .Where(link => link.RelocationId == relocationId)
            .Select(link => new { link.TaskId, link.DocumentId })
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(link => link.TaskId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.DocumentId).OrderBy(id => id).ToArray());
    }

    private static RelocationTasksResponse ToResponse(
        IReadOnlyList<RelocationTask> tasks,
        IReadOnlyDictionary<Guid, Guid[]>? documentsByTask = null)
    {
        var byId = tasks.ToDictionary(task => task.Id);
        var progress = TaskProgressResponse.From(TaskProgress.Calculate(tasks.Select(task => task.Status)));
        var responses = tasks
            .OrderBy(task => task.Position)
            .ThenBy(task => task.Id)
            .Select(task => new RelocationTaskResponse(
                task.Id,
                task.Code,
                task.Title,
                task.Description,
                task.Url,
                task.DaysBeforeMove,
                task.DueDate,
                task.Phase,
                task.Priority,
                task.Status,
                task.SourceTemplateTaskId,
                task.Dependencies
                    .OrderBy(dependency => byId[dependency.DependsOnTaskId].Code, StringComparer.Ordinal)
                    .Select(dependency =>
                    {
                        var prerequisite = byId[dependency.DependsOnTaskId];
                        return new TaskDependencyResponse(
                            prerequisite.Id,
                            prerequisite.Code,
                            TaskProgress.IsDependencySatisfied(prerequisite.Status));
                    })
                    .ToArray(),
                documentsByTask is not null && documentsByTask.TryGetValue(task.Id, out var documentIds)
                    ? documentIds
                    : [],
                task.Position))
            .ToArray();

        return new RelocationTasksResponse(progress, responses);
    }
}
