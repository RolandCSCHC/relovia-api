using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class RelocationTests
{
    [Fact]
    public void Same_country_code_is_domestic_even_when_the_cities_differ()
    {
        var relocation = new Relocation
        {
            OriginCountryCode = " us ",
            OriginCity = "Austin",
            DestinationCountryCode = "US",
            DestinationCity = "Seattle",
        };

        Assert.False(relocation.IsInternational);
    }

    [Fact]
    public void Different_country_codes_are_international()
    {
        var relocation = new Relocation
        {
            OriginCountryCode = "US",
            OriginCity = "Austin",
            DestinationCountryCode = "de",
            DestinationCity = "Berlin",
        };

        Assert.True(relocation.IsInternational);
    }
}
