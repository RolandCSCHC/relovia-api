namespace Relovia.Api.Features.Inventory;

public abstract record InventoryResult<T>
{
    private InventoryResult()
    {
    }

    public sealed record Success(T Value) : InventoryResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : InventoryResult<T>;

    /// <summary>The relocation, or an item inside it, does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : InventoryResult<T>;
}
