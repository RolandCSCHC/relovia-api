using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.ExchangeRates;

namespace Relovia.Api.UnitTests;

public sealed class BudgetComparisonTests
{
    private static readonly Guid RelocationId = Guid.CreateVersion7();

    // One CLP buys 0.001 NZD, so 1 NZD is 1000 CLP, and one CLP buys 0.001 EUR.
    private static readonly ExchangeRateTable ClpRates = new(
        "CLP",
        new Dictionary<string, decimal> { ["CLP"] = 1m, ["NZD"] = 0.002m, ["EUR"] = 0.001m },
        new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero));

    private readonly BudgetCategory _housing = Category("Housing");
    private readonly BudgetCategory _furnishing = Category("Furnishing");

    [Fact]
    public void A_budget_without_a_currency_has_no_figures()
    {
        var response = BudgetComparison.Build(null, [], [], null);

        Assert.Null(response.CurrencyCode);
        Assert.Empty(response.Categories);
        Assert.Equal(0m, response.Totals.Estimated);
        Assert.Empty(response.Unconverted);
        Assert.False(response.RatesUnavailable);
    }

    [Fact]
    public void Estimates_in_the_budget_currency_add_up_per_category_and_in_total()
    {
        _housing.Estimates.Add(Estimate(_housing, "Deposit", 2000, "EUR"));
        _housing.Estimates.Add(Estimate(_housing, "First month", 1500.50m, "EUR"));
        _furnishing.Estimates.Add(Estimate(_furnishing, "Sofa", 800, "EUR"));

        var response = BudgetComparison.Build("EUR", [_furnishing, _housing], [], null);

        var housing = Assert.Single(response.Categories, category => category.Name == "Housing");
        Assert.Equal(3500.50m, housing.Estimated);
        Assert.Equal(["Deposit", "First month"], housing.Estimates.Select(estimate => estimate.Label));
        Assert.Equal(4300.50m, response.Totals.Estimated);
        Assert.Equal(["Furnishing", "Housing"], response.Categories.Select(category => category.Name));
        Assert.Null(response.RatesAsOf);
        Assert.False(response.RatesUnavailable);
    }

    [Fact]
    public void Remaining_is_the_estimate_minus_what_was_spent()
    {
        _housing.Estimates.Add(Estimate(_housing, "Deposit", 2000, "EUR"));
        _furnishing.Estimates.Add(Estimate(_furnishing, "Sofa", 800, "EUR"));

        var response = BudgetComparison.Build(
            "EUR",
            [_housing, _furnishing],
            [
                Spend(_housing, 1800, "EUR"),
                Spend(_housing, 300, "EUR"),
                Spend(_furnishing, 250.25m, "EUR"),
            ],
            null);

        var housing = response.Categories.Single(category => category.Name == "Housing");
        Assert.Equal(2100m, housing.Actual);
        Assert.Equal(-100m, housing.Remaining);
        Assert.Equal(2, housing.ExpenseCount);
        Assert.Equal(549.75m, response.Categories.Single(category => category.Name == "Furnishing").Remaining);
        Assert.Equal(2350.25m, response.Totals.Actual);
        Assert.Equal(449.75m, response.Totals.Remaining);
    }

    [Fact]
    public void Amounts_in_other_currencies_are_converted_into_the_budget_currency()
    {
        _housing.Estimates.Add(Estimate(_housing, "Visa", 300_000, "CLP"));
        _furnishing.Estimates.Add(Estimate(_furnishing, "Sofa", 800, "NZD"));

        var response = BudgetComparison.Build(
            "CLP",
            [_housing, _furnishing],
            [
                Spend(_housing, 100_000, "CLP"),
                Spend(_furnishing, 100, "NZD"),
                Spend(_furnishing, 20, "EUR"),
            ],
            ClpRates);

        var sofa = response.Categories.Single(category => category.Name == "Furnishing");
        Assert.Equal(400_000m, sofa.Estimated);
        Assert.Equal(50_000m + 20_000m, sofa.Actual);
        Assert.Equal(330_000m, sofa.Remaining);
        Assert.Equal(2, sofa.ExpenseCount);
        var estimate = Assert.Single(sofa.Estimates);
        Assert.Equal(800m, estimate.Amount);
        Assert.Equal("NZD", estimate.CurrencyCode);
        Assert.Equal(400_000m, estimate.ConvertedAmount);
        Assert.Equal(700_000m, response.Totals.Estimated);
        Assert.Equal(170_000m, response.Totals.Actual);
        Assert.Equal(530_000m, response.Totals.Remaining);
        Assert.Empty(response.Unconverted);
        Assert.Equal(ClpRates.UpdatedAt, response.RatesAsOf);
        Assert.False(response.RatesUnavailable);
    }

    [Fact]
    public void An_amount_without_a_rate_is_left_out_and_listed_by_currency()
    {
        _housing.Estimates.Add(Estimate(_housing, "Deposit", 1000, "CLP"));
        _housing.Estimates.Add(Estimate(_housing, "Storage", 90, "XTS"));

        var response = BudgetComparison.Build(
            "CLP",
            [_housing],
            [Spend(_housing, 500, "CLP"), Spend(_housing, 40, "XTS"), Spend(_housing, 60, "XTS")],
            ClpRates);

        Assert.Equal(1000m, response.Totals.Estimated);
        Assert.Equal(500m, response.Totals.Actual);
        Assert.Equal(1, response.Categories[0].ExpenseCount);
        Assert.Null(response.Categories[0].Estimates.Single(estimate => estimate.Label == "Storage").ConvertedAmount);
        var row = Assert.Single(response.Unconverted);
        Assert.Equal(("XTS", 2, 100m, 1, 90m), (row.CurrencyCode, row.ExpenseCount, row.ExpenseTotal, row.EstimateCount, row.EstimateTotal));
    }

    [Fact]
    public void Without_any_rates_only_the_budget_currency_counts_and_the_gap_is_reported()
    {
        _housing.Estimates.Add(Estimate(_housing, "Deposit", 1000, "CLP"));

        var response = BudgetComparison.Build(
            "CLP",
            [_housing],
            [Spend(_housing, 500, "CLP"), Spend(_housing, 40, "NZD")],
            null);

        Assert.Equal(500m, response.Totals.Actual);
        Assert.True(response.RatesUnavailable);
        Assert.Null(response.RatesAsOf);
        Assert.Equal(["NZD"], response.Unconverted.Select(row => row.CurrencyCode));
    }

    [Fact]
    public void Spending_in_a_category_without_estimates_still_shows_as_actual()
    {
        var response = BudgetComparison.Build("EUR", [_housing], [Spend(_housing, 75, "EUR")], null);

        Assert.Equal(0m, response.Categories[0].Estimated);
        Assert.Equal(75m, response.Categories[0].Actual);
        Assert.Equal(-75m, response.Categories[0].Remaining);
    }

    [Fact]
    public void Foreign_currencies_are_those_used_outside_the_budget_currency()
    {
        _housing.Estimates.Add(Estimate(_housing, "Deposit", 1000, "CLP"));
        _housing.Estimates.Add(Estimate(_housing, "Sofa", 10, "NZD"));

        var currencies = BudgetComparison.ForeignCurrencies("CLP", [_housing], [Spend(_housing, 5, "EUR")]);

        Assert.Equal(["EUR", "NZD"], currencies.Order(StringComparer.Ordinal));
        Assert.Empty(BudgetComparison.ForeignCurrencies("CLP", [], []));
    }

    private static BudgetCategory Category(string name)
    {
        return new BudgetCategory { RelocationId = RelocationId, Name = name };
    }

    private static BudgetEstimate Estimate(BudgetCategory category, string label, decimal amount, string currency)
    {
        return new BudgetEstimate
        {
            RelocationId = RelocationId,
            CategoryId = category.Id,
            Label = label,
            Amount = amount,
            CurrencyCode = currency,
        };
    }

    private static Expense Spend(BudgetCategory category, decimal amount, string currency)
    {
        return new Expense
        {
            RelocationId = RelocationId,
            CategoryId = category.Id,
            Description = "Paid",
            Amount = amount,
            CurrencyCode = currency,
            PaidOn = new DateOnly(2026, 11, 1),
        };
    }
}
