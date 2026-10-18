using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed class StarterInventoryListPersistenceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public StarterInventoryListPersistenceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Published_starter_lists_are_available_for_selection()
    {
        await using var db = _postgres.CreateContext();
        var templates = await db.InventoryListTemplates
            .Include(template => template.Items)
            .OrderBy(template => template.Code)
            .ToListAsync();

        Assert.Equal(8, templates.Count);
        Assert.All(templates, template =>
        {
            Assert.Equal(1, template.Version);
            Assert.NotNull(template.PublishedAt);
            Assert.NotEmpty(template.Items);
        });

        var furnished = InventoryListSelector.Select(Furnishing.Furnished, room: null, templates);
        Assert.Equal(
            [
                StarterInventoryLists.BathroomBasicsCode,
                StarterInventoryLists.BedroomBasicsCode,
                StarterInventoryLists.KitchenBasicsCode,
            ],
            furnished.Templates.Select(template => template.Code));
        Assert.Contains(furnished.Items, item => item.Code == "frying-pan");
        Assert.DoesNotContain(furnished.Items, item => item.Code == "sofa");
    }

    [Fact]
    public async Task A_published_starter_list_cannot_be_changed()
    {
        await using var db = _postgres.CreateContext();
        var template = await db.InventoryListTemplates.SingleAsync(candidate =>
            candidate.Code == StarterInventoryLists.KitchenBasicsCode);
        template.Title = "Changed";

        await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_published_starter_list_cannot_gain_an_item()
    {
        await using var db = _postgres.CreateContext();
        db.InventoryTemplateItems.Add(new InventoryTemplateItem
        {
            InventoryListTemplateId = StarterInventoryLists.KitchenBasicsId,
            Code = "extra-pan",
            Name = "Extra pan",
            Category = ItemCategory.Cookware,
            Quantity = 1,
            Priority = ItemPriority.Low,
        });

        await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
    }
}
