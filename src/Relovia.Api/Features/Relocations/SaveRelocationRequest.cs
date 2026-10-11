using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Relocations;

public sealed class SaveRelocationRequest
{
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    [JsonRequired]
    public string OriginCountryCode { get; set; } = string.Empty;

    [JsonRequired]
    public string OriginCity { get; set; } = string.Empty;

    [JsonRequired]
    public string DestinationCountryCode { get; set; } = string.Empty;

    [JsonRequired]
    public string DestinationCity { get; set; } = string.Empty;

    [JsonRequired]
    public DateOnly TargetDate { get; set; }

    [JsonRequired]
    public RelocationPurpose Purpose { get; set; }

    [JsonRequired]
    public RelocationStatus Status { get; set; }
}
