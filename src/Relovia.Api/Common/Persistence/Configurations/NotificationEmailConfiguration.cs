using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Notifications;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class NotificationEmailConfiguration : IEntityTypeConfiguration<NotificationEmail>
{
    public void Configure(EntityTypeBuilder<NotificationEmail> builder)
    {
        builder.HasKey(email => email.Id);

        builder.Property(email => email.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(email => email.Attempts)
            .IsRequired();

        builder.Property(email => email.NextAttemptAt)
            .HasColumnType("timestamptz");

        builder.Property(email => email.SentAt)
            .HasColumnType("timestamptz");

        builder.Property(email => email.LastError)
            .HasMaxLength(ReminderRules.ErrorMaxLength);

        builder.Property(email => email.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(email => email.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(email => email.NotificationId)
            .IsUnique()
            .HasDatabaseName("ux_notification_emails_notification_id");

        builder.HasIndex(email => new { email.Status, email.NextAttemptAt })
            .HasDatabaseName("ix_notification_emails_status_next_attempt_at");

        builder.HasOne(email => email.Notification)
            .WithOne()
            .HasForeignKey<NotificationEmail>(email => email.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("notification_emails", table =>
        {
            table.HasCheckConstraint(
                "ck_notification_emails_status",
                "status IN ('Pending', 'Sent', 'Failed')");
            table.HasCheckConstraint(
                "ck_notification_emails_attempts",
                $"attempts >= 0 AND attempts <= {ReminderRules.MaxEmailAttempts}");
        });
    }
}
