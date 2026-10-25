using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Features.Notifications;

/// <summary>How far ahead, and on which channels, one user wants reminders.</summary>
public sealed class NotificationPreference : ICreationTimestamp, IUpdateTimestamp
{
    public Guid UserId { get; set; }

    public NotificationChannel Channel { get; set; } = ReminderRules.DefaultChannel;

    public int DaysBefore { get; set; } = ReminderRules.DefaultDaysBefore;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
