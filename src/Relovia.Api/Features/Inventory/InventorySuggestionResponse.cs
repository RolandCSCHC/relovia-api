namespace Relovia.Api.Features.Inventory;

public sealed record InventorySuggestionResponse(
    Furnishing Furnishing,
    ItemRoom? Room,
    IReadOnlyList<string> TemplateCodes,
    IReadOnlyList<SuggestedInventoryItemResponse> Items)
{
    public static InventorySuggestionResponse From(
        Furnishing furnishing,
        ItemRoom? room,
        InventoryListSelection selection,
        IReadOnlySet<string> presentCodes)
    {
        var items = selection.Items
            .Where(item => !presentCodes.Contains(item.Code))
            .Select(item => new SuggestedInventoryItemResponse(
                item.Code,
                item.Name,
                item.Category,
                item.Room,
                item.Quantity,
                item.Priority,
                item.SourceTemplateItemId))
            .ToArray();

        return new InventorySuggestionResponse(
            furnishing,
            room,
            selection.Templates.Select(template => template.Code).ToArray(),
            items);
    }
}

public sealed record SuggestedInventoryItemResponse(
    string Code,
    string Name,
    ItemCategory Category,
    ItemRoom Room,
    int Quantity,
    ItemPriority Priority,
    Guid SourceTemplateItemId);
