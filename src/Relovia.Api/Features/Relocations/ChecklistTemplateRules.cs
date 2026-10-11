namespace Relovia.Api.Features.Relocations;

public static class ChecklistTemplateRules
{
    public const int TitleMaxLength = 200;

    public const int CodeMaxLength = 64;

    public static string NormalizeCode(string code)
    {
        return code.Trim();
    }

    public static bool IsCode(string code)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length is 0 or > CodeMaxLength)
        {
            return false;
        }

        foreach (var character in normalized)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')
            {
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyList<string> ValidateDependencies(string taskCode, IEnumerable<string> dependencyCodes)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dependencyCode in dependencyCodes)
        {
            if (!IsCode(dependencyCode))
            {
                errors.Add("A dependency code must be a stable code.");
                continue;
            }

            var normalized = NormalizeCode(dependencyCode);
            if (string.Equals(normalized, NormalizeCode(taskCode), StringComparison.Ordinal))
            {
                errors.Add("A task template cannot depend on its own code.");
            }

            if (!seen.Add(normalized))
            {
                errors.Add("A task template cannot list the same dependency code twice.");
            }
        }

        return errors;
    }
}
