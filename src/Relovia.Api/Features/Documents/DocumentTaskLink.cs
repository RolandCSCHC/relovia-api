using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Documents;

/// <summary>
/// One task on this relocation that the document supports. The task and the document share a relocation.
/// </summary>
public sealed class DocumentTaskLink
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public Guid DocumentId { get; set; }

    public Guid TaskId { get; set; }

    public Document Document { get; set; } = null!;

    public RelocationTask Task { get; set; } = null!;
}
