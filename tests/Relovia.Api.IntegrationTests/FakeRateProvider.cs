using Relovia.Api.Features.ExchangeRates;

namespace Relovia.Api.IntegrationTests;

/// <summary>Fixed rates so tests never call the network: 1 USD = 0.5 EUR = 1000 CLP = 2 NZD.</summary>
public sealed class FakeRateProvider : IExchangeRateProvider
{
    public static readonly DateTimeOffset UpdatedAt = new(2026, 10, 17, 0, 0, 0, TimeSpan.Zero);

    private static readonly Dictionary<string, decimal> PerUsd = new(StringComparer.Ordinal)
    {
        ["USD"] = 1m,
        ["EUR"] = 0.5m,
        ["CLP"] = 1000m,
        ["NZD"] = 2m,
    };

    public Task<ExchangeRateTable?> GetLatestAsync(string baseCode, CancellationToken cancellationToken)
    {
        if (!PerUsd.TryGetValue(baseCode, out var basePerUsd))
        {
            return Task.FromResult<ExchangeRateTable?>(null);
        }

        var rates = PerUsd.ToDictionary(pair => pair.Key, pair => pair.Value / basePerUsd, StringComparer.Ordinal);
        return Task.FromResult<ExchangeRateTable?>(new ExchangeRateTable(baseCode, rates, UpdatedAt));
    }
}
