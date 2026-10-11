namespace Relovia.Api.Features.Relocations;

public sealed class TaskTemplateDependency
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TaskTemplateId { get; set; }

    public string DependsOnCode { get; set; } = string.Empty;

    public TaskTemplate TaskTemplate { get; set; } = null!;
}
