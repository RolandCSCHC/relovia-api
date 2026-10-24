using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Documents;

/// <summary>
/// Metadata for one private file on a relocation. The bytes live in <see cref="IObjectStorage"/>
/// under <see cref="StorageKey"/>, which is never the name the user chose.
/// </summary>
public sealed class Document : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DocumentCategory? Category { get; set; }

    public DateOnly? ExpiresOn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;

    public ICollection<DocumentTaskLink> Links { get; } = new List<DocumentTaskLink>();
}
