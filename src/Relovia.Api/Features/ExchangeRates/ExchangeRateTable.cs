namespace Relovia.Api.Features.ExchangeRates;

/// <summary>
/// Rates for one base currency. <see cref="Rates"/> says how many units of each currency one unit
/// of the base currency buys, so converting into the base divides by the rate.
/// </summary>
public sealed record ExchangeRateTable(
    string BaseCode,
    IReadOnlyDictionary<string, decimal> Rates,
    DateTimeOffset UpdatedAt)
{
    public const int Scale = 4;

    /// <summary>Converts an amount in <paramref name="fromCode"/> into the base currency.</summary>
    public bool TryConvert(decimal amount, string fromCode, out decimal converted)
    {
        if (string.Equals(fromCode, BaseCode, StringComparison.Ordinal))
        {
            converted = amount;
            return true;
        }

        if (Rates.TryGetValue(fromCode, out var rate) && rate > 0)
        {
            converted = decimal.Round(amount / rate, Scale, MidpointRounding.AwayFromZero);
            return true;
        }

        converted = 0;
        return false;
    }
}
