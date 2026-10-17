using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Budgets;

public sealed class SaveBudgetRequest
{
    [JsonRequired]
    public string CurrencyCode { get; set; } = string.Empty;
}

public sealed class SaveBudgetCategoryRequest
{
    [JsonRequired]
    public string Name { get; set; } = string.Empty;
}

public sealed class SaveBudgetEstimateRequest
{
    [JsonRequired]
    public Guid CategoryId { get; set; }

    [JsonRequired]
    public string Label { get; set; } = string.Empty;

    [JsonRequired]
    public decimal Amount { get; set; }

    [JsonRequired]
    public string CurrencyCode { get; set; } = string.Empty;
}

public sealed class SaveExpenseRequest
{
    [JsonRequired]
    public Guid CategoryId { get; set; }

    public Guid? TaskId { get; set; }

    [JsonRequired]
    public string Description { get; set; } = string.Empty;

    [JsonRequired]
    public decimal Amount { get; set; }

    [JsonRequired]
    public string CurrencyCode { get; set; } = string.Empty;

    [JsonRequired]
    public DateOnly PaidOn { get; set; }

    public string? Notes { get; set; }
}
