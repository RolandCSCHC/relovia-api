using System.Globalization;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Features.Inventory;

public static class InventoryRules
{
    public const int NameMaxLength = 200;

    public const int NotesMaxLength = BudgetRules.NotesMaxLength;

    public const int MinQuantity = 1;

    public const int MaxQuantity = 100_000;

    public const int PriceScale = BudgetRules.AmountScale;

    public const decimal MaxPrice = BudgetRules.MaxAmount;

    public static Dictionary<string, string[]> Validate(SaveInventoryItemRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        RequireText(errors, "name", request.Name, NameMaxLength, "Name");
        if (request.Category is { } category && !Enum.IsDefined(category))
        {
            errors["category"] = ["Choose a category."];
        }

        if (request.Room is { } room && !Enum.IsDefined(room))
        {
            errors["room"] = ["Choose a room."];
        }

        RequireQuantity(errors, request.Quantity);
        RequireDefined(
            errors,
            "acquisition",
            request.Acquisition,
            "Choose whether you own this or still need to buy it.");
        if (request.MovingAction is { } action && !Enum.IsDefined(action))
        {
            errors["movingAction"] = ["Choose what to do with this item."];
        }

        if (request.PackingStatus is { } packing && !Enum.IsDefined(packing))
        {
            errors["packingStatus"] = ["Choose a packing status."];
        }
        else if (request.MovingAction != MovingAction.Bring && request.PackingStatus is not null)
        {
            errors["packingStatus"] = ["Packing status applies only when you are bringing the item."];
        }

        RequireDefined(errors, "priority", request.Priority, "Choose a priority.");
        ValidatePrices(errors, request);
        ValidateBudgetCategory(errors, request);

        if (request.Notes is { } notes && notes.Trim().Length > NotesMaxLength)
        {
            errors["notes"] = [$"Notes must be {NotesMaxLength} characters or fewer."];
        }

        return errors;
    }

    private static void ValidatePrices(Dictionary<string, string[]> errors, SaveInventoryItemRequest request)
    {
        var hasEstimate = request.EstimatedPrice is not null;
        var hasActual = request.ActualPrice is not null;
        if (request.EstimatedPrice is { } estimated)
        {
            RequirePrice(errors, "estimatedPrice", estimated);
        }

        if (request.ActualPrice is { } actual)
        {
            RequirePrice(errors, "actualPrice", actual);
        }

        var currency = request.CurrencyCode?.Trim() ?? string.Empty;
        if (!hasEstimate && !hasActual)
        {
            if (currency.Length > 0)
            {
                errors["currencyCode"] = ["Enter a price, or leave the currency empty."];
            }

            return;
        }

        if (!BudgetRules.IsCurrencyCode(currency))
        {
            errors["currencyCode"] = ["Enter a three-letter currency code."];
        }
    }

    /// <summary>
    /// A price on something still to buy needs a budget category. An owned item is not a new cost.
    /// </summary>
    private static void ValidateBudgetCategory(
        Dictionary<string, string[]> errors,
        SaveInventoryItemRequest request)
    {
        var priced = request.EstimatedPrice is not null || request.ActualPrice is not null;
        if (request.Acquisition == AcquisitionStatus.Owned && request.BudgetCategoryId is not null)
        {
            errors["budgetCategoryId"] = ["A budget category applies only when you still need to buy the item."];
            return;
        }

        if (request.Acquisition == AcquisitionStatus.ToPurchase && priced && request.BudgetCategoryId is null)
        {
            errors["budgetCategoryId"] = ["Choose a budget category."];
        }
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

    private static void RequireQuantity(Dictionary<string, string[]> errors, int quantity)
    {
        if (quantity < MinQuantity || quantity > MaxQuantity)
        {
            errors["quantity"] = [string.Create(
                CultureInfo.InvariantCulture,
                $"Quantity must be a whole number from {MinQuantity:N0} to {MaxQuantity:N0}.")];
        }
    }

    private static void RequirePrice(Dictionary<string, string[]> errors, string key, decimal amount)
    {
        if (amount <= 0)
        {
            errors[key] = ["Amount must be greater than zero."];
        }
        else if (amount > MaxPrice)
        {
            errors[key] = [string.Create(CultureInfo.InvariantCulture, $"Amount must be {MaxPrice:N0} or less.")];
        }
        else if (decimal.Round(amount, PriceScale) != amount)
        {
            errors[key] = [$"Amount can have at most {PriceScale} decimal places."];
        }
    }

    private static void RequireDefined<TEnum>(
        Dictionary<string, string[]> errors,
        string key,
        TEnum value,
        string message)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            errors[key] = [message];
        }
    }
}
