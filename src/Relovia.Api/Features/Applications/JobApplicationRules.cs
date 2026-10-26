namespace Relovia.Api.Features.Applications;

public static class JobApplicationRules
{
    public const int EmployerMaxLength = 200;

    public const int RoleMaxLength = 200;

    public const int LocationMaxLength = 200;

    public const int NotesMaxLength = 1000;

    public const string UnknownStatusMessage = "Choose a status.";

    public const string InternationalOnlyMessage = "Job applications are for an international move.";

    public static Dictionary<string, string[]> Validate(SaveJobApplicationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireText(errors, "employer", request.Employer, EmployerMaxLength, "Employer");
        RequireText(errors, "role", request.Role, RoleMaxLength, "Role");
        OptionalText(errors, "location", request.Location, LocationMaxLength, "Location");
        OptionalText(errors, "notes", request.Notes, NotesMaxLength, "Notes");

        if (!Enum.IsDefined(request.Status))
        {
            errors["status"] = [UnknownStatusMessage];
        }

        return errors;
    }

    public static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    public static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void RequireText(
        Dictionary<string, string[]> errors,
        string key,
        string value,
        int maxLength,
        string label)
    {
        var text = value.Trim();
        if (text.Length == 0)
        {
            errors[key] = [$"{label} is required."];
            return;
        }

        if (text.Length > maxLength)
        {
            errors[key] = [$"{label} must be {maxLength} characters or fewer."];
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
}
