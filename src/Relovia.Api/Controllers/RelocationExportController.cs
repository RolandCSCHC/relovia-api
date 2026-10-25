using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Features.Export;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}")]
public sealed class RelocationExportController(RelocationExportService export) : ControllerBase
{
    [HttpGet("export")]
    [Produces("application/pdf")]
    [EndpointName("downloadRelocationPdf")]
    [EndpointSummary("Downloads the relocation as a PDF. The file is not stored.")]
    public async Task<IActionResult> Download(
        Guid relocationId,
        string? timeZone,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (await export.DownloadAsync(userId, relocationId, timeZone, cancellationToken) is not ExportResult.Pdf pdf)
        {
            return Missing();
        }

        return Pdf(pdf);
    }

    [HttpGet("export/summary")]
    [Produces("application/pdf")]
    [EndpointName("downloadRelocationSummaryPdf")]
    [EndpointSummary("Downloads the summary as a PDF. The file is not stored.")]
    public async Task<IActionResult> DownloadSummary(
        Guid relocationId,
        string? timeZone,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (await export.DownloadSummaryAsync(userId, relocationId, timeZone, cancellationToken) is not ExportResult.Pdf pdf)
        {
            return Missing();
        }

        return Pdf(pdf);
    }

    private FileContentResult Pdf(ExportResult.Pdf pdf)
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "private, no-store";
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    private ObjectResult Missing()
    {
        return Problem(statusCode: StatusCodes.Status404NotFound, title: "Relocation was not found.");
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }
}
