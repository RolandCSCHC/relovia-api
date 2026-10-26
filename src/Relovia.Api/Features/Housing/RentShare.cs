using Relovia.Api.Features.ExchangeRates;

namespace Relovia.Api.Features.Housing;

/// <summary>
/// Turns a home's rent into a share of monthly net income. The percentage is calculated when the
/// home is read and is not stored.
/// </summary>
public static class RentShare
{
    public const int PercentScale = 1;

    /// <summary>The monthly amount, or the weekly amount × 52 ÷ 12.</summary>
    public static decimal MonthlyAmount(decimal rent, RentPeriod period)
    {
        return period == RentPeriod.Weekly ? rent * 52m / 12m : rent;
    }

    /// <summary>
    /// Monthly rent as a percentage of income, to one decimal place. The same currency divides
    /// directly. A different currency converts the monthly rent into the income currency. Returns
    /// null when that rate is missing.
    /// </summary>
    public static decimal? PercentOfIncome(
        decimal rent,
        RentPeriod period,
        string rentCurrency,
        decimal income,
        string incomeCurrency,
        ExchangeRateTable? rates)
    {
        if (income <= 0)
        {
            return null;
        }

        var monthly = MonthlyAmount(rent, period);
        decimal inIncomeCurrency;
        if (string.Equals(rentCurrency, incomeCurrency, StringComparison.Ordinal))
        {
            inIncomeCurrency = monthly;
        }
        else if (rates is not null && rates.TryConvert(monthly, rentCurrency, out var converted))
        {
            inIncomeCurrency = converted;
        }
        else
        {
            return null;
        }

        return decimal.Round(inIncomeCurrency / income * 100m, PercentScale, MidpointRounding.AwayFromZero);
    }
}
