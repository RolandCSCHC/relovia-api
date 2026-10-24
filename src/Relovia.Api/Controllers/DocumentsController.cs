using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Documents;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}/documents")]
public sealed class DocumentsController(DocumentService documents) : ControllerBase
{
    [HttpGet]
    [EndpointName("listDocuments")]
    [EndpointSummary("Lists the relocation's documents, oldest first.")]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> List(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await documents.ListAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [EndpointName("createDocument")]
    [EndpointSummary("Stores a private file on this relocation. PDF, JPEG, PNG, and WebP up to 10 MB.")]
    [RequestSizeLimit(DocumentRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentRules.MaxRequestBytes)]
    public async Task<ActionResult<DocumentResponse>> Create(
        Guid relocationId,
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var file = request.File;
        if (file is null)
        {
            return ApiValidation.Invalid(HttpContext, new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = ["Choose a file."],
            });
        }

        await using var stream = file.OpenReadStream();
        return Map(
            await documents.CreateAsync(userId, relocationId, file.FileName, stream, cancellationToken),
            document => Created($"/api/relocations/{relocationId}/documents/{document.Id}/content", document));
    }

    [HttpGet("{documentId:guid}/content")]
    [Produces("application/octet-stream", "application/pdf", "image/jpeg", "image/png", "image/webp")]
    [EndpointName("downloadDocument")]
    [EndpointSummary("Returns the file when the signed-in user owns the relocation.")]
    public async Task<IActionResult> Download(
        Guid relocationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var opened = await documents.OpenAsync(userId, relocationId, documentId, cancellationToken);
        if (opened is DocumentResult<DocumentContent>.Success ok)
        {
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.CacheControl = "private, no-store";
            return File(ok.Value.Content, ok.Value.ContentType, ok.Value.FileName);
        }

        return Map(opened, _ => throw new InvalidOperationException("A stored file is returned directly."));
    }

    [HttpDelete("{documentId:guid}")]
    [EndpointName("deleteDocument")]
    [EndpointSummary("Deletes the document and its stored file.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await documents.DeleteAsync(userId, relocationId, documentId, cancellationToken),
            _ => NoContent());
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    private ActionResult Map<T>(DocumentResult<T> result, Func<T, ActionResult> success)
    {
        return result switch
        {
            DocumentResult<T>.Success ok => success(ok.Value),
            DocumentResult<T>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            DocumentResult<T>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            _ => throw new InvalidOperationException("Unexpected document result."),
        };
    }
}
