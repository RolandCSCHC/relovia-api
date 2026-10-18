using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Inventory;

/// <summary>
/// One belonging for a relocation, whether the user already owns it or still needs to buy it.
/// Prices are the original amounts in <see cref="CurrencyCode"/> and are never recomputed.
/// </summary>
public sealed class InventoryItem : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ItemCategory? Category { get; set; }

    public ItemRoom? Room { get; set; }

    public int Quantity { get; set; }

    public AcquisitionStatus Acquisition { get; set; }

    public MovingAction? MovingAction { get; set; }

    /// <summary>Set only when <see cref="MovingAction"/> is <see cref="Inventory.MovingAction.Bring"/>.</summary>
    public PackingStatus? PackingStatus { get; set; }

    public ItemPriority Priority { get; set; }

    public decimal? EstimatedPrice { get; set; }

    public decimal? ActualPrice { get; set; }

    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Set for an item that still needs to be bought, so its prices count in that budget category.
    /// </summary>
    public Guid? BudgetCategoryId { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Copied from a room list. Empty when the user added the item by hand.
    /// A later list skips a code that is already stored.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>The list row this item was copied from. Cleared if that row is removed.</summary>
    public Guid? SourceTemplateItemId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;

    public BudgetCategory? BudgetCategory { get; set; }

    public InventoryTemplateItem? SourceTemplateItem { get; set; }
}
