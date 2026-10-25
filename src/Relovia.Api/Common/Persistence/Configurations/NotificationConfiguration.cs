using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Notifications;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(title => title.Trim(), title => title);

        builder.Property(notification => notification.OccursOn)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(notification => notification.ShowInApp)
            .IsRequired();

        builder.Property(notification => notification.ReadAt)
            .HasColumnType("timestamptz");

        builder.Property(notification => notification.CreatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(notification => new { notification.UserId, notification.Kind, notification.SourceId })
            .IsUnique()
            .HasDatabaseName("ux_notifications_user_id_kind_source_id");

        builder.HasIndex(notification => notification.UserId)
            .HasDatabaseName("ix_notifications_user_id");

        builder.HasIndex(notification => notification.RelocationId)
            .HasDatabaseName("ix_notifications_relocation_id");

        builder.HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(notification => notification.Relocation)
            .WithMany()
            .HasForeignKey(notification => notification.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("notifications", table =>
        {
            table.HasCheckConstraint(
                "ck_notifications_kind",
                "kind IN ('TaskDue', 'Milestone', 'Appointment', 'DocumentExpiration')");
            table.HasCheckConstraint(
                "ck_notifications_title",
                "char_length(btrim(title)) > 0 AND char_length(title) <= 200");
        });
    }
}
