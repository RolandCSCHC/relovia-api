namespace Relovia.Api.Features.Places;

public sealed class CityListFiles(Stream cities, Stream regions, IDisposable? extra = null) : IAsyncDisposable
{
    public Stream Cities { get; } = cities;

    public Stream Regions { get; } = regions;

    public async ValueTask DisposeAsync()
    {
        await Cities.DisposeAsync();
        await Regions.DisposeAsync();
        extra?.Dispose();
    }
}
