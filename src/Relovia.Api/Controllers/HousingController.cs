using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Housing;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}")]
public sealed class HousingController(HousingService housing) : ControllerBase
{
    [HttpGet("housing")]
    [EndpointName("listHousingOptions")]
    [EndpointSummary("Lists homes the signed-in user is comparing for this relocation.")]
    public async Task<ActionResult<IReadOnlyList<HousingOptionResponse>>> List(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await housing.ListAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpPost("housing")]
    [EndpointName("createHousingOption")]
    [EndpointSummary("Adds a home to compare. The budget, tasks, and calendar stay unchanged.")]
    public async Task<ActionResult<HousingOptionResponse>> Create(
        Guid relocationId,
        SaveHousingOptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await housing.CreateAsync(userId, relocationId, request, cancellationToken),
            option => Created($"/api/relocations/{relocationId}/housing/{option.Id}", option));
    }

    [HttpPut("housing/{optionId:guid}")]
    [EndpointName("updateHousingOption")]
    [EndpointSummary("Updates a home. Choosing it does not copy the rent into the budget.")]
    public async Task<ActionResult<HousingOptionResponse>> Update(
        Guid relocationId,
        Guid optionId,
        SaveHousingOptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await housing.UpdateAsync(userId, relocationId, optionId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("housing/{optionId:guid}")]
    [EndpointName("deleteHousingOption")]
    [EndpointSummary("Deletes a home. The relocation's budget and tasks stay.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await housing.DeleteAsync(userId, relocationId, optionId, cancellationToken),
            _ => NoContent());
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    private ActionResult Map<T>(HousingResult<T> result, Func<T, ActionResult> success)
    {
        return result switch
        {
            HousingResult<T>.Success ok => success(ok.Value),
            HousingResult<T>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            HousingResult<T>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            _ => throw new InvalidOperationException("Unexpected housing result."),
        };
    }
}
