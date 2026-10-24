namespace Relovia.Api.Features.Documents;

/// <summary>
/// Private bytes for one document. The key is an opaque id, never a path chosen by the caller.
/// </summary>
public interface IObjectStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>The object's bytes, or null when the key is missing. The caller disposes the stream.</summary>
    Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
