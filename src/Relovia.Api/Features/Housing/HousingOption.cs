using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Housing;

/// <summary>
/// A home the user is comparing. Amounts stay as entered. Choosing one does not change the budget.
/// </summary>
public sealed class HousingOption : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Area { get; set; }

    public decimal? Rent { get; set; }

    public decimal? Deposit { get; set; }

    public string? CurrencyCode { get; set; }

    public string? Size { get; set; }

    public HousingFurnishing? Furnishing { get; set; }

    public DateOnly? AvailableOn { get; set; }

    public HousingStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;
}
