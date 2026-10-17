namespace Relovia.Api.Features.Budgets;

public abstract record BudgetResult<T>
{
    private BudgetResult()
    {
    }

    public sealed record Success(T Value) : BudgetResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : BudgetResult<T>;

    /// <summary>The relocation, or an item inside it, does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : BudgetResult<T>;

    /// <summary>The request is well formed but conflicts with the current budget.</summary>
    public sealed record Conflict(string Title) : BudgetResult<T>;
}
