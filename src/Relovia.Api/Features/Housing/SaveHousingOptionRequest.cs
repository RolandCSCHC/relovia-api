using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Housing;

public sealed class SaveHousingOptionRequest
{
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    public string? Area { get; set; }

    public decimal? Rent { get; set; }

    public RentPeriod? RentPeriod { get; set; }

    public decimal? Deposit { get; set; }

    public string? CurrencyCode { get; set; }

    public string? Size { get; set; }

    public HousingFurnishing? Furnishing { get; set; }

    public DateOnly? AvailableOn { get; set; }

    [JsonRequired]
    public HousingStatus Status { get; set; }

    public string? Notes { get; set; }
}
