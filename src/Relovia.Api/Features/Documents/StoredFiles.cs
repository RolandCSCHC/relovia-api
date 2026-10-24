namespace Relovia.Api.Features.Documents;

internal static class StoredFiles
{
    public static async Task DeleteAsync(
        IObjectStorage storage,
        ILogger logger,
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        foreach (var key in keys)
        {
            try
            {
                await storage.DeleteAsync(key, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "A stored document could not be deleted.");
            }
        }
    }
}
