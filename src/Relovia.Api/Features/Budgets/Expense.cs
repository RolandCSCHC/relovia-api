using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Budgets;

/// <summary>
/// Money that was actually spent. The amount and currency are stored as the user paid them
/// and are never recomputed.
/// </summary>
public sealed class Expense : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? TaskId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public DateOnly PaidOn { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;

    public BudgetCategory Category { get; set; } = null!;

    public RelocationTask? Task { get; set; }
}
