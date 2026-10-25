using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Calendar;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.HasKey(calendarEvent => calendarEvent.Id);

        builder.Property(calendarEvent => calendarEvent.Title)
            .HasMaxLength(CalendarEventRules.TitleMaxLength)
            .IsRequired()
            .HasConversion(title => title.Trim(), title => title);

        builder.Property(calendarEvent => calendarEvent.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(calendarEvent => calendarEvent.OccursOn)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(calendarEvent => calendarEvent.StartsAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(calendarEvent => calendarEvent.EndsAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(calendarEvent => calendarEvent.Location)
            .HasMaxLength(CalendarEventRules.LocationMaxLength)
            .IsRequired(false);

        builder.Property(calendarEvent => calendarEvent.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(calendarEvent => calendarEvent.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(calendarEvent => calendarEvent.RelocationId)
            .HasDatabaseName("ix_calendar_events_relocation_id");

        builder.HasOne(calendarEvent => calendarEvent.Relocation)
            .WithMany()
            .HasForeignKey(calendarEvent => calendarEvent.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("calendar_events", table =>
        {
            table.HasCheckConstraint(
                "ck_calendar_events_title",
                $"char_length(btrim(title)) > 0 AND char_length(title) <= {CalendarEventRules.TitleMaxLength}");
            table.HasCheckConstraint(
                "ck_calendar_events_location",
                $"location IS NULL OR (char_length(btrim(location)) > 0 AND char_length(location) <= {CalendarEventRules.LocationMaxLength})");
            table.HasCheckConstraint(
                "ck_calendar_events_kind",
                """
                (kind = 'Milestone' AND occurs_on IS NOT NULL AND starts_at IS NULL AND ends_at IS NULL)
                OR (kind = 'Appointment' AND occurs_on IS NULL AND starts_at IS NOT NULL AND (ends_at IS NULL OR ends_at > starts_at))
                """);
        });
    }
}
