using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Relocations;

public sealed class UpdateRelocationTaskRequest
{
    [JsonRequired]
    public string Title { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }

    [JsonRequired]
    public TaskPhase Phase { get; set; }

    [JsonRequired]
    public TaskPriority Priority { get; set; }

    [JsonRequired]
    public RelocationTaskStatus Status { get; set; }
}
