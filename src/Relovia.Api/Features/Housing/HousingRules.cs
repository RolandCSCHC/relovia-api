using System.Globalization;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Features.Housing;

public static class HousingRules
{
    public const int NameMaxLength = 200;

    public const int AreaMaxLength = 200;

    public const int SizeMaxLength = 80;

    public const int NotesMaxLength = 1000;

    public const string UnknownStatusMessage = "Choose a status.";

    public const string UnknownFurnishingMessage = "Choose furnished or unfurnished.";

    public const string CurrencyWithoutMoneyMessage = "Enter a rent or a deposit, or leave the currency empty.";

    public const string CurrencyRequiredMessage = "Enter a three-letter currency code.";

    public const string AmountPositiveMessage = "Amount must be greater than zero.";

    public const string AmountScaleMessage = "Amount can have at most 4 decimal places.";

    public const string AlreadyChosenMessage = "One home is already chosen. Change that one first.";

    public const string InternationalOnlyMessage = "Housing comparison is for an international move.";

    public const string RentPeriodRequiredMessage = "Choose weekly or monthly.";

    public const string RentPeriodWithoutRentMessage = "A home with no rent has no rent period.";

    public static Dictionary<string, string[]> Validate(SaveHousingOptionRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireText(errors, "name", request.Name, NameMaxLength, "Name");
        OptionalText(errors, "area", request.Area, AreaMaxLength, "Area");
        OptionalText(errors, "size", request.Size, SizeMaxLength, "Size");
        OptionalText(errors, "notes", request.Notes, NotesMaxLength, "Notes");

        if (!Enum.IsDefined(request.Status))
        {
            errors["status"] = [UnknownStatusMessage];
        }

        if (request.Furnishing is { } furnishing && !Enum.IsDefined(furnishing))
        {
            errors["furnishing"] = [UnknownFurnishingMessage];
        }

        var hasRent = request.Rent is not null;
        var hasDeposit = request.Deposit is not null;
        if (request.Rent is { } rent)
        {
            RequireAmount(errors, "rent", rent);
        }

        if (request.Deposit is { } deposit)
        {
            RequireAmount(errors, "deposit", deposit);
        }

        if (hasRent)
        {
            if (request.RentPeriod is not { } period || !Enum.IsDefined(period))
            {
                errors["rentPeriod"] = [RentPeriodRequiredMessage];
            }
        }
        else if (request.RentPeriod is not null)
        {
            errors["rentPeriod"] = [RentPeriodWithoutRentMessage];
        }

        var currency = request.CurrencyCode?.Trim() ?? string.Empty;
        if (!hasRent && !hasDeposit)
        {
            if (currency.Length > 0)
            {
                errors["currencyCode"] = [CurrencyWithoutMoneyMessage];
            }
        }
        else if (!BudgetRules.IsCurrencyCode(currency))
        {
            errors["currencyCode"] = [CurrencyRequiredMessage];
        }

        return errors;
    }

    public static string NormalizeName(string name)
    {
        return name.Trim();
    }

    public static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public static string? NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant();
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

    private static void RequireAmount(Dictionary<string, string[]> errors, string key, decimal amount)
    {
        if (amount <= 0)
        {
            errors[key] = [AmountPositiveMessage];
        }
        else if (amount > BudgetRules.MaxAmount)
        {
            errors[key] = [string.Create(CultureInfo.InvariantCulture, $"Amount must be {BudgetRules.MaxAmount:N0} or less.")];
        }
        else if (decimal.Round(amount, BudgetRules.AmountScale) != amount)
        {
            errors[key] = [AmountScaleMessage];
        }
    }
}
