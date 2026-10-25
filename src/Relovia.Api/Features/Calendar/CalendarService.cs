using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Calendar;

public sealed class CalendarService(ReloviaDbContext db)
{
    public const string RelocationNotFoundTitle = "Relocation was not found.";

    public const string EventNotFoundTitle = "Event was not found.";

    public async Task<CalendarResult<RelocationCalendarResponse>> GetAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new CalendarResult<RelocationCalendarResponse>.NotFound(RelocationNotFoundTitle);
        }

        var events = await db.CalendarEvents.AsNoTracking()
            .Where(calendarEvent => calendarEvent.RelocationId == relocationId)
            .ToListAsync(cancellationToken);
        var tasks = await db.RelocationTasks.AsNoTracking()
            .Where(task => task.RelocationId == relocationId && task.DueDate != null)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .Select(task => new TaskDueDateResponse(task.Id, task.Title, task.DueDate!.Value))
            .ToListAsync(cancellationToken);

        return new CalendarResult<RelocationCalendarResponse>.Success(
            new RelocationCalendarResponse(Order(events), tasks));
    }

    public async Task<CalendarResult<CalendarEventResponse>> CreateAsync(
        Guid userId,
        Guid relocationId,
        SaveCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new CalendarResult<CalendarEventResponse>.NotFound(RelocationNotFoundTitle);
        }

        var errors = CalendarEventRules.Validate(request);
        if (errors.Count > 0)
        {
            return new CalendarResult<CalendarEventResponse>.Invalid(errors);
        }

        var calendarEvent = new CalendarEvent { RelocationId = relocationId };
        Apply(calendarEvent, request);
        db.CalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync(cancellationToken);
        return new CalendarResult<CalendarEventResponse>.Success(CalendarEventResponse.From(calendarEvent));
    }

    public async Task<CalendarResult<CalendarEventResponse>> UpdateAsync(
        Guid userId,
        Guid relocationId,
        Guid eventId,
        SaveCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new CalendarResult<CalendarEventResponse>.NotFound(RelocationNotFoundTitle);
        }

        var errors = CalendarEventRules.Validate(request);
        if (errors.Count > 0)
        {
            return new CalendarResult<CalendarEventResponse>.Invalid(errors);
        }

        var calendarEvent = await FindAsync(relocationId, eventId, cancellationToken);
        if (calendarEvent is null)
        {
            return new CalendarResult<CalendarEventResponse>.NotFound(EventNotFoundTitle);
        }

        Apply(calendarEvent, request);
        await db.SaveChangesAsync(cancellationToken);
        return new CalendarResult<CalendarEventResponse>.Success(CalendarEventResponse.From(calendarEvent));
    }

    public async Task<CalendarResult<Guid>> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new CalendarResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var calendarEvent = await FindAsync(relocationId, eventId, cancellationToken);
        if (calendarEvent is null)
        {
            return new CalendarResult<Guid>.NotFound(EventNotFoundTitle);
        }

        db.CalendarEvents.Remove(calendarEvent);
        await db.SaveChangesAsync(cancellationToken);
        return new CalendarResult<Guid>.Success(eventId);
    }

    private static void Apply(CalendarEvent calendarEvent, SaveCalendarEventRequest request)
    {
        calendarEvent.Title = CalendarEventRules.NormalizeTitle(request.Title);
        calendarEvent.Kind = request.Kind;
        calendarEvent.Location = CalendarEventRules.NormalizeLocation(request.Location);
        if (request.Kind == CalendarEventKind.Milestone)
        {
            calendarEvent.OccursOn = request.OccursOn;
            calendarEvent.StartsAt = null;
            calendarEvent.EndsAt = null;
            return;
        }

        calendarEvent.OccursOn = null;
        calendarEvent.StartsAt = request.StartsAt;
        calendarEvent.EndsAt = request.EndsAt;
    }

    private static IReadOnlyList<CalendarEventResponse> Order(IReadOnlyList<CalendarEvent> events)
    {
        return events
            .OrderBy(calendarEvent => calendarEvent.Kind == CalendarEventKind.Milestone
                ? calendarEvent.OccursOn!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                : calendarEvent.StartsAt!.Value.UtcDateTime)
            .ThenBy(calendarEvent => calendarEvent.Title, StringComparer.Ordinal)
            .ThenBy(calendarEvent => calendarEvent.Id)
            .Select(CalendarEventResponse.From)
            .ToArray();
    }

    private async Task<CalendarEvent?> FindAsync(
        Guid relocationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await db.CalendarEvents.SingleOrDefaultAsync(
            calendarEvent => calendarEvent.Id == eventId && calendarEvent.RelocationId == relocationId,
            cancellationToken);
    }

    private async Task<bool> OwnsRelocationAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AsNoTracking()
            .AnyAsync(relocation => relocation.Id == relocationId && relocation.UserId == userId, cancellationToken);
    }
}
