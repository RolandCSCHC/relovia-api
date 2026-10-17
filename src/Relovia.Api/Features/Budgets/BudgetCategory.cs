using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Budgets;

public sealed class BudgetCategory : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Budget Budget { get; set; } = null!;

    public ICollection<BudgetEstimate> Estimates { get; } = new List<BudgetEstimate>();

    public ICollection<Expense> Expenses { get; } = new List<Expense>();
}
