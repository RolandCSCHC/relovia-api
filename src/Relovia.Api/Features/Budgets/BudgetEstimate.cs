using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Budgets;

/// <summary>
/// A planned amount in the currency the user expects to pay it in. The budget converts it to the
/// budget currency with the latest rates.
/// </summary>
public sealed class BudgetEstimate : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public Guid CategoryId { get; set; }

    public string Label { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public BudgetCategory Category { get; set; } = null!;
}
