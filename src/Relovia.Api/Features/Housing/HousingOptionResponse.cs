namespace Relovia.Api.Features.Housing;

public sealed record HousingOptionResponse(
    Guid Id,
    string Name,
    string? Area,
    decimal? Rent,
    decimal? Deposit,
    string? CurrencyCode,
    string? Size,
    HousingFurnishing? Furnishing,
    DateOnly? AvailableOn,
    HousingStatus Status,
    string? Notes)
{
    public static HousingOptionResponse From(HousingOption option)
    {
        return new HousingOptionResponse(
            option.Id,
            option.Name,
            option.Area,
            option.Rent,
            option.Deposit,
            option.CurrencyCode,
            option.Size,
            option.Furnishing,
            option.AvailableOn,
            option.Status,
            option.Notes);
    }
}
