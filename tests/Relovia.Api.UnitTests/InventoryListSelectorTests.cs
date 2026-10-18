using Relovia.Api.Features.Inventory;

namespace Relovia.Api.UnitTests;

public sealed class InventoryListSelectorTests
{
    [Fact]
    public void A_furnished_home_keeps_shared_lists_and_a_more_specific_copy_of_the_same_code()
    {
        var selection = InventoryListSelector.Select(
            Furnishing.Furnished,
            room: null,
            [
                Template("shared", FurnishingScope.Both, ItemRoom.Bathroom, Item("towels", "Towels")),
                Template("furnished-bath", FurnishingScope.Furnished, ItemRoom.Bathroom, Item("towels", "Bath towels")),
                Template("unfurnished-bath", FurnishingScope.Unfurnished, ItemRoom.Bathroom, Item("towels", "Pack of towels")),
                Template("draft", FurnishingScope.Furnished, ItemRoom.Bathroom, Item("secret", "Secret"), published: false),
            ]);

        Assert.Equal(["furnished-bath", "shared"], selection.Templates.Select(template => template.Code));
        var towels = Assert.Single(selection.Items);
        Assert.Equal("Bath towels", towels.Name);
        Assert.Equal(ItemRoom.Bathroom, towels.Room);
    }

    [Fact]
    public void An_unfurnished_home_uses_the_unfurnished_copy_and_includes_furniture()
    {
        var selection = InventoryListSelector.Select(
            Furnishing.Unfurnished,
            room: null,
            [
                Template("shared", FurnishingScope.Both, ItemRoom.Bathroom, Item("towels", "Towels")),
                Template("furnished-bath", FurnishingScope.Furnished, ItemRoom.Bathroom, Item("towels", "Bath towels")),
                Template("unfurnished-bath", FurnishingScope.Unfurnished, ItemRoom.Bathroom, Item("towels", "Pack of towels")),
                Template("living-room", FurnishingScope.Unfurnished, ItemRoom.LivingRoom, Item("sofa", "Sofa")),
            ]);

        Assert.Equal(["living-room", "shared", "unfurnished-bath"], selection.Templates.Select(template => template.Code));
        Assert.Equal(["Pack of towels", "Sofa"], selection.Items.Select(item => item.Name));
    }

    [Fact]
    public void A_room_limits_the_lists_and_a_newer_published_version_replaces_the_older_one()
    {
        var older = Template("kitchen", FurnishingScope.Both, ItemRoom.Kitchen, Item("pan", "Pan"), version: 1);
        var newer = Template("kitchen", FurnishingScope.Both, ItemRoom.Kitchen, Item("pan", "New pan"), version: 2);
        var draft = Template(
            "kitchen",
            FurnishingScope.Both,
            ItemRoom.Kitchen,
            Item("pan", "Draft pan"),
            version: 3,
            published: false);
        var bedroom = Template("bedroom", FurnishingScope.Both, ItemRoom.Bedroom, Item("sheets", "Sheets"));

        var selection = InventoryListSelector.Select(
            Furnishing.Furnished,
            ItemRoom.Kitchen,
            [older, newer, draft, bedroom]);

        var template = Assert.Single(selection.Templates);
        Assert.Equal(2, template.Version);
        Assert.Equal("New pan", Assert.Single(selection.Items).Name);
    }

    [Fact]
    public void Items_are_ordered_by_room_then_priority_then_name()
    {
        var selection = InventoryListSelector.Select(
            Furnishing.Unfurnished,
            room: null,
            [
                Template(
                    "bedroom",
                    FurnishingScope.Both,
                    ItemRoom.Bedroom,
                    Item("pillow", "Pillow", ItemPriority.High)),
                Template(
                    "kitchen",
                    FurnishingScope.Both,
                    ItemRoom.Kitchen,
                    Item("pan", "Pan", ItemPriority.Low),
                    Item("knife", "Knife", ItemPriority.High)),
            ]);

        Assert.Equal(["knife", "pan", "pillow"], selection.Items.Select(item => item.Code));
    }

    [Fact]
    public void Confirmation_requires_one_code_from_the_list_and_rejects_a_duplicate()
    {
        var empty = InventoryTemplateRules.ValidateConfirmation(new ConfirmInventoryListRequest
        {
            Furnishing = Furnishing.Unfurnished,
            Codes = [],
        });
        Assert.Equal("Select at least one item.", Assert.Single(empty["codes"]));

        var duplicate = InventoryTemplateRules.ValidateConfirmation(new ConfirmInventoryListRequest
        {
            Furnishing = Furnishing.Furnished,
            Codes = ["frying-pan", " frying-pan "],
        });
        Assert.Equal("An item can only be selected once.", Assert.Single(duplicate["codes"]));

        var unknown = InventoryTemplateRules.ValidateConfirmation(new ConfirmInventoryListRequest
        {
            Furnishing = Furnishing.Furnished,
            Codes = [" "],
        });
        Assert.Equal("Choose items from this list.", Assert.Single(unknown["codes"]));
    }

    private static InventoryListTemplate Template(
        string code,
        FurnishingScope scope,
        ItemRoom room,
        InventoryTemplateItem item,
        int version = 1,
        bool published = true)
    {
        return Template(code, scope, room, version, published, [item]);
    }

    private static InventoryListTemplate Template(
        string code,
        FurnishingScope scope,
        ItemRoom room,
        InventoryTemplateItem first,
        InventoryTemplateItem second,
        int version = 1,
        bool published = true)
    {
        return Template(code, scope, room, version, published, [first, second]);
    }

    private static InventoryListTemplate Template(
        string code,
        FurnishingScope scope,
        ItemRoom room,
        int version,
        bool published,
        IReadOnlyList<InventoryTemplateItem> items)
    {
        var template = new InventoryListTemplate
        {
            Code = code,
            Version = version,
            Title = code,
            Room = room,
            Scope = scope,
            PublishedAt = published ? DateTimeOffset.UtcNow : null,
        };
        foreach (var item in items)
        {
            item.InventoryListTemplateId = template.Id;
            template.Items.Add(item);
        }

        return template;
    }

    private static InventoryTemplateItem Item(string code, string name, ItemPriority priority = ItemPriority.Normal)
    {
        return new InventoryTemplateItem
        {
            Code = code,
            Name = name,
            Category = ItemCategory.Other,
            Quantity = 1,
            Priority = priority,
        };
    }
}
