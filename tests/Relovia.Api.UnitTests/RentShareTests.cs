using Relovia.Api.Features.ExchangeRates;
using Relovia.Api.Features.Housing;

namespace Relovia.Api.UnitTests;

public sealed class RentShareTests
{
    private static readonly ExchangeRateTable EurRates = new(
        "EUR",
        new Dictionary<string, decimal> { ["EUR"] = 1m, ["USD"] = 2m },
        new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void The_same_currency_divides_the_monthly_rent_directly()
    {
        var percent = RentShare.PercentOfIncome(1200m, RentPeriod.Monthly, "EUR", 4000m, "EUR", rates: null);

        Assert.Equal(30.0m, percent);
    }

    [Fact]
    public void A_weekly_rent_uses_fifty_two_weeks_over_twelve_months()
    {
        var percent = RentShare.PercentOfIncome(300m, RentPeriod.Weekly, "EUR", 4000m, "EUR", rates: null);

        Assert.Equal(1300m, RentShare.MonthlyAmount(300m, RentPeriod.Weekly));
        Assert.Equal(32.5m, percent);
    }

    [Fact]
    public void A_different_currency_converts_into_the_income_currency()
    {
        var percent = RentShare.PercentOfIncome(300m, RentPeriod.Weekly, "USD", 4000m, "EUR", EurRates);

        Assert.Equal(16.3m, percent);
    }

    [Fact]
    public void A_missing_rate_leaves_the_percentage_out()
    {
        var percent = RentShare.PercentOfIncome(100m, RentPeriod.Monthly, "XTS", 4000m, "EUR", EurRates);

        Assert.Null(percent);
    }

    [Fact]
    public void The_percentage_is_rounded_to_one_decimal_place()
    {
        var percent = RentShare.PercentOfIncome(100m, RentPeriod.Weekly, "EUR", 3000m, "EUR", rates: null);

        Assert.Equal(14.4m, percent);
    }
}
