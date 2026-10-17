using Relovia.Api.Features.ExchangeRates;

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
        IEnumerable<Expense> expenses)
    {
        return categories
            .SelectMany(category => category.Estimates.Select(estimate => estimate.CurrencyCode))
            .Concat(expenses.Select(expense => expense.CurrencyCode))
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
        ExchangeRateTable? rates)
    {
        var categoryList = categories.ToList();
        var expenseList = expenses.ToList();
        if (currencyCode is null)
        {
            return new BudgetResponse(
                null,
                [],
                new BudgetTotalsResponse(0, 0, 0),
                [],
                null,
                false);
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
                var estimated = estimates.Sum(estimate => estimate.ConvertedAmount ?? 0);
                var counted = countedByCategory[category.Id].ToList();
                var actual = counted.Sum();
                return new BudgetCategoryResponse(
                    category.Id,
                    category.Name,
                    estimates,
                    estimated,
                    actual,
                    estimated - actual,
                    counted.Count);
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
        var unconverted = unconvertedEstimates.Select(estimate => estimate.CurrencyCode)
            .Concat(unconvertedExpenses.Select(expense => expense.CurrencyCode))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(code =>
            {
                var expensesInCode = unconvertedExpenses.Where(expense => expense.CurrencyCode == code).ToList();
                var estimatesInCode = unconvertedEstimates.Where(estimate => estimate.CurrencyCode == code).ToList();
                return new UnconvertedAmountsResponse(
                    code,
                    expensesInCode.Count,
                    expensesInCode.Sum(expense => expense.Amount),
                    estimatesInCode.Count,
                    estimatesInCode.Sum(estimate => estimate.Amount));
            })
            .ToArray();

        var needsRates = ForeignCurrencies(currencyCode, categoryList, expenseList).Count > 0;
        return new BudgetResponse(
            currencyCode,
            rows,
            new BudgetTotalsResponse(totalEstimated, totalActual, totalEstimated - totalActual),
            unconverted,
            needsRates ? rates?.UpdatedAt : null,
            needsRates && rates is null);
    }
}
