using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Relovia.Api.Features.ExchangeRates;

/// <summary>
/// Hands out rates from a cache. A fresh entry is reused. When a refresh fails, the last rates are
/// served instead, so a provider outage does not break the budget. With nothing cached the result
/// is null and the caller shows amounts in other currencies as not converted.
/// </summary>
public sealed class ExchangeRateService(
    IExchangeRateProvider provider,
    IMemoryCache cache,
    TimeProvider time,
    ILogger<ExchangeRateService> logger)
{
    public static readonly TimeSpan FreshFor = TimeSpan.FromHours(6);

    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(7);

    public async Task<ExchangeRateTable?> GetAsync(string baseCode, CancellationToken cancellationToken)
    {
        var key = $"exchange-rates:{baseCode}";
        var now = time.GetUtcNow();
        cache.TryGetValue(key, out CachedRates? cached);
        if (cached is not null && now - cached.FetchedAt < FreshFor)
        {
            return cached.Table;
        }

        try
        {
            var table = await provider.GetLatestAsync(baseCode, cancellationToken);
            if (table is not null)
            {
                cache.Set(key, new CachedRates(table, now), KeepFor);
                return table;
            }

            logger.LogWarning("The exchange rate provider does not support {BaseCode}.", baseCode);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or TaskCanceledException or JsonException
                or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            logger.LogWarning(exception, "Exchange rates for {BaseCode} could not be refreshed.", baseCode);
        }

        return cached?.Table;
    }

    private sealed record CachedRates(ExchangeRateTable Table, DateTimeOffset FetchedAt);
}
