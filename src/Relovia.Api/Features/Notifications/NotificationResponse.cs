namespace Relovia.Api.Features.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid RelocationId,
    string RelocationName,
    ReminderKind Kind,
    string Title,
    DateOnly OccursOn,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationPreferenceResponse(NotificationChannel Channel, int DaysBefore)
{
    public static NotificationPreferenceResponse Default { get; } = new(
        ReminderRules.DefaultChannel,
        ReminderRules.DefaultDaysBefore);

    public static NotificationPreferenceResponse From(NotificationPreference preference)
    {
        return new NotificationPreferenceResponse(preference.Channel, preference.DaysBefore);
    }
}
