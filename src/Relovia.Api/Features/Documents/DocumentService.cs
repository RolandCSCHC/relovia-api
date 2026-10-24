using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Documents;

public sealed class DocumentService(ReloviaDbContext db, IObjectStorage storage, ILogger<DocumentService> logger)
{
    public const string RelocationNotFoundTitle = "Relocation was not found.";

    public const string DocumentNotFoundTitle = "Document was not found.";

    public async Task<DocumentResult<IReadOnlyList<DocumentResponse>>> ListAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new DocumentResult<IReadOnlyList<DocumentResponse>>.NotFound(RelocationNotFoundTitle);
        }

        var documents = await db.Documents.AsNoTracking()
            .Where(document => document.RelocationId == relocationId)
            .OrderBy(document => document.CreatedAt)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return new DocumentResult<IReadOnlyList<DocumentResponse>>.Success(
            documents.Select(DocumentResponse.From).ToArray());
    }

    public async Task<DocumentResult<DocumentResponse>> CreateAsync(
        Guid userId,
        Guid relocationId,
        string? fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new DocumentResult<DocumentResponse>.NotFound(RelocationNotFoundTitle);
        }

        var prepared = await DocumentRules.PrepareAsync(content, fileName, cancellationToken);
        if (prepared is PreparedDocument.Rejected rejected)
        {
            return new DocumentResult<DocumentResponse>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = [rejected.Message],
            });
        }

        var accepted = (PreparedDocument.Accepted)prepared;
        var document = new Document
        {
            Id = Guid.CreateVersion7(),
            RelocationId = relocationId,
            FileName = accepted.FileName,
            ContentType = accepted.ContentType,
            SizeBytes = accepted.Content.LongLength,
        };
        document.StorageKey = document.Id.ToString("N");

        await using (var stream = new MemoryStream(accepted.Content))
        {
            await storage.SaveAsync(document.StorageKey, stream, document.ContentType, cancellationToken);
        }

        db.Documents.Add(document);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await StoredFiles.DeleteAsync(storage, logger, [document.StorageKey], CancellationToken.None);
            throw;
        }

        return new DocumentResult<DocumentResponse>.Success(DocumentResponse.From(document));
    }

    /// <summary>
    /// Opens the file only when the signed-in user owns the relocation. A missing or foreign document
    /// never touches storage.
    /// </summary>
    public async Task<DocumentResult<DocumentContent>> OpenAsync(
        Guid userId,
        Guid relocationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new DocumentResult<DocumentContent>.NotFound(RelocationNotFoundTitle);
        }

        var document = await FindOwnedAsync(userId, relocationId, documentId, tracking: false, cancellationToken);
        if (document is null)
        {
            return new DocumentResult<DocumentContent>.NotFound(DocumentNotFoundTitle);
        }

        var stream = await storage.OpenAsync(document.StorageKey, cancellationToken);
        if (stream is null)
        {
            return new DocumentResult<DocumentContent>.NotFound(DocumentNotFoundTitle);
        }

        return new DocumentResult<DocumentContent>.Success(
            new DocumentContent(stream, document.ContentType, document.FileName));
    }

    public async Task<DocumentResult<Guid>> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new DocumentResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var document = await FindOwnedAsync(userId, relocationId, documentId, tracking: true, cancellationToken);
        if (document is null)
        {
            return new DocumentResult<Guid>.NotFound(DocumentNotFoundTitle);
        }

        var key = document.StorageKey;
        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        await StoredFiles.DeleteAsync(storage, logger, [key], cancellationToken);
        return new DocumentResult<Guid>.Success(document.Id);
    }

    public async Task<IReadOnlyList<string>> StorageKeysAsync(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Documents
            .Where(document => document.RelocationId == relocationId)
            .Select(document => document.StorageKey)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> OwnsRelocationAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AnyAsync(
            relocation => relocation.Id == relocationId && relocation.UserId == userId,
            cancellationToken);
    }

    private Task<Document?> FindOwnedAsync(
        Guid userId,
        Guid relocationId,
        Guid documentId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = tracking ? db.Documents : db.Documents.AsNoTracking();
        return query.SingleOrDefaultAsync(
            document => document.Id == documentId
                && document.RelocationId == relocationId
                && document.Relocation.UserId == userId,
            cancellationToken);
    }
}
