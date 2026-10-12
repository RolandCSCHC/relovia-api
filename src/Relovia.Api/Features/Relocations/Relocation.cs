using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Features.Relocations;

public sealed class Relocation : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OriginCountryCode { get; set; } = string.Empty;

    public string OriginCity { get; set; } = string.Empty;

    public string DestinationCountryCode { get; set; } = string.Empty;

    public string DestinationCity { get; set; } = string.Empty;

    public DateOnly? TargetDate { get; set; }

    public RelocationPurpose Purpose { get; set; }

    public RelocationStatus Status { get; set; } = RelocationStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<RelocationTask> Tasks { get; } = new List<RelocationTask>();

    public bool IsInternational =>
        !CountryCodes.AreSameCountry(OriginCountryCode, DestinationCountryCode);
}
