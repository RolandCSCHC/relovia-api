using System.Globalization;
using System.Text.Json;

namespace Relovia.Api.Features.ExchangeRates;

/// <summary>
/// Rates from open.er-api.com: free, no key, one update a day, latest rates only. Its terms ask for
/// attribution to exchangerate-api.com wherever the rates are shown.
/// </summary>
public sealed class OpenErApiRateProvider(HttpClient http) : IExchangeRateProvider
{
    public async Task<ExchangeRateTable?> GetLatestAsync(string baseCode, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(
            $"v6/latest/{Uri.EscapeDataString(baseCode)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return Parse(document.RootElement, baseCode);
    }

    public static ExchangeRateTable? Parse(JsonElement root, string baseCode)
    {
        if (root.TryGetProperty("result", out var result) && result.GetString() != "success")
        {
            return null;
        }

        var rates = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var rate in root.GetProperty("rates").EnumerateObject())
        {
            if (rate.Value.ValueKind == JsonValueKind.Number
                && rate.Value.TryGetDecimal(out var value)
                && value > 0)
            {
                rates[rate.Name] = value;
            }
        }

        var updatedAt = root.TryGetProperty("time_last_update_unix", out var unix) && unix.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : DateTimeOffset.UtcNow;

        return new ExchangeRateTable(baseCode.ToUpper(CultureInfo.InvariantCulture), rates, updatedAt);
    }
}
