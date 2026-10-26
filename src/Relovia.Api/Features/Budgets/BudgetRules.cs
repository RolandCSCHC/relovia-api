using System.Globalization;

namespace Relovia.Api.Features.Budgets;

public static class BudgetRules
{
    public const int CurrencyCodeLength = 3;

    public const int CategoryNameMaxLength = 100;

    public const int LabelMaxLength = 200;

    public const int DescriptionMaxLength = 200;

    public const int NotesMaxLength = 1000;

    /// <summary>Amounts are stored as numeric(18,4). Four places cover currencies with three minor units.</summary>
    public const int AmountScale = 4;

    public const decimal MaxAmount = 1_000_000_000_000m;

    public const string IncomePairMessage = "Enter the amount and the currency, or leave both empty.";

    public static string NormalizeCurrencyCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public static bool IsCurrencyCode(string value)
    {
        var code = value.Trim();
        return code.Length == CurrencyCodeLength && code.All(char.IsAsciiLetter);
    }

    public static Dictionary<string, string[]> Validate(SaveBudgetRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireCurrency(errors, "currencyCode", request.CurrencyCode);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(SaveMonthlyNetIncomeRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var currency = request.CurrencyCode?.Trim() ?? string.Empty;
        var hasAmount = request.Amount is not null;
        var hasCurrency = currency.Length > 0;
        if (!hasAmount && !hasCurrency)
        {
            return errors;
        }

        if (!hasAmount || !hasCurrency)
        {
            errors["amount"] = [IncomePairMessage];
            return errors;
        }

        RequireAmount(errors, "amount", request.Amount!.Value);
        RequireCurrency(errors, "currencyCode", currency);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(SaveBudgetCategoryRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireText(errors, "name", request.Name, CategoryNameMaxLength, "Name");
        return errors;
    }

    public static Dictionary<string, string[]> Validate(SaveBudgetEstimateRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireText(errors, "label", request.Label, LabelMaxLength, "Label");
        RequireAmount(errors, "amount", request.Amount);
        RequireCurrency(errors, "currencyCode", request.CurrencyCode);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(SaveExpenseRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireText(errors, "description", request.Description, DescriptionMaxLength, "Description");
        RequireAmount(errors, "amount", request.Amount);
        RequireCurrency(errors, "currencyCode", request.CurrencyCode);

        if (request.Notes is { } notes && notes.Trim().Length > NotesMaxLength)
        {
            errors["notes"] = [$"Notes must be {NotesMaxLength} characters or fewer."];
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

    private static void RequireCurrency(Dictionary<string, string[]> errors, string key, string value)
    {
        if (!IsCurrencyCode(value))
        {
            errors[key] = ["Enter a three-letter currency code."];
        }
    }

    private static void RequireAmount(Dictionary<string, string[]> errors, string key, decimal amount)
    {
        if (amount <= 0)
        {
            errors[key] = ["Amount must be greater than zero."];
        }
        else if (amount > MaxAmount)
        {
            errors[key] = [string.Create(CultureInfo.InvariantCulture, $"Amount must be {MaxAmount:N0} or less.")];
        }
        else if (decimal.Round(amount, AmountScale) != amount)
        {
            errors[key] = [$"Amount can have at most {AmountScale} decimal places."];
        }
    }
}
