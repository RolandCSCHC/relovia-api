using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relovia.Api.Features.Places;

namespace Relovia.Api.UnitTests;

public sealed class CityCatalogTests
{
    [Fact]
    public void Duplicate_city_names_include_the_region_and_are_sorted()
    {
        const string cities = """
            1	Springfield	Springfield		0	0	P	PPL	US		MO
            2	Berlin	Berlin		0	0	P	PPL	DE		BE
            3	Springfield	Springfield		0	0	P	PPL	US		IL
            """;
        const string regions = """
            US.IL	Illinois
            US.MO	Missouri
            DE.BE	Berlin
            """;

        var catalog = CityNames.ByCountry(new StringReader(cities), new StringReader(regions));

        Assert.Equal(["Springfield, Illinois", "Springfield, Missouri"], catalog["US"]);
        Assert.Equal(["Berlin"], catalog["DE"]);
    }

    [Fact]
    public async Task The_list_is_downloaded_once_and_then_cached()
    {
        var source = new MemorySource(
            """
            1	Zurich	Zurich		0	0	P	PPL	CH		ZH
            2	Bern	Bern		0	0	P	PPL	CH		BE
            """,
            """
            CH.ZH	Zurich
            CH.BE	Bern
            """);
        var catalog = CreateCatalog(source);

        var first = await catalog.ListAsync("ch", CancellationToken.None);
        var second = await catalog.ListAsync("CH", CancellationToken.None);

        Assert.Equal(["Bern", "Zurich"], first);
        Assert.Equal(first, second);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task A_failed_download_is_not_cached()
    {
        var source = new MemorySource(string.Empty, string.Empty, new HttpRequestException("offline"));
        var catalog = CreateCatalog(source);

        var error = await Assert.ThrowsAsync<CityCatalogException>(() =>
            catalog.ListAsync("DE", CancellationToken.None));
        await Assert.ThrowsAsync<CityCatalogException>(() =>
            catalog.ListAsync("DE", CancellationToken.None));

        Assert.Equal("City names could not be loaded.", error.Message);
        Assert.Equal(2, source.Calls);
    }

    private static CityCatalog CreateCatalog(MemorySource source)
    {
        return new CityCatalog(source, new MemoryCache(new MemoryCacheOptions()), NullLogger<CityCatalog>.Instance);
    }

    private sealed class MemorySource(string cities, string regions, Exception? failure = null) : ICityListSource
    {
        public int Calls { get; private set; }

        public Task<CityListFiles> OpenAsync(CancellationToken cancellationToken)
        {
            Calls++;
            if (failure is not null)
            {
                throw failure;
            }

            return Task.FromResult(new CityListFiles(
                new MemoryStream(Encoding.UTF8.GetBytes(cities)),
                new MemoryStream(Encoding.UTF8.GetBytes(regions))));
        }
    }
}
