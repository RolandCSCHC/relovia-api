using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Relocations;

public sealed class ConfirmChecklistRequest
{
    [JsonRequired]
    public List<ConfirmChecklistTaskRequest> Tasks { get; set; } = [];
}

public sealed class ConfirmChecklistTaskRequest
{
    [JsonRequired]
    public string Code { get; set; } = string.Empty;

    [JsonRequired]
    public string Title { get; set; } = string.Empty;

    [JsonRequired]
    public int DaysBeforeMove { get; set; }

    [JsonRequired]
    public TaskPhase Phase { get; set; }

    [JsonRequired]
    public TaskPriority Priority { get; set; }
}
