using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Inventory;

public sealed class SaveInventoryItemRequest
{
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    public ItemCategory? Category { get; set; }

    public ItemRoom? Room { get; set; }

    [JsonRequired]
    public int Quantity { get; set; }

    [JsonRequired]
    public AcquisitionStatus Acquisition { get; set; }

    public MovingAction? MovingAction { get; set; }

    public PackingStatus? PackingStatus { get; set; }

    [JsonRequired]
    public ItemPriority Priority { get; set; }

    public decimal? EstimatedPrice { get; set; }

    public decimal? ActualPrice { get; set; }

    public string? CurrencyCode { get; set; }

    public Guid? BudgetCategoryId { get; set; }

    public string? Notes { get; set; }
}
