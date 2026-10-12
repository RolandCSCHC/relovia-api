using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}/checklist")]
public sealed class ChecklistController(ChecklistService checklists) : ControllerBase
{
    [HttpGet]
    [EndpointName("getChecklistSuggestion")]
    [EndpointSummary("Returns the suggested checklist without writing tasks.")]
    public async Task<ActionResult<ChecklistSuggestionResponse>> Get(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var suggestion = await checklists.SuggestAsync(userId, relocationId, cancellationToken);
        return suggestion is null ? NotFoundRelocation() : Ok(suggestion);
    }

    [HttpPost]
    [EndpointName("confirmChecklist")]
    [EndpointSummary("Confirms the selected checklist tasks for a relocation.")]
    public async Task<ActionResult<RelocationTasksResponse>> Confirm(
        Guid relocationId,
        ConfirmChecklistRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await checklists.ConfirmAsync(userId, relocationId, request, cancellationToken);
        return result switch
        {
            ChecklistConfirmResult.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            ChecklistConfirmResult.NotFound => NotFoundRelocation(),
            ChecklistConfirmResult.AlreadyConfirmed => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Checklist is already confirmed."),
            ChecklistConfirmResult.Confirmed confirmed => Created(
                $"/api/relocations/{relocationId}/tasks",
                confirmed.Tasks),
            _ => throw new InvalidOperationException("Unexpected checklist confirm result."),
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
