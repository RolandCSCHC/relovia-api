using Relovia.Api.Features.Housing;

namespace Relovia.Api.UnitTests;

public sealed class HousingRulesTests
{
    [Fact]
    public void A_home_can_omit_money_and_keeps_a_trimmed_name()
    {
        var errors = HousingRules.Validate(new SaveHousingOptionRequest
        {
            Name = " Kreuzberg flat ",
            Area = "  ",
            Status = HousingStatus.Considering,
            Notes = "  ",
        });

        Assert.Empty(errors);
        Assert.Equal("Kreuzberg flat", HousingRules.NormalizeName(" Kreuzberg flat "));
        Assert.Null(HousingRules.NormalizeOptional("  "));
        Assert.Equal("EUR", HousingRules.NormalizeCurrency(" eur "));
    }

    [Fact]
    public void Rent_and_deposit_need_a_currency_and_a_positive_amount()
    {
        var missingCurrency = HousingRules.Validate(new SaveHousingOptionRequest
        {
            Name = "Kreuzberg flat",
            Rent = 1200m,
            Status = HousingStatus.Considering,
        });
        var currencyOnly = HousingRules.Validate(new SaveHousingOptionRequest
        {
            Name = "Kreuzberg flat",
            CurrencyCode = "EUR",
            Status = HousingStatus.Considering,
        });
        var zero = HousingRules.Validate(new SaveHousingOptionRequest
        {
            Name = "Kreuzberg flat",
            Deposit = 0m,
            CurrencyCode = "EUR",
            Status = HousingStatus.Considering,
        });
        var tooFine = HousingRules.Validate(new SaveHousingOptionRequest
        {
            Name = "Kreuzberg flat",
            Rent = 1.23456m,
            CurrencyCode = "EUR",
            Status = HousingStatus.Considering,
        });

        Assert.Equal([HousingRules.CurrencyRequiredMessage], missingCurrency["currencyCode"]);
        Assert.Equal([HousingRules.CurrencyWithoutMoneyMessage], currencyOnly["currencyCode"]);
        Assert.Equal([HousingRules.AmountPositiveMessage], zero["deposit"]);
        Assert.Equal([HousingRules.AmountScaleMessage], tooFine["rent"]);
    }

    [Fact]
    public void A_home_needs_a_name_and_a_known_status()
    {
        var errors = HousingRules.Validate(new SaveHousingOptionRequest
        {
            Name = " ",
            Status = (HousingStatus)100,
            Furnishing = (HousingFurnishing)100,
        });

        Assert.Equal(["Name is required."], errors["name"]);
        Assert.Equal([HousingRules.UnknownStatusMessage], errors["status"]);
        Assert.Equal([HousingRules.UnknownFurnishingMessage], errors["furnishing"]);
    }
}
