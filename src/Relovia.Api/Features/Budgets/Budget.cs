using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Budgets;

/// <summary>
/// The budget of one relocation. Its currency is the one estimates are written in and the one
/// actual expenses are compared against.
/// </summary>
public sealed class Budget : ICreationTimestamp, IUpdateTimestamp
{
    public Guid RelocationId { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional monthly net income, stored as the user entered it. It is not an estimate or an expense.
    /// </summary>
    public decimal? MonthlyNetIncome { get; set; }

    public string? MonthlyNetIncomeCurrency { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;

    public ICollection<BudgetCategory> Categories { get; } = new List<BudgetCategory>();
}
