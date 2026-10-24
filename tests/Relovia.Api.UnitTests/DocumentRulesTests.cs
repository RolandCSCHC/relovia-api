using Relovia.Api.Features.Documents;

namespace Relovia.Api.UnitTests;

public sealed class DocumentRulesTests
{
    [Fact]
    public async Task A_pdf_jpeg_png_or_webp_is_accepted_and_other_bytes_are_rejected()
    {
        var pdf = await DocumentRules.PrepareAsync(StreamOf("%PDF-1.4\n"u8), "visa.pdf", CancellationToken.None);
        var jpeg = await DocumentRules.PrepareAsync(StreamOf([0xFF, 0xD8, 0xFF, 0xE0]), "photo.jpg", CancellationToken.None);
        var png = await DocumentRules.PrepareAsync(
            StreamOf([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]),
            "scan.png",
            CancellationToken.None);
        var webp = await DocumentRules.PrepareAsync(StreamOf("RIFF\0\0\0\0WEBP"u8), "scan.webp", CancellationToken.None);
        var html = await DocumentRules.PrepareAsync(StreamOf("<html></html>"u8), "page.pdf", CancellationToken.None);
        var empty = await DocumentRules.PrepareAsync(StreamOf([]), "empty.pdf", CancellationToken.None);

        Assert.Equal("application/pdf", Accepted(pdf).ContentType);
        Assert.Equal("image/jpeg", Accepted(jpeg).ContentType);
        Assert.Equal("image/png", Accepted(png).ContentType);
        Assert.Equal("image/webp", Accepted(webp).ContentType);
        Assert.Equal("Upload a PDF, JPEG, PNG, or WebP file.", Rejected(html));
        Assert.Equal("Choose a file that is not empty.", Rejected(empty));
    }

    [Fact]
    public async Task A_file_larger_than_10_mb_is_rejected()
    {
        var bytes = new byte[DocumentRules.MaxBytes + 1];
        "%PDF-1.4"u8.CopyTo(bytes);

        var prepared = await DocumentRules.PrepareAsync(new MemoryStream(bytes), "large.pdf", CancellationToken.None);

        Assert.Equal("The file must be 10 MB or smaller.", Rejected(prepared));
    }

    [Fact]
    public void A_file_name_keeps_the_last_segment_and_drops_a_path()
    {
        Assert.Equal("visa.pdf", DocumentRules.SanitizeFileName(@"..\..\visa.pdf"));
        Assert.Equal("visa.pdf", DocumentRules.SanitizeFileName("/tmp/visa.pdf"));
        Assert.Null(DocumentRules.SanitizeFileName("   "));
        Assert.Null(DocumentRules.SanitizeFileName(new string('a', DocumentRules.FileNameMaxLength + 1) + ".pdf"));
    }

    private static PreparedDocument.Accepted Accepted(PreparedDocument prepared)
    {
        return Assert.IsType<PreparedDocument.Accepted>(prepared);
    }

    private static string Rejected(PreparedDocument prepared)
    {
        return Assert.IsType<PreparedDocument.Rejected>(prepared).Message;
    }

    private static MemoryStream StreamOf(ReadOnlySpan<byte> content)
    {
        return new MemoryStream(content.ToArray());
    }
}
