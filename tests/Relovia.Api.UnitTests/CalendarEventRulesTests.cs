using Relovia.Api.Features.Calendar;

namespace Relovia.Api.UnitTests;

public sealed class CalendarEventRulesTests
{
    [Fact]
    public void A_milestone_is_a_date_and_an_appointment_is_a_time()
    {
        var milestone = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = " Move day ",
            Kind = CalendarEventKind.Milestone,
            OccursOn = new DateOnly(2026, 12, 1),
            Location = "  ",
        });
        var appointment = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Key handover",
            Kind = CalendarEventKind.Appointment,
            StartsAt = At(15, 0),
            EndsAt = At(16, 0),
            Location = " Landlord's office ",
        });
        var openEnded = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Flight",
            Kind = CalendarEventKind.Appointment,
            StartsAt = At(9, 0),
        });

        Assert.Empty(milestone);
        Assert.Empty(appointment);
        Assert.Empty(openEnded);
        Assert.Equal("Move day", CalendarEventRules.NormalizeTitle(" Move day "));
        Assert.Null(CalendarEventRules.NormalizeLocation("  "));
        Assert.Equal("Landlord's office", CalendarEventRules.NormalizeLocation(" Landlord's office "));
    }

    [Fact]
    public void A_milestone_cannot_carry_a_time_and_an_appointment_cannot_carry_a_date()
    {
        var datedMilestone = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Move day",
            Kind = CalendarEventKind.Milestone,
            OccursOn = new DateOnly(2026, 12, 1),
            StartsAt = At(9, 0),
        });
        var undatedMilestone = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Move day",
            Kind = CalendarEventKind.Milestone,
        });
        var datedAppointment = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Key handover",
            Kind = CalendarEventKind.Appointment,
            OccursOn = new DateOnly(2026, 12, 1),
            StartsAt = At(15, 0),
        });
        var timelessAppointment = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Key handover",
            Kind = CalendarEventKind.Appointment,
        });

        Assert.Equal([CalendarEventRules.MilestoneHasTimeMessage], datedMilestone["startsAt"]);
        Assert.Equal([CalendarEventRules.MilestoneDateRequiredMessage], undatedMilestone["occursOn"]);
        Assert.Equal([CalendarEventRules.AppointmentHasDateMessage], datedAppointment["occursOn"]);
        Assert.Equal([CalendarEventRules.AppointmentStartRequiredMessage], timelessAppointment["startsAt"]);
    }

    [Fact]
    public void An_appointment_end_must_be_after_the_start()
    {
        var sameTime = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Key handover",
            Kind = CalendarEventKind.Appointment,
            StartsAt = At(15, 0),
            EndsAt = At(15, 0),
        });
        var earlier = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Key handover",
            Kind = CalendarEventKind.Appointment,
            StartsAt = At(15, 0),
            EndsAt = At(14, 0),
        });

        Assert.Equal([CalendarEventRules.EndBeforeStartMessage], sameTime["endsAt"]);
        Assert.Equal([CalendarEventRules.EndBeforeStartMessage], earlier["endsAt"]);
    }

    [Fact]
    public void A_blank_title_or_an_unknown_kind_is_rejected()
    {
        var blank = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "  ",
            Kind = CalendarEventKind.Milestone,
            OccursOn = new DateOnly(2026, 12, 1),
        });
        var tooLong = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = new string('a', CalendarEventRules.TitleMaxLength + 1),
            Kind = CalendarEventKind.Milestone,
            OccursOn = new DateOnly(2026, 12, 1),
        });
        var unknown = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Move day",
            Kind = (CalendarEventKind)99,
            OccursOn = new DateOnly(2026, 12, 1),
        });
        var longLocation = CalendarEventRules.Validate(new SaveCalendarEventRequest
        {
            Title = "Move day",
            Kind = CalendarEventKind.Milestone,
            OccursOn = new DateOnly(2026, 12, 1),
            Location = new string('a', CalendarEventRules.LocationMaxLength + 1),
        });

        Assert.Equal([CalendarEventRules.TitleRequiredMessage], blank["title"]);
        Assert.Equal([CalendarEventRules.TitleTooLongMessage], tooLong["title"]);
        Assert.Equal([CalendarEventRules.UnknownKindMessage], unknown["kind"]);
        Assert.Equal([CalendarEventRules.LocationTooLongMessage], longLocation["location"]);
    }

    private static DateTimeOffset At(int hour, int minute)
    {
        return new DateTimeOffset(2026, 11, 15, hour, minute, 0, TimeSpan.Zero);
    }
}
