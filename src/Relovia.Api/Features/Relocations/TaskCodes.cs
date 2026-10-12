namespace Relovia.Api.Features.Relocations;

public static class TaskCodes
{
    public static string Create(string title, IReadOnlySet<string> taken)
    {
        var slug = Slug(title);
        if (!taken.Contains(slug))
        {
            return slug;
        }

        for (var number = 2; number < 1000; number++)
        {
            var suffix = $"-{number}";
            var stem = slug;
            var room = ChecklistTemplateRules.CodeMaxLength - suffix.Length;
            if (stem.Length > room)
            {
                stem = stem[..room].TrimEnd('-');
            }

            var candidate = stem + suffix;
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("A unique task code could not be chosen.");
    }

    private static string Slug(string title)
    {
        var buffer = new char[Math.Min(title.Length, ChecklistTemplateRules.CodeMaxLength)];
        var length = 0;
        var hyphen = false;
        foreach (var character in title.Trim().ToLowerInvariant())
        {
            if (length == buffer.Length)
            {
                break;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                buffer[length++] = character;
                hyphen = false;
                continue;
            }

            if (length > 0 && !hyphen)
            {
                buffer[length++] = '-';
                hyphen = true;
            }
        }

        if (length > 0 && buffer[length - 1] == '-')
        {
            length--;
        }

        return length == 0 ? "task" : new string(buffer, 0, length);
    }
}
