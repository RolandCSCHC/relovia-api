namespace Relovia.Api.Features.Inventory;

public static class InventoryTemplateRules
{
    public const int CodeMaxLength = 64;

    public static string NormalizeCode(string code)
    {
        return code.Trim();
    }

    public static bool IsCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

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

    public static Dictionary<string, string[]> ValidateConfirmation(ConfirmInventoryListRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!Enum.IsDefined(request.Furnishing))
        {
            errors["furnishing"] = ["Choose a furnished or unfurnished home."];
        }

        if (request.Room is { } room && !Enum.IsDefined(room))
        {
            errors["room"] = ["Choose a room."];
        }

        var codes = request.Codes ?? [];
        if (codes.Count == 0)
        {
            errors["codes"] = ["Select at least one item."];
            return errors;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in codes)
        {
            if (!IsCode(code))
            {
                errors["codes"] = ["Choose items from this list."];
                return errors;
            }

            if (!seen.Add(NormalizeCode(code)))
            {
                errors["codes"] = ["An item can only be selected once."];
                return errors;
            }
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateSuggestion(Furnishing? furnishing, ItemRoom? room)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (furnishing is null || !Enum.IsDefined(furnishing.Value))
        {
            errors["furnishing"] = ["Choose a furnished or unfurnished home."];
        }

        if (room is { } value && !Enum.IsDefined(value))
        {
            errors["room"] = ["Choose a room."];
        }

        return errors;
    }
}
