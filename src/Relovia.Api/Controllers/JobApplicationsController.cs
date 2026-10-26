using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Applications;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}")]
public sealed class JobApplicationsController(JobApplicationService applications) : ControllerBase
{
    [HttpGet("applications")]
    [EndpointName("listJobApplications")]
    [EndpointSummary("Lists job applications the signed-in user is tracking for this relocation.")]
    public async Task<ActionResult<IReadOnlyList<JobApplicationResponse>>> List(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await applications.ListAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpPost("applications")]
    [EndpointName("createJobApplication")]
    [EndpointSummary("Adds a job application. The next date is not written to the calendar.")]
    public async Task<ActionResult<JobApplicationResponse>> Create(
        Guid relocationId,
        SaveJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await applications.CreateAsync(userId, relocationId, request, cancellationToken),
            application => Created(
                $"/api/relocations/{relocationId}/applications/{application.Id}",
                application));
    }

    [HttpPut("applications/{applicationId:guid}")]
    [EndpointName("updateJobApplication")]
    [EndpointSummary("Updates a job application. The calendar stays unchanged.")]
    public async Task<ActionResult<JobApplicationResponse>> Update(
        Guid relocationId,
        Guid applicationId,
        SaveJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await applications.UpdateAsync(userId, relocationId, applicationId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("applications/{applicationId:guid}")]
    [EndpointName("deleteJobApplication")]
    [EndpointSummary("Deletes a job application. The relocation's calendar stays.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await applications.DeleteAsync(userId, relocationId, applicationId, cancellationToken),
            _ => NoContent());
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    private ActionResult Map<T>(JobApplicationResult<T> result, Func<T, ActionResult> success)
    {
        return result switch
        {
            JobApplicationResult<T>.Success ok => success(ok.Value),
            JobApplicationResult<T>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            JobApplicationResult<T>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            _ => throw new InvalidOperationException("Unexpected job application result."),
        };
    }
}
