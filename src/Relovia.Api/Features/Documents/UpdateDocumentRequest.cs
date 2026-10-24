using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Documents;

public sealed class UpdateDocumentRequest
{
    public DocumentCategory? Category { get; set; }

    public DateOnly? ExpiresOn { get; set; }

    [JsonRequired]
    public IReadOnlyList<Guid>? TaskIds { get; set; }
}
