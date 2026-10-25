namespace Relovia.Api.Features.Calendar;

public sealed record CalendarEventResponse(
    Guid Id,
    string Title,
    CalendarEventKind Kind,
    DateOnly? OccursOn,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location)
{
    public static CalendarEventResponse From(CalendarEvent calendarEvent)
    {
        return new CalendarEventResponse(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Kind,
            calendarEvent.OccursOn,
            calendarEvent.StartsAt,
            calendarEvent.EndsAt,
            calendarEvent.Location);
    }
}

/// <summary>A task due date shown on the calendar. It is not a calendar event.</summary>
public sealed record TaskDueDateResponse(Guid TaskId, string Title, DateOnly DueDate);

public sealed record RelocationCalendarResponse(
    IReadOnlyList<CalendarEventResponse> Events,
    IReadOnlyList<TaskDueDateResponse> TaskDueDates);
