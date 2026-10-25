using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Calendar;

/// <summary>
/// A date the user put on the calendar. A task due date is not stored here.
/// </summary>
public sealed class CalendarEvent : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string Title { get; set; } = string.Empty;

    public CalendarEventKind Kind { get; set; }

    /// <summary>The calendar date of a milestone. Empty for an appointment.</summary>
    public DateOnly? OccursOn { get; set; }

    /// <summary>When an appointment starts. Empty for a milestone.</summary>
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>When an appointment ends. Empty when the appointment has no end, and for a milestone.</summary>
    public DateTimeOffset? EndsAt { get; set; }

    public string? Location { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;
}
