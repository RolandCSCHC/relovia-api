using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Notifications;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(preference => preference.UserId)
            .HasName("pk_notification_preferences");

        builder.Property(preference => preference.Channel)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(preference => preference.DaysBefore)
            .IsRequired();

        builder.Property(preference => preference.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(preference => preference.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasOne(preference => preference.User)
            .WithMany()
            .HasForeignKey(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("notification_preferences", table =>
        {
            table.HasCheckConstraint(
                "ck_notification_preferences_channel",
                "channel IN ('None', 'InApp', 'Email', 'Both')");
            table.HasCheckConstraint(
                "ck_notification_preferences_days_before",
                $"days_before >= {ReminderRules.MinDaysBefore} AND days_before <= {ReminderRules.MaxDaysBefore}");
        });
    }
}
