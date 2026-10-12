using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Relovia.Api.Features.Places;

public sealed class CityCatalog(ICityListSource source, IMemoryCache cache, ILogger<CityCatalog> logger)
{
    private const string CacheKey = "cities:catalog";

    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<IReadOnlyList<string>> ListAsync(string countryCode, CancellationToken cancellationToken)
    {
        var country = countryCode.Trim().ToUpperInvariant();
        var catalog = await CatalogAsync(cancellationToken);
        return catalog.TryGetValue(country, out var cities) ? cities : [];
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> CatalogAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, IReadOnlyList<string>>? cached) && cached is not null)
        {
            return cached;
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(CacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            try
            {
                var catalog = await LoadAsync(cancellationToken);
                cache.Set(CacheKey, catalog, TimeSpan.FromHours(24));
                logger.LogInformation("Loaded city names for {Countries} countries.", catalog.Count);
                return catalog;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException or InvalidDataException)
            {
                logger.LogWarning(exception, "City list download failed.");
                throw new CityCatalogException("City names could not be loaded.");
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> LoadAsync(CancellationToken cancellationToken)
    {
        await using var files = await source.OpenAsync(cancellationToken);
        using var cities = new StreamReader(files.Cities, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var regions = new StreamReader(files.Regions, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var catalog = CityNames.ByCountry(cities, regions);
        if (catalog.Count == 0)
        {
            throw new InvalidDataException("City list was empty.");
        }

        return catalog;
    }
}
