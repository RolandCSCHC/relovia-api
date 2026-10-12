namespace Relovia.Api.Features.Relocations;

public sealed record RelocationResponse(
    Guid Id,
    string Name,
    string OriginCountryCode,
    string OriginCity,
    string DestinationCountryCode,
    string DestinationCity,
    DateOnly? TargetDate,
    RelocationPurpose Purpose,
    RelocationStatus Status,
    bool IsInternational,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static RelocationResponse From(Relocation relocation)
    {
        return new RelocationResponse(
            relocation.Id,
            relocation.Name,
            relocation.OriginCountryCode,
            relocation.OriginCity,
            relocation.DestinationCountryCode,
            relocation.DestinationCity,
            relocation.TargetDate,
            relocation.Purpose,
            relocation.Status,
            relocation.IsInternational,
            relocation.CreatedAt,
            relocation.UpdatedAt);
    }
}
