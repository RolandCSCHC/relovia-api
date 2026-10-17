namespace Relovia.Api.Features.ExchangeRates;

/// <summary>A source of current exchange rates. Nothing outside this folder knows which one is used.</summary>
public interface IExchangeRateProvider
{
    /// <summary>Returns the latest rates for the base currency, or null when the provider does not support it.</summary>
    Task<ExchangeRateTable?> GetLatestAsync(string baseCode, CancellationToken cancellationToken);
}
