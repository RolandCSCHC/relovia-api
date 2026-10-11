using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Relocations;

public sealed class TaskTemplate : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ChecklistTemplateId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int DaysBeforeMove { get; set; }

    public TaskPhase Phase { get; set; }

    public TaskPriority Priority { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;

    public ICollection<TaskTemplateDependency> Dependencies { get; } = new List<TaskTemplateDependency>();

    public void ReplaceDependencies(IEnumerable<string> dependencyCodes)
    {
        var errors = ChecklistTemplateRules.ValidateDependencies(Code, dependencyCodes);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(errors[0]);
        }

        Dependencies.Clear();
        foreach (var dependencyCode in dependencyCodes)
        {
            Dependencies.Add(new TaskTemplateDependency
            {
                DependsOnCode = ChecklistTemplateRules.NormalizeCode(dependencyCode),
            });
        }
    }
}
