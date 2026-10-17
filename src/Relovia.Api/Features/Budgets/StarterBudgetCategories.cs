namespace Relovia.Api.Features.Budgets;

/// <summary>The categories a new budget starts with. The user can rename or delete them.</summary>
public static class StarterBudgetCategories
{
    public static readonly IReadOnlyList<string> Names =
    [
        "Housing",
        "Transport",
        "Visa and legal",
        "Moving",
        "Furnishing",
        "Living costs",
        "Other",
    ];
}
