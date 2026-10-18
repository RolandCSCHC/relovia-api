namespace Relovia.Api.Features.Inventory;

public static class InventoryListSelector
{
    public static InventoryListSelection Select(
        Furnishing furnishing,
        ItemRoom? room,
        IEnumerable<InventoryListTemplate> templates)
    {
        var chosen = templates
            .Where(template => template.IsPublished && Applies(furnishing, room, template))
            .GroupBy(template => InventoryTemplateRules.NormalizeCode(template.Code), StringComparer.Ordinal)
            .Select(versions => versions.OrderByDescending(template => template.Version).First())
            .OrderByDescending(Specificity)
            .ThenBy(template => InventoryTemplateRules.NormalizeCode(template.Code), StringComparer.Ordinal)
            .ToArray();

        var selected = new List<SelectedInventoryItem>();
        var byCode = new Dictionary<string, SelectedInventoryItem>(StringComparer.Ordinal);
        foreach (var template in chosen)
        {
            foreach (var item in template.Items)
            {
                var code = InventoryTemplateRules.NormalizeCode(item.Code);
                if (!InventoryTemplateRules.IsCode(code) || byCode.ContainsKey(code))
                {
                    continue;
                }

                var selectedItem = new SelectedInventoryItem(
                    item.Id,
                    code,
                    item.Name.Trim(),
                    item.Category,
                    template.Room,
                    item.Quantity,
                    item.Priority);
                byCode.Add(code, selectedItem);
                selected.Add(selectedItem);
            }
        }

        var items = selected
            .OrderBy(item => (int)item.Room)
            .ThenBy(PriorityRank)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToArray();
        var orderedTemplates = chosen
            .OrderBy(template => InventoryTemplateRules.NormalizeCode(template.Code), StringComparer.Ordinal)
            .ToArray();

        return new InventoryListSelection(orderedTemplates, items);
    }

    private static bool Applies(Furnishing furnishing, ItemRoom? room, InventoryListTemplate template)
    {
        if (room is not null && template.Room != room)
        {
            return false;
        }

        return template.Scope switch
        {
            FurnishingScope.Both => true,
            FurnishingScope.Furnished => furnishing == Furnishing.Furnished,
            FurnishingScope.Unfurnished => furnishing == Furnishing.Unfurnished,
            _ => false,
        };
    }

    // A list for one kind of home is more specific than a list shared by both.
    private static int Specificity(InventoryListTemplate template)
    {
        return template.Scope == FurnishingScope.Both ? 0 : 1;
    }

    private static int PriorityRank(SelectedInventoryItem item)
    {
        return item.Priority switch
        {
            ItemPriority.High => 0,
            ItemPriority.Normal => 1,
            ItemPriority.Low => 2,
            _ => 3,
        };
    }
}

public sealed record InventoryListSelection(
    IReadOnlyList<InventoryListTemplate> Templates,
    IReadOnlyList<SelectedInventoryItem> Items);

public sealed record SelectedInventoryItem(
    Guid SourceTemplateItemId,
    string Code,
    string Name,
    ItemCategory Category,
    ItemRoom Room,
    int Quantity,
    ItemPriority Priority);
