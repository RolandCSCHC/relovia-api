namespace Relovia.Api.Features.Relocations;

public static class RelocationTaskRules
{
    public const int DescriptionMaxLength = 1000;

    public const int UrlMaxLength = 2048;

    public const string UrlMessage = "Link must be an absolute http or https address.";

    public static Dictionary<string, string[]> Validate(CreateRelocationTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireDetails(errors, request.Title, request.Phase, request.Priority, request.Description, request.Url);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(UpdateRelocationTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireDetails(
            errors,
            request.Title,
            request.Phase,
            request.Priority,
            request.Description,
            request.Url);

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

    public static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public static bool IsWebAddress(string value)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && !string.IsNullOrEmpty(uri.Host)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static void RequireDetails(
        Dictionary<string, string[]> errors,
        string title,
        TaskPhase phase,
        TaskPriority priority,
        string? description,
        string? url)
    {
        RequireTitle(errors, "title", title);
        OptionalText(errors, "description", description, DescriptionMaxLength, "Description");
        OptionalWebAddress(errors, url);

        if (!Enum.IsDefined(phase))
        {
            errors["phase"] = ["Phase must be Preparation, Housing, Administration, Finances, Moving, or Settlement."];
        }

        if (!Enum.IsDefined(priority))
        {
            errors["priority"] = ["Priority must be Low, Normal, or High."];
        }
    }

    private static void OptionalText(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength,
        string label)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            errors[key] = [$"{label} must be {maxLength} characters or fewer."];
        }
    }

    private static void OptionalWebAddress(Dictionary<string, string[]> errors, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var url = value.Trim();
        if (url.Length > UrlMaxLength)
        {
            errors["url"] = [$"Link must be {UrlMaxLength} characters or fewer."];
            return;
        }

        if (!IsWebAddress(url))
        {
            errors["url"] = [UrlMessage];
        }
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
