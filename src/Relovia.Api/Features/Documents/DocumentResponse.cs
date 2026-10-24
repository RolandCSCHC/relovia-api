namespace Relovia.Api.Features.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt)
{
    public static DocumentResponse From(Document document)
    {
        return new DocumentResponse(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.CreatedAt);
    }
}

/// <summary>Bytes opened only after the caller was allowed to see this document.</summary>
public sealed record DocumentContent(Stream Content, string ContentType, string FileName);
