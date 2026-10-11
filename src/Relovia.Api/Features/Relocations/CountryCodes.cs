namespace Relovia.Api.Features.Relocations;

public static class CountryCodes
{
    public static string Normalize(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    public static bool AreSameCountry(string left, string right)
    {
        return string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
    }
}
