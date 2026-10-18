namespace Relovia.Api.Features.Inventory;

public sealed record InventoryItemResponse(
    Guid Id,
    string Name,
    ItemCategory? Category,
    ItemRoom? Room,
    int Quantity,
    AcquisitionStatus Acquisition,
    MovingAction? MovingAction,
    PackingStatus? PackingStatus,
    ItemPriority Priority,
    decimal? EstimatedPrice,
    decimal? ActualPrice,
    string? CurrencyCode,
    Guid? BudgetCategoryId,
    string? Notes)
{
    public static InventoryItemResponse From(InventoryItem item)
    {
        return new InventoryItemResponse(
            item.Id,
            item.Name,
            item.Category,
            item.Room,
            item.Quantity,
            item.Acquisition,
            item.MovingAction,
            item.PackingStatus,
            item.Priority,
            item.EstimatedPrice,
            item.ActualPrice,
            item.CurrencyCode,
            item.BudgetCategoryId,
            item.Notes);
    }
}
