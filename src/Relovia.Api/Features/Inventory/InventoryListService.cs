using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Inventory;

public sealed class InventoryListService(ReloviaDbContext db)
{
    public async Task<InventoryResult<InventorySuggestionResponse>> SuggestAsync(
        Guid userId,
        Guid relocationId,
        Furnishing? furnishing,
        ItemRoom? room,
        CancellationToken cancellationToken)
    {
        var errors = InventoryTemplateRules.ValidateSuggestion(furnishing, room);
        if (errors.Count > 0)
        {
            return new InventoryResult<InventorySuggestionResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new InventoryResult<InventorySuggestionResponse>.NotFound(InventoryService.RelocationNotFoundTitle);
        }

        var selection = InventoryListSelector.Select(
            furnishing!.Value,
            room,
            await TemplatesAsync(cancellationToken));
        var present = await PresentCodesAsync(relocationId, cancellationToken);
        return new InventoryResult<InventorySuggestionResponse>.Success(
            InventorySuggestionResponse.From(furnishing.Value, room, selection, present));
    }

    public async Task<InventoryResult<IReadOnlyList<InventoryItemResponse>>> ConfirmAsync(
        Guid userId,
        Guid relocationId,
        ConfirmInventoryListRequest request,
        CancellationToken cancellationToken)
    {
        var errors = InventoryTemplateRules.ValidateConfirmation(request);
        if (errors.Count > 0)
        {
            return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.NotFound(
                InventoryService.RelocationNotFoundTitle);
        }

        var selection = InventoryListSelector.Select(
            request.Furnishing,
            request.Room,
            await TemplatesAsync(cancellationToken));
        var suggested = selection.Items.ToDictionary(item => item.Code, StringComparer.Ordinal);
        var codes = request.Codes.Select(InventoryTemplateRules.NormalizeCode).ToArray();
        if (codes.Any(code => !suggested.ContainsKey(code)))
        {
            return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.Invalid(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["codes"] = ["Choose items from this list."],
                });
        }

        var present = await PresentCodesAsync(relocationId, cancellationToken);
        var creating = codes.Where(code => !present.Contains(code)).Select(code => suggested[code]).ToArray();
        if (creating.Length == 0)
        {
            return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.Success([]);
        }

        var items = creating.Select(source => new InventoryItem
        {
            RelocationId = relocationId,
            Code = source.Code,
            Name = source.Name,
            Category = source.Category,
            Room = source.Room,
            Quantity = source.Quantity,
            Acquisition = AcquisitionStatus.ToPurchase,
            Priority = source.Priority,
            SourceTemplateItemId = source.SourceTemplateItemId,
        }).ToArray();

        db.InventoryItems.AddRange(items);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateCode(exception))
        {
            return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.Invalid(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["codes"] = ["Some of these items are already on the list."],
                });
        }

        return new InventoryResult<IReadOnlyList<InventoryItemResponse>>.Success(
            items.Select(InventoryItemResponse.From).ToArray());
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

    private Task<List<InventoryListTemplate>> TemplatesAsync(CancellationToken cancellationToken)
    {
        return db.InventoryListTemplates.AsNoTracking()
            .Include(template => template.Items)
            .ToListAsync(cancellationToken);
    }

    private static bool IsDuplicateCode(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    private async Task<HashSet<string>> PresentCodesAsync(Guid relocationId, CancellationToken cancellationToken)
    {
        var codes = await db.InventoryItems.AsNoTracking()
            .Where(item => item.RelocationId == relocationId && item.Code != null)
            .Select(item => item.Code!)
            .ToListAsync(cancellationToken);
        return codes.ToHashSet(StringComparer.Ordinal);
    }
}
