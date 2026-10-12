using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}/tasks")]
public sealed class RelocationTasksController(ChecklistService checklists) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists the signed-in user's tasks for a relocation, with progress.")]
    public async Task<ActionResult<RelocationTasksResponse>> List(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var tasks = await checklists.ListAsync(userId, relocationId, cancellationToken);
        return tasks is null ? NotFoundRelocation() : Ok(tasks);
    }

    [HttpPut("{taskId:guid}")]
    [EndpointSummary("Updates a task owned by the signed-in user.")]
    public async Task<ActionResult<RelocationTaskResponse>> Update(
        Guid relocationId,
        Guid taskId,
        UpdateRelocationTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await checklists.UpdateAsync(userId, relocationId, taskId, request, cancellationToken);
        return result switch
        {
            RelocationTaskUpdateResult.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            RelocationTaskUpdateResult.RelocationNotFound => NotFoundRelocation(),
            RelocationTaskUpdateResult.TaskNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task was not found."),
            RelocationTaskUpdateResult.Updated updated => Ok(updated.Task),
            _ => throw new InvalidOperationException("Unexpected task update result."),
        };
    }

    private ActionResult NotFoundRelocation()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Relocation was not found.");
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }
}
