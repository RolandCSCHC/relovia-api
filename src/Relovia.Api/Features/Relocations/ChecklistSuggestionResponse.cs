namespace Relovia.Api.Features.Relocations;

public sealed record ChecklistSuggestionResponse(
    Guid RelocationId,
    DateOnly TargetDate,
    IReadOnlyList<string> TemplateCodes,
    IReadOnlyList<SuggestedTaskResponse> Tasks)
{
    public static ChecklistSuggestionResponse From(Relocation relocation, ChecklistSelection selection)
    {
        return new ChecklistSuggestionResponse(
            relocation.Id,
            relocation.TargetDate,
            selection.Templates.Select(template => template.Code).ToArray(),
            selection.Tasks.Select(task => new SuggestedTaskResponse(
                task.Code,
                task.Title,
                task.DaysBeforeMove,
                ChecklistDueDate.On(relocation.TargetDate, task.DaysBeforeMove),
                task.Phase,
                task.Priority,
                task.SourceTemplateTaskId,
                task.DependsOnCodes)).ToArray());
    }
}

public sealed record SuggestedTaskResponse(
    string Code,
    string Title,
    int DaysBeforeMove,
    DateOnly DueDate,
    TaskPhase Phase,
    TaskPriority Priority,
    Guid SourceTemplateTaskId,
    IReadOnlyList<string> DependsOnCodes);
