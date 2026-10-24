namespace Relovia.Api.Features.Relocations;

public sealed record RelocationTasksResponse(
    TaskProgressResponse Progress,
    IReadOnlyList<RelocationTaskResponse> Tasks);

public sealed record TaskProgressResponse(int Done, int Counted, decimal Ratio)
{
    public static TaskProgressResponse From(TaskProgress.Snapshot snapshot)
    {
        return new TaskProgressResponse(snapshot.Done, snapshot.Counted, snapshot.Ratio);
    }
}

public sealed record RelocationTaskResponse(
    Guid Id,
    string Code,
    string Title,
    int DaysBeforeMove,
    DateOnly? DueDate,
    TaskPhase Phase,
    TaskPriority Priority,
    RelocationTaskStatus Status,
    Guid? SourceTemplateTaskId,
    IReadOnlyList<TaskDependencyResponse> Dependencies,
    IReadOnlyList<Guid> DocumentIds);

public sealed record TaskDependencyResponse(Guid TaskId, string Code, bool IsSatisfied);
