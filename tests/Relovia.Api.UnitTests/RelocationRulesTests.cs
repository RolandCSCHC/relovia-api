using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class RelocationRulesTests
{
    [Fact]
    public void Known_purpose_and_status_are_accepted()
    {
        var errors = RelocationRules.Validate(ValidRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void Destination_city_and_target_date_can_be_left_unset()
    {
        var request = ValidRequest();
        request.DestinationCity = "   ";
        request.TargetDate = null;

        var errors = RelocationRules.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Blank_text_and_a_bad_country_code_are_rejected()
    {
        var request = ValidRequest();
        request.Name = "   ";
        request.OriginCountryCode = "USA";
        request.DestinationCity = new string('a', RelocationRules.CityMaxLength + 1);

        var errors = RelocationRules.Validate(request);

        Assert.Equal("Name is required.", Assert.Single(errors["name"]));
        Assert.Equal("Enter a two-letter country code.", Assert.Single(errors["originCountryCode"]));
        Assert.Equal(
            $"Destination city must be {RelocationRules.CityMaxLength} characters or fewer.",
            Assert.Single(errors["destinationCity"]));
    }

    [Theory]
    [InlineData((RelocationPurpose)100, RelocationStatus.Draft, "purpose")]
    [InlineData(RelocationPurpose.Work, (RelocationStatus)100, "status")]
    public void Undefined_purpose_and_status_are_rejected(
        RelocationPurpose purpose,
        RelocationStatus status,
        string key)
    {
        var request = ValidRequest();
        request.Purpose = purpose;
        request.Status = status;

        var errors = RelocationRules.Validate(request);

        Assert.True(errors.ContainsKey(key));
    }

    private static SaveRelocationRequest ValidRequest()
    {
        return new SaveRelocationRequest
        {
            Name = "Summer move",
            OriginCountryCode = "US",
            OriginCity = "Austin",
            DestinationCountryCode = "DE",
            DestinationCity = "Berlin",
            TargetDate = new DateOnly(2026, 8, 1),
            Purpose = RelocationPurpose.Work,
            Status = RelocationStatus.Active,
        };
    }
}
