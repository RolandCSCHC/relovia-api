namespace Relovia.Api.Features.Relocations;

public static class RelocationRules
{
    public const int NameMaxLength = 200;

    public const int CityMaxLength = 200;

    public const int CountryCodeLength = 2;

    public static Dictionary<string, string[]> Validate(SaveRelocationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        RequireText(errors, "name", request.Name, NameMaxLength, "Name");
        RequireCountryCode(errors, "originCountryCode", request.OriginCountryCode);
        RequireText(errors, "originCity", request.OriginCity, CityMaxLength, "Origin city");
        RequireCountryCode(errors, "destinationCountryCode", request.DestinationCountryCode);
        RequireText(errors, "destinationCity", request.DestinationCity, CityMaxLength, "Destination city");

        if (!Enum.IsDefined(request.Purpose))
        {
            errors["purpose"] = ["Purpose must be Work, Study, Family, Lifestyle, or Other."];
        }

        if (!Enum.IsDefined(request.Status))
        {
            errors["status"] = ["Status must be Draft, Active, Completed, or Archived."];
        }

        return errors;
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

    private static void RequireCountryCode(Dictionary<string, string[]> errors, string key, string value)
    {
        var code = value.Trim();
        if (code.Length == CountryCodeLength && code.All(char.IsAsciiLetter))
        {
            return;
        }

        errors[key] = ["Enter a two-letter country code."];
    }
}
