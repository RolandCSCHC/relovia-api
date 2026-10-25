using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Calendar;

public sealed class SaveCalendarEventRequest
{
    [JsonRequired]
    public string Title { get; set; } = string.Empty;

    [JsonRequired]
    public CalendarEventKind Kind { get; set; }

    public DateOnly? OccursOn { get; set; }

    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public string? Location { get; set; }
}
