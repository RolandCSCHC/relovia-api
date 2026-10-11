using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Relocations;

public sealed class ChecklistTemplate : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Code { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public string Title { get; set; } = string.Empty;

    public ChecklistScope Scope { get; set; }

    public string? DestinationCountryCode { get; set; }

    public RelocationPurpose? Purpose { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<TaskTemplate> Tasks { get; } = new List<TaskTemplate>();

    public bool IsPublished => PublishedAt is not null;
}
