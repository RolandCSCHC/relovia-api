using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations")]
public sealed class RelocationsController(RelocationService relocations) : ControllerBase
{
    [HttpGet]
    [EndpointName("listRelocations")]
    [EndpointSummary("Lists the signed-in user's relocations.")]
    public async Task<ActionResult<IReadOnlyList<RelocationResponse>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await relocations.ListAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [EndpointName("getRelocation")]
    [EndpointSummary("Returns one relocation owned by the signed-in user.")]
    public async Task<ActionResult<RelocationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var relocation = await relocations.FindAsync(userId, id, cancellationToken);
        return relocation is null ? NotFoundRelocation() : Ok(relocation);
    }

    [HttpPost]
    [EndpointName("createRelocation")]
    [EndpointSummary("Creates a relocation for the signed-in user.")]
    public async Task<ActionResult<RelocationResponse>> Create(
        SaveRelocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await relocations.CreateAsync(userId, request, cancellationToken);
        return Map(result, created: true);
    }

    [HttpPut("{id:guid}")]
    [EndpointName("updateRelocation")]
    [EndpointSummary("Updates a relocation owned by the signed-in user.")]
    public async Task<ActionResult<RelocationResponse>> Update(
        Guid id,
        SaveRelocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await relocations.UpdateAsync(userId, id, request, cancellationToken);
        return Map(result, created: false);
    }

    [HttpDelete("{id:guid}")]
    [EndpointName("deleteRelocation")]
    [EndpointSummary("Deletes a relocation owned by the signed-in user.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await relocations.DeleteAsync(userId, id, cancellationToken);
        return deleted ? NoContent() : NotFoundRelocation();
    }

    private ActionResult Map(RelocationSaveResult result, bool created)
    {
        return result switch
        {
            RelocationSaveResult.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            RelocationSaveResult.NotFound => NotFoundRelocation(),
            RelocationSaveResult.Saved saved when created => CreatedAtAction(
                nameof(Get),
                new { id = saved.Relocation.Id },
                saved.Relocation),
            RelocationSaveResult.Saved saved => Ok(saved.Relocation),
            _ => throw new InvalidOperationException("Unexpected relocation save result."),
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
