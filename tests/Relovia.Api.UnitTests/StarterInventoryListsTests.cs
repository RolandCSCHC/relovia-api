using Relovia.Api.Features.Inventory;

namespace Relovia.Api.UnitTests;

public sealed class StarterInventoryListsTests
{
    [Fact]
    public void Starter_lists_are_published_and_item_codes_do_not_repeat()
    {
        Assert.Equal(8, StarterInventoryLists.Templates.Count);
        Assert.All(StarterInventoryLists.Templates, template =>
        {
            Assert.Equal(1, template.Version);
            Assert.Equal(StarterInventoryLists.PublishedAt, template.PublishedAt);
            Assert.True(InventoryTemplateRules.IsCode(template.Code));
            Assert.Contains(
                StarterInventoryLists.Items,
                item => item.InventoryListTemplateId == template.Id);
        });

        var codes = StarterInventoryLists.Items.Select(item => item.Code).ToArray();
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(StarterInventoryLists.Items, item =>
        {
            Assert.True(InventoryTemplateRules.IsCode(item.Code));
            Assert.InRange(item.Quantity, InventoryRules.MinQuantity, InventoryRules.MaxQuantity);
        });
    }

    [Fact]
    public void A_furnished_home_is_the_shared_basics_and_an_unfurnished_home_adds_the_furniture()
    {
        var graph = Graph();

        var furnished = InventoryListSelector.Select(Furnishing.Furnished, room: null, graph);
        Assert.Equal(
            [
                StarterInventoryLists.BathroomBasicsCode,
                StarterInventoryLists.BedroomBasicsCode,
                StarterInventoryLists.KitchenBasicsCode,
            ],
            furnished.Templates.Select(template => template.Code));
        Assert.Equal(
            [
                "knife", "frying-pan", "saucepan",
                "bin", "bowls", "cutlery", "cutting-board", "glasses", "kettle", "plates",
                "tea-towels",
                "bath-towels", "hand-towels", "toilet-brush", "bath-mat",
                "bed-sheets", "duvet", "pillows", "hangers", "laundry-basket",
            ],
            furnished.Items.Select(item => item.Code));
        Assert.DoesNotContain(furnished.Items, item => item.Code is "sofa" or "fridge");

        var unfurnished = InventoryListSelector.Select(Furnishing.Unfurnished, room: null, graph);
        Assert.Equal(
            [
                StarterInventoryLists.BathroomBasicsCode,
                StarterInventoryLists.BedroomBasicsCode,
                StarterInventoryLists.BedroomFurnitureCode,
                StarterInventoryLists.DiningRoomCode,
                StarterInventoryLists.KitchenAppliancesCode,
                StarterInventoryLists.KitchenBasicsCode,
                StarterInventoryLists.LivingRoomCode,
                StarterInventoryLists.OfficeCode,
            ],
            unfurnished.Templates.Select(template => template.Code));
        Assert.Equal(
            [
                "knife", "cooker", "fridge", "frying-pan", "saucepan",
                "bin", "bowls", "cutlery", "cutting-board", "glasses", "kettle", "plates",
                "microwave", "tea-towels",
                "bath-towels", "hand-towels", "toilet-brush", "bath-mat",
                "bed-frame", "bed-sheets", "duvet", "mattress", "pillows",
                "hangers", "wardrobe", "laundry-basket",
                "sofa", "coffee-table", "curtains", "floor-lamp",
                "dining-chairs", "dining-table",
                "desk", "desk-chair",
            ],
            unfurnished.Items.Select(item => item.Code));

        var kitchen = InventoryListSelector.Select(Furnishing.Unfurnished, ItemRoom.Kitchen, graph);
        Assert.Equal(
            [StarterInventoryLists.KitchenAppliancesCode, StarterInventoryLists.KitchenBasicsCode],
            kitchen.Templates.Select(template => template.Code));
        Assert.All(kitchen.Items, item => Assert.Equal(ItemRoom.Kitchen, item.Room));
        Assert.Equal(4, kitchen.Items.Single(item => item.Code == "plates").Quantity);
    }

    private static IReadOnlyList<InventoryListTemplate> Graph()
    {
        var templates = StarterInventoryLists.Templates.ToDictionary(
            template => template.Id,
            template => new InventoryListTemplate
            {
                Id = template.Id,
                Code = template.Code,
                Version = template.Version,
                Title = template.Title,
                Room = template.Room,
                Scope = template.Scope,
                PublishedAt = template.PublishedAt,
            });

        foreach (var source in StarterInventoryLists.Items)
        {
            templates[source.InventoryListTemplateId].Items.Add(new InventoryTemplateItem
            {
                Id = source.Id,
                InventoryListTemplateId = source.InventoryListTemplateId,
                Code = source.Code,
                Name = source.Name,
                Category = source.Category,
                Quantity = source.Quantity,
                Priority = source.Priority,
            });
        }

        return templates.Values.ToArray();
    }
}
