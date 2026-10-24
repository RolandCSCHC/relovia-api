using Relovia.Api.Features.Documents;

namespace Relovia.Api.UnitTests;

public sealed class LocalObjectStorageTests
{
    [Fact]
    public async Task A_document_round_trips_and_a_second_delete_is_safe()
    {
        var root = NewRoot();
        try
        {
            var storage = new LocalObjectStorage(root);
            var key = Guid.NewGuid().ToString("N");
            var bytes = "%PDF-1.4\n"u8.ToArray();
            await storage.SaveAsync(key, new MemoryStream(bytes), "application/pdf", CancellationToken.None);

            var opened = await storage.OpenAsync(key, CancellationToken.None);
            Assert.NotNull(opened);
            await using (opened)
            {
                using var copy = new MemoryStream();
                await opened.CopyToAsync(copy);
                Assert.Equal(bytes, copy.ToArray());
            }

            await storage.DeleteAsync(key, CancellationToken.None);
            await storage.DeleteAsync(key, CancellationToken.None);
            Assert.Null(await storage.OpenAsync(key, CancellationToken.None));
            Assert.Empty(Directory.GetFiles(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task A_key_cannot_leave_the_storage_directory()
    {
        var root = NewRoot();
        var escaped = "escape-" + Guid.NewGuid().ToString("N");
        try
        {
            var storage = new LocalObjectStorage(root);
            await using var content = new MemoryStream("%PDF-1.4\n"u8.ToArray());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                storage.SaveAsync("../" + escaped, content, "application/pdf", CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                storage.OpenAsync("not-a-key", CancellationToken.None));

            Assert.False(File.Exists(Path.Combine(Directory.GetParent(root)!.FullName, escaped)));
            Assert.Empty(Directory.GetFiles(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "relovia-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
