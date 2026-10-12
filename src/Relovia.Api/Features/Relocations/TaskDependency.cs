namespace Relovia.Api.Features.Relocations;

public sealed class TaskDependency
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public Guid TaskId { get; set; }

    public Guid DependsOnTaskId { get; set; }

    public RelocationTask Task { get; set; } = null!;

    public RelocationTask DependsOnTask { get; set; } = null!;
}
