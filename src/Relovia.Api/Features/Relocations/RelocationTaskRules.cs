namespace Relovia.Api.Features.Relocations;

public static class RelocationTaskRules
{
    public static Dictionary<string, string[]> Validate(UpdateRelocationTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireTitle(errors, "title", request.Title);

        if (!Enum.IsDefined(request.Phase))
        {
            errors["phase"] = ["Phase must be Preparation, Housing, Administration, Finances, Moving, or Settlement."];
        }

        if (!Enum.IsDefined(request.Priority))
        {
            errors["priority"] = ["Priority must be Low, Normal, or High."];
        }

        if (!Enum.IsDefined(request.Status))
        {
            errors["status"] = ["Status must be Todo, InProgress, Done, or Skipped."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateConfirmation(
        IReadOnlyList<ConfirmChecklistTaskRequest> tasks,
        IReadOnlyDictionary<string, SelectedChecklistTask> suggested)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            var code = ChecklistTemplateRules.NormalizeCode(task.Code);
            if (!ChecklistTemplateRules.IsCode(task.Code) || !suggested.ContainsKey(code))
            {
                errors[$"tasks[{index}].code"] = ["Choose a task from the suggestion."];
            }
            else if (!seen.Add(code))
            {
                errors[$"tasks[{index}].code"] = ["A checklist cannot contain the same task code twice."];
            }

            RequireTitle(errors, $"tasks[{index}].title", task.Title);

            if (!Enum.IsDefined(task.Phase))
            {
                errors[$"tasks[{index}].phase"] = ["Phase must be Preparation, Housing, Administration, Finances, Moving, or Settlement."];
            }

            if (!Enum.IsDefined(task.Priority))
            {
                errors[$"tasks[{index}].priority"] = ["Priority must be Low, Normal, or High."];
            }
        }

        return errors;
    }

    private static void RequireTitle(Dictionary<string, string[]> errors, string key, string value)
    {
        var title = value.Trim();
        if (title.Length == 0)
        {
            errors[key] = ["Title is required."];
            return;
        }

        if (title.Length > ChecklistTemplateRules.TitleMaxLength)
        {
            errors[key] = [$"Title must be {ChecklistTemplateRules.TitleMaxLength} characters or fewer."];
        }
    }
}
