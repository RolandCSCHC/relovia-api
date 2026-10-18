using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Inventory;

/// <summary>One suggested purchase on a room list. Prices are chosen later, on the inventory item.</summary>
public sealed class InventoryTemplateItem : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid InventoryListTemplateId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ItemCategory Category { get; set; }

    public int Quantity { get; set; }

    public ItemPriority Priority { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public InventoryListTemplate InventoryListTemplate { get; set; } = null!;
}
