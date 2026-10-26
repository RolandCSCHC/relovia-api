using Relovia.Api.Features.ExchangeRates;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.Features.Budgets;

public static class BudgetComparison
{
    /// <summary>
    /// Converts an amount into the budget currency, or returns null when that needs a rate that is
    /// not available. The original amount is never changed.
    /// </summary>
    public static decimal? Convert(
        decimal amount,
        string currencyCode,
        string budgetCurrency,
        ExchangeRateTable? rates)
    {
        if (string.Equals(currencyCode, budgetCurrency, StringComparison.Ordinal))
        {
            return amount;
        }

        return rates is not null && rates.TryConvert(amount, currencyCode, out var converted)
            ? converted
            : null;
    }

    /// <summary>The currencies other than the budget currency that estimates or expenses use.</summary>
    public static IReadOnlySet<string> ForeignCurrencies(
        string budgetCurrency,
        IEnumerable<BudgetCategory> categories,
        IEnumerable<Expense> expenses,
        IEnumerable<InventoryItem>? shopping = null)
    {
        return categories
            .SelectMany(category => category.Estimates.Select(estimate => estimate.CurrencyCode))
            .Concat(expenses.Select(expense => expense.CurrencyCode))
            .Concat(PricedShopping(shopping).Select(item => item.CurrencyCode!))
            .Where(code => !string.Equals(code, budgetCurrency, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Builds the estimate versus actual view in the budget currency. Amounts in other currencies
    /// are converted with <paramref name="rates"/>. One without a rate is left out of every figure and
    /// totalled by currency in the unconverted list.
    /// </summary>
    public static BudgetResponse Build(
        string? currencyCode,
        IEnumerable<BudgetCategory> categories,
        IEnumerable<Expense> expenses,
        ExchangeRateTable? rates,
        IEnumerable<InventoryItem>? shopping = null,
        decimal? monthlyNetIncome = null,
        string? monthlyNetIncomeCurrency = null)
    {
        var categoryList = categories.ToList();
        var expenseList = expenses.ToList();
        var shoppingList = PricedShopping(shopping).ToList();
        if (currencyCode is null)
        {
            return new BudgetResponse(
                null,
                [],
                new BudgetTotalsResponse(0, 0, 0),
                [],
                null,
                false,
                monthlyNetIncome,
                monthlyNetIncomeCurrency);
        }

        var convertedExpenses = expenseList
            .Select(expense => (Expense: expense, Converted: Convert(expense.Amount, expense.CurrencyCode, currencyCode, rates)))
            .ToList();
        var countedByCategory = convertedExpenses
            .Where(item => item.Converted is not null)
            .ToLookup(item => item.Expense.CategoryId, item => item.Converted!.Value);

        var rows = categoryList
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(category => category.Id)
            .Select(category =>
            {
                var estimates = category.Estimates
                    .OrderBy(estimate => estimate.Label, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(estimate => estimate.Id)
                    .Select(estimate => new BudgetEstimateResponse(
                        estimate.Id,
                        estimate.CategoryId,
                        estimate.Label,
                        estimate.Amount,
                        estimate.CurrencyCode,
                        Convert(estimate.Amount, estimate.CurrencyCode, currencyCode, rates)))
                    .ToArray();
                var lines = shoppingList
                    .Where(item => item.BudgetCategoryId == category.Id)
                    .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.Id)
                    .Select(item => Line(item, currencyCode, rates))
                    .ToArray();
                var estimated = estimates.Sum(estimate => estimate.ConvertedAmount ?? 0)
                    + lines.Sum(line => line.ConvertedEstimated ?? 0);
                var counted = countedByCategory[category.Id].ToList();
                var actual = counted.Sum() + lines.Sum(line => line.ConvertedActual ?? 0);
                return new BudgetCategoryResponse(
                    category.Id,
                    category.Name,
                    estimates,
                    estimated,
                    actual,
                    estimated - actual,
                    counted.Count,
                    lines);
            })
            .ToArray();

        var totalEstimated = rows.Sum(row => row.Estimated);
        var totalActual = rows.Sum(row => row.Actual);

        var unconvertedEstimates = rows
            .SelectMany(row => row.Estimates)
            .Where(estimate => estimate.ConvertedAmount is null)
            .ToList();
        var unconvertedExpenses = convertedExpenses
            .Where(item => item.Converted is null)
            .Select(item => item.Expense)
            .ToList();
        var unconvertedShopping = rows
            .SelectMany(row => row.Shopping)
            .Where(line =>
                (line.EstimatedPrice is not null && line.ConvertedEstimated is null)
                || (line.ActualPrice is not null && line.ConvertedActual is null))
            .ToList();
        var unconverted = unconvertedEstimates.Select(estimate => estimate.CurrencyCode)
            .Concat(unconvertedExpenses.Select(expense => expense.CurrencyCode))
            .Concat(unconvertedShopping.Select(line => line.CurrencyCode))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(code =>
            {
                var expensesInCode = unconvertedExpenses.Where(expense => expense.CurrencyCode == code).ToList();
                var estimatesInCode = unconvertedEstimates.Where(estimate => estimate.CurrencyCode == code).ToList();
                var shoppingInCode = unconvertedShopping.Where(line => line.CurrencyCode == code).ToList();
                return new UnconvertedAmountsResponse(
                    code,
                    expensesInCode.Count,
                    expensesInCode.Sum(expense => expense.Amount),
                    estimatesInCode.Count,
                    estimatesInCode.Sum(estimate => estimate.Amount),
                    shoppingInCode.Count,
                    shoppingInCode
                        .Where(line => line.EstimatedPrice is not null && line.ConvertedEstimated is null)
                        .Sum(line => line.EstimatedPrice ?? 0),
                    shoppingInCode
                        .Where(line => line.ActualPrice is not null && line.ConvertedActual is null)
                        .Sum(line => line.ActualPrice ?? 0));
            })
            .ToArray();

        var needsRates = ForeignCurrencies(currencyCode, categoryList, expenseList, shoppingList).Count > 0;
        return new BudgetResponse(
            currencyCode,
            rows,
            new BudgetTotalsResponse(totalEstimated, totalActual, totalEstimated - totalActual),
            unconverted,
            needsRates ? rates?.UpdatedAt : null,
            needsRates && rates is null,
            monthlyNetIncome,
            monthlyNetIncomeCurrency);
    }

    /// <summary>To-purchase items with a budget category and a price. Owned items are not a new cost.</summary>
    private static IEnumerable<InventoryItem> PricedShopping(IEnumerable<InventoryItem>? shopping)
    {
        return (shopping ?? []).Where(item =>
            item.Acquisition == AcquisitionStatus.ToPurchase
            && item.BudgetCategoryId is not null
            && item.CurrencyCode is not null
            && (item.EstimatedPrice is not null || item.ActualPrice is not null));
    }

    private static ShoppingLineResponse Line(InventoryItem item, string budgetCurrency, ExchangeRateTable? rates)
    {
        return new ShoppingLineResponse(
            item.Id,
            item.Name,
            item.EstimatedPrice,
            item.ActualPrice,
            item.CurrencyCode!,
            item.EstimatedPrice is { } estimated
                ? Convert(estimated, item.CurrencyCode!, budgetCurrency, rates)
                : null,
            item.ActualPrice is { } actual
                ? Convert(actual, item.CurrencyCode!, budgetCurrency, rates)
                : null);
    }
}
