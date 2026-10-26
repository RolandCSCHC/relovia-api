using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Relocations;

public sealed class CreateRelocationTaskRequest
{
    [JsonRequired]
    public string Title { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }

    [JsonRequired]
    public TaskPhase Phase { get; set; }

    [JsonRequired]
    public TaskPriority Priority { get; set; }

    public string? Description { get; set; }

    public string? Url { get; set; }
}
