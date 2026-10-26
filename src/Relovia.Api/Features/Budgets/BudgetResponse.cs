namespace Relovia.Api.Features.Budgets;

/// <summary>
/// The budget with its estimate versus actual comparison, all in the budget currency. Estimates and
/// expenses in other currencies are converted with the latest exchange rates. Anything that cannot
/// be converted is left out of the figures and listed in <see cref="Unconverted"/>.
/// </summary>
/// <param name="RatesAsOf">When the provider last updated the rates behind the conversions, if any were needed.</param>
/// <param name="RatesUnavailable">True when conversions were needed and no rates could be loaded.</param>
public sealed record BudgetResponse(
    string? CurrencyCode,
    IReadOnlyList<BudgetCategoryResponse> Categories,
    BudgetTotalsResponse Totals,
    IReadOnlyList<UnconvertedAmountsResponse> Unconverted,
    DateTimeOffset? RatesAsOf,
    bool RatesUnavailable,
    decimal? MonthlyNetIncome = null,
    string? MonthlyNetIncomeCurrency = null);

public sealed record BudgetCategoryResponse(
    Guid Id,
    string Name,
    IReadOnlyList<BudgetEstimateResponse> Estimates,
    decimal Estimated,
    decimal Actual,
    decimal Remaining,
    int ExpenseCount,
    IReadOnlyList<ShoppingLineResponse> Shopping);

/// <summary>A to-purchase inventory item counted in this category. Prices are the originals.</summary>
public sealed record ShoppingLineResponse(
    Guid ItemId,
    string Name,
    decimal? EstimatedPrice,
    decimal? ActualPrice,
    string CurrencyCode,
    decimal? ConvertedEstimated,
    decimal? ConvertedActual);

/// <param name="ConvertedAmount">The amount in the budget currency, or null when there is no rate for its currency.</param>
public sealed record BudgetEstimateResponse(
    Guid Id,
    Guid CategoryId,
    string Label,
    decimal Amount,
    string CurrencyCode,
    decimal? ConvertedAmount);

public sealed record BudgetTotalsResponse(decimal Estimated, decimal Actual, decimal Remaining);

public sealed record UnconvertedAmountsResponse(
    string CurrencyCode,
    int ExpenseCount,
    decimal ExpenseTotal,
    int EstimateCount,
    decimal EstimateTotal,
    int ShoppingCount,
    decimal ShoppingEstimated,
    decimal ShoppingSpent);

/// <param name="ConvertedAmount">The amount in the budget currency at the latest rate, or null when it cannot be converted.</param>
public sealed record ExpenseResponse(
    Guid Id,
    Guid CategoryId,
    Guid? TaskId,
    string Description,
    decimal Amount,
    string CurrencyCode,
    decimal? ConvertedAmount,
    DateOnly PaidOn,
    string? Notes)
{
    public static ExpenseResponse From(Expense expense, decimal? convertedAmount)
    {
        return new ExpenseResponse(
            expense.Id,
            expense.CategoryId,
            expense.TaskId,
            expense.Description,
            expense.Amount,
            expense.CurrencyCode,
            convertedAmount,
            expense.PaidOn,
            expense.Notes);
    }
}
