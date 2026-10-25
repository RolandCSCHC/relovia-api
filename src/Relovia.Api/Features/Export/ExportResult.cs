namespace Relovia.Api.Features.Export;

public abstract record ExportResult
{
    private ExportResult()
    {
    }

    public sealed record NotFound : ExportResult;

    public sealed record Pdf(byte[] Content, string FileName) : ExportResult;
}
