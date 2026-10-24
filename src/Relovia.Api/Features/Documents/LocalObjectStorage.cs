namespace Relovia.Api.Features.Documents;

/// <summary>
/// Writes each object as one file under a private directory. The directory is not served as static files.
/// </summary>
public sealed class LocalObjectStorage : IObjectStorage
{
    private readonly string _root;

    public LocalObjectStorage(string root)
    {
        _root = Path.GetFullPath(root);
    }

    public async Task SaveAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(_root);
        var temporary = path + ".uploading";
        await using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(temporary, path, overwrite: true);
    }

    public Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var temporary = path + ".uploading";
        if (File.Exists(temporary))
        {
            File.Delete(temporary);
        }

        return Task.CompletedTask;
    }

    private string PathFor(string key)
    {
        ObjectKeys.Require(key);
        var path = Path.GetFullPath(Path.Combine(_root, key));
        var prefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The storage key is not a document id.", nameof(key));
        }

        return path;
    }
}
