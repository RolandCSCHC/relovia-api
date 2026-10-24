using System.Buffers;

namespace Relovia.Api.Features.Documents;

public static class DocumentRules
{
    public const long MaxBytes = 10 * 1024 * 1024;

    public const long MaxRequestBytes = MaxBytes + (64 * 1024);

    public const int FileNameMaxLength = 200;

    public static async Task<PreparedDocument> PrepareAsync(
        Stream content,
        string? fileName,
        CancellationToken cancellationToken)
    {
        var name = SanitizeFileName(fileName);
        if (name is null)
        {
            return new PreparedDocument.Rejected(
                "Use a file name of 200 characters or fewer, without a path.");
        }

        var buffer = new MemoryStream();
        var rented = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(rented.AsMemory(0, rented.Length), cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxBytes)
                {
                    return new PreparedDocument.Rejected("The file must be 10 MB or smaller.");
                }

                buffer.Write(rented, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        if (buffer.Length == 0)
        {
            return new PreparedDocument.Rejected("Choose a file that is not empty.");
        }

        var bytes = buffer.GetBuffer();
        var type = DetectContentType(bytes.AsSpan(0, (int)buffer.Length));
        if (type is null)
        {
            return new PreparedDocument.Rejected("Upload a PDF, JPEG, PNG, or WebP file.");
        }

        return new PreparedDocument.Accepted(buffer.ToArray(), type, name);
    }

    public static string? SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var trimmed = fileName.Trim().Replace('\\', '/');
        var name = Path.GetFileName(trimmed);
        var cleaned = new string(name.Where(character => !char.IsControl(character) && character is not '"' and not ';').ToArray())
            .Trim();
        if (cleaned.Length is 0 or > FileNameMaxLength || cleaned is "." or "..")
        {
            return null;
        }

        if (cleaned.Contains('/') || cleaned.Contains('\\'))
        {
            return null;
        }

        return cleaned;
    }

    public static string? DetectContentType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith("%PDF-"u8))
        {
            return "application/pdf";
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.StartsWith(PngSignature))
        {
            return "image/png";
        }

        if (content.Length >= 12
            && content[..4].SequenceEqual("RIFF"u8)
            && content.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
}

public abstract record PreparedDocument
{
    private PreparedDocument()
    {
    }

    public sealed record Accepted(byte[] Content, string ContentType, string FileName) : PreparedDocument;

    public sealed record Rejected(string Message) : PreparedDocument;
}
