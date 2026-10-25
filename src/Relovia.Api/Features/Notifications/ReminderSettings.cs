namespace Relovia.Api.Features.Notifications;

public sealed class ReminderSettings
{
    public const string SectionName = "Reminders";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
}
