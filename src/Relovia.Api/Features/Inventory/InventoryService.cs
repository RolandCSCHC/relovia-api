using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Features.Inventory;

public sealed class InventoryService(ReloviaDbContext db)
{
    public const string RelocationNotFoundTitle = "Relocation was not found.";

    public const string ItemNotFoundTitle = "Item was not found.";

    public async Task<InventoryResult<IReadOnlyList<InventoryItemResponse>>> ListAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.NotFound(RelocationNotFoundTitle);
        }

        var items = await db.InventoryItems.AsNoTracking()
            .Where(item => item.RelocationId == relocationId)
            .OrderBy(item => item.Room)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.Success(
            items.Select(InventoryItemResponse.From).ToArray());
    }

    public async Task<InventoryResult<InventoryItemResponse>> CreateAsync(
        Guid userId,
        Guid relocationId,
        SaveInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var errors = InventoryRules.Validate(request);
        if (errors.Count > 0)
        {
            return new InventoryResult<InventoryItemResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new InventoryResult<InventoryItemResponse>.NotFound(RelocationNotFoundTitle);
        }

        var links = await LinkErrorsAsync(relocationId, request, cancellationToken);
        if (links.Count > 0)
        {
            return new InventoryResult<InventoryItemResponse>.Invalid(links);
        }

        var item = new InventoryItem { RelocationId = relocationId };
        Apply(item, request);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return new InventoryResult<InventoryItemResponse>.Success(InventoryItemResponse.From(item));
    }

    public async Task<InventoryResult<InventoryItemResponse>> UpdateAsync(
        Guid userId,
        Guid relocationId,
        Guid itemId,
        SaveInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var errors = InventoryRules.Validate(request);
        if (errors.Count > 0)
        {
            return new InventoryResult<InventoryItemResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new InventoryResult<InventoryItemResponse>.NotFound(RelocationNotFoundTitle);
        }

        var item = await db.InventoryItems.SingleOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (item is null)
        {
            return new InventoryResult<InventoryItemResponse>.NotFound(ItemNotFoundTitle);
        }

        var links = await LinkErrorsAsync(relocationId, request, cancellationToken);
        if (links.Count > 0)
        {
            return new InventoryResult<InventoryItemResponse>.Invalid(links);
        }

        Apply(item, request);
        await db.SaveChangesAsync(cancellationToken);
        return new InventoryResult<InventoryItemResponse>.Success(InventoryItemResponse.From(item));
    }

    public async Task<InventoryResult<Guid>> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new InventoryResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var item = await db.InventoryItems.SingleOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (item is null)
        {
            return new InventoryResult<Guid>.NotFound(ItemNotFoundTitle);
        }

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return new InventoryResult<Guid>.Success(item.Id);
    }

    private async Task<bool> RelocationExistsAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AnyAsync(
            relocation => relocation.Id == relocationId && relocation.UserId == userId,
            cancellationToken);
    }

    private async Task<Dictionary<string, string[]>> LinkErrorsAsync(
        Guid relocationId,
        SaveInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Acquisition != AcquisitionStatus.ToPurchase || request.BudgetCategoryId is not Guid categoryId)
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal);
        }

        var exists = await db.BudgetCategories.AnyAsync(
            category => category.Id == categoryId && category.RelocationId == relocationId,
            cancellationToken);
        if (exists)
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal);
        }

        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["budgetCategoryId"] = ["Choose a category from this budget."],
        };
    }

    /// <summary>
    /// A bring item with no packing status starts as not packed. Any other action clears packing.
    /// Prices stay as entered.
    /// </summary>
    private static void Apply(InventoryItem item, SaveInventoryItemRequest request)
    {
        item.Name = request.Name.Trim();
        item.Category = request.Category;
        item.Room = request.Room;
        item.Quantity = request.Quantity;
        item.Acquisition = request.Acquisition;
        item.MovingAction = request.MovingAction;
        item.PackingStatus = request.MovingAction == MovingAction.Bring
            ? request.PackingStatus ?? PackingStatus.NotPacked
            : null;
        item.Priority = request.Priority;
        item.EstimatedPrice = request.EstimatedPrice;
        item.ActualPrice = request.ActualPrice;
        item.CurrencyCode = request.EstimatedPrice is null && request.ActualPrice is null
            ? null
            : BudgetRules.NormalizeCurrencyCode(request.CurrencyCode!);
        item.BudgetCategoryId = request.Acquisition == AcquisitionStatus.ToPurchase
            ? request.BudgetCategoryId
            : null;
        item.Notes = BlankToNull(request.Notes);
    }

    private static string? BlankToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
