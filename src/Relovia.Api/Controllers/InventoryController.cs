using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}/inventory")]
public sealed class InventoryController(InventoryService inventory, InventoryListService lists) : ControllerBase
{
    [HttpGet]
    [EndpointName("listInventoryItems")]
    [EndpointSummary("Lists the relocation's belongings, by room and then by name.")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemResponse>>> List(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await inventory.ListAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpGet("suggestion")]
    [EndpointName("getInventorySuggestion")]
    [EndpointSummary("Returns a room shopping list without writing items. Items already stored under the same code are left out.")]
    public async Task<ActionResult<InventorySuggestionResponse>> Suggest(
        Guid relocationId,
        [FromQuery] Furnishing? furnishing,
        [FromQuery] ItemRoom? room,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await lists.SuggestAsync(userId, relocationId, furnishing, room, cancellationToken),
            Ok);
    }

    [HttpPost("suggestion")]
    [EndpointName("confirmInventoryList")]
    [EndpointSummary("Adds the selected list items as things to buy. Items already stored under the same code are skipped.")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemResponse>>> Confirm(
        Guid relocationId,
        ConfirmInventoryListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await lists.ConfirmAsync(userId, relocationId, request, cancellationToken),
            Ok);
    }

    [HttpPost]
    [EndpointName("createInventoryItem")]
    [EndpointSummary("Adds a belonging. A bring item with no packing status starts as not packed.")]
    public async Task<ActionResult<InventoryItemResponse>> Create(
        Guid relocationId,
        SaveInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await inventory.CreateAsync(userId, relocationId, request, cancellationToken),
            item => Created($"/api/relocations/{relocationId}/inventory/{item.Id}", item));
    }

    [HttpPut("{itemId:guid}")]
    [EndpointName("updateInventoryItem")]
    [EndpointSummary("Updates a belonging. Prices stay as entered.")]
    public async Task<ActionResult<InventoryItemResponse>> Update(
        Guid relocationId,
        Guid itemId,
        SaveInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await inventory.UpdateAsync(userId, relocationId, itemId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("{itemId:guid}")]
    [EndpointName("deleteInventoryItem")]
    [EndpointSummary("Deletes a belonging.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await inventory.DeleteAsync(userId, relocationId, itemId, cancellationToken),
            _ => NoContent());
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    private ActionResult Map<T>(InventoryResult<T> result, Func<T, ActionResult> success)
    {
        return result switch
        {
            InventoryResult<T>.Success ok => success(ok.Value),
            InventoryResult<T>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            InventoryResult<T>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            _ => throw new InvalidOperationException("Unexpected inventory result."),
        };
    }
}
