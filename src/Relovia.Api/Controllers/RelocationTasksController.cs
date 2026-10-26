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
    [EndpointName("listRelocationTasks")]
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

    [HttpPost]
    [EndpointName("createRelocationTask")]
    [EndpointSummary("Adds a task to the signed-in user's relocation.")]
    public async Task<ActionResult<RelocationTaskResponse>> Create(
        Guid relocationId,
        CreateRelocationTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await checklists.CreateAsync(userId, relocationId, request, cancellationToken);
        return result switch
        {
            RelocationTaskCreateResult.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            RelocationTaskCreateResult.RelocationNotFound => NotFoundRelocation(),
            RelocationTaskCreateResult.Created created => Created(
                $"/api/relocations/{relocationId}/tasks/{created.Task.Id}",
                created.Task),
            _ => throw new InvalidOperationException("Unexpected task create result."),
        };
    }

    [HttpDelete("{taskId:guid}")]
    [EndpointName("deleteRelocationTask")]
    [EndpointSummary("Deletes a task owned by the signed-in user.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await checklists.DeleteAsync(userId, relocationId, taskId, cancellationToken);
        return result switch
        {
            RelocationTaskDeleteResult.RelocationNotFound => NotFoundRelocation(),
            RelocationTaskDeleteResult.TaskNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task was not found."),
            RelocationTaskDeleteResult.Deleted => NoContent(),
            _ => throw new InvalidOperationException("Unexpected task delete result."),
        };
    }

    [HttpPost("{taskId:guid}/earlier")]
    [EndpointName("moveRelocationTaskEarlier")]
    [EndpointSummary("Swaps this task with the one before it. The first task has no earlier move.")]
    public Task<ActionResult<RelocationTasksResponse>> MoveEarlier(
        Guid relocationId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return Move(relocationId, taskId, checklists.MoveEarlierAsync, cancellationToken);
    }

    [HttpPost("{taskId:guid}/later")]
    [EndpointName("moveRelocationTaskLater")]
    [EndpointSummary("Swaps this task with the one after it. The last task has no later move.")]
    public Task<ActionResult<RelocationTasksResponse>> MoveLater(
        Guid relocationId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return Move(relocationId, taskId, checklists.MoveLaterAsync, cancellationToken);
    }

    [HttpPut("{taskId:guid}")]
    [EndpointName("updateRelocationTask")]
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

    private async Task<ActionResult<RelocationTasksResponse>> Move(
        Guid relocationId,
        Guid taskId,
        Func<Guid, Guid, Guid, CancellationToken, Task<RelocationTaskMoveResult>> move,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await move(userId, relocationId, taskId, cancellationToken);
        return result switch
        {
            RelocationTaskMoveResult.RelocationNotFound => NotFoundRelocation(),
            RelocationTaskMoveResult.TaskNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task was not found."),
            RelocationTaskMoveResult.NotAllowed denied => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: denied.Message),
            RelocationTaskMoveResult.Moved moved => Ok(moved.Tasks),
            _ => throw new InvalidOperationException("Unexpected task move result."),
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
