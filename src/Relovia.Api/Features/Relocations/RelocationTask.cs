using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Relocations;

public sealed class RelocationTask : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int DaysBeforeMove { get; set; }

    public DateOnly DueDate { get; set; }

    public TaskPhase Phase { get; set; }

    public TaskPriority Priority { get; set; }

    public RelocationTaskStatus Status { get; set; } = RelocationTaskStatus.Todo;

    public Guid? SourceTemplateTaskId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;

    public TaskTemplate? SourceTemplateTask { get; set; }

    public ICollection<TaskDependency> Dependencies { get; } = new List<TaskDependency>();

    public ICollection<TaskDependency> Dependents { get; } = new List<TaskDependency>();
}
