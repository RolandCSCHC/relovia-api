using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Inventory;

/// <summary>
/// A published room shopping list. Confirming a selection copies items onto the relocation.
/// A published list is not edited in place; a later version is a new row.
/// </summary>
public sealed class InventoryListTemplate : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Code { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public string Title { get; set; } = string.Empty;

    public ItemRoom Room { get; set; }

    public FurnishingScope Scope { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<InventoryTemplateItem> Items { get; } = new List<InventoryTemplateItem>();

    public bool IsPublished => PublishedAt is not null;
}
