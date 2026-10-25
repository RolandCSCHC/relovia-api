using System.Globalization;

namespace Relovia.Api.Features.Notifications;

public static class ReminderRules
{
    public const int DefaultDaysBefore = 3;

    public const int MinDaysBefore = 0;

    public const int MaxDaysBefore = 90;

    public const int MaxEmailAttempts = 5;

    public const int ErrorMaxLength = 500;

    public const NotificationChannel DefaultChannel = NotificationChannel.InApp;

    public static readonly TimeSpan UnconfiguredDelay = TimeSpan.FromHours(1);

    public const string UnknownChannelMessage = "Choose how to be reminded.";

    public const string DaysBeforeMessage = "Remind between 0 and 90 days before.";

    /// <summary>
    /// True from the lead day through the day itself. A date that has already passed is left alone.
    /// </summary>
    public static bool IsInsideWindow(DateOnly occursOn, DateOnly today, int daysBefore)
    {
        if (daysBefore is < MinDaysBefore or > MaxDaysBefore)
        {
            return false;
        }

        return occursOn >= today && occursOn <= today.AddDays(daysBefore);
    }

    public static bool ShowsInApp(NotificationChannel channel)
    {
        return channel is NotificationChannel.InApp or NotificationChannel.Both;
    }

    public static bool SendsEmail(NotificationChannel channel)
    {
        return channel is NotificationChannel.Email or NotificationChannel.Both;
    }

    public static DateTimeOffset NextAttempt(DateTimeOffset failedAt, int attempts)
    {
        var delay = attempts switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromHours(1),
        };
        return failedAt.Add(delay);
    }

    public static Dictionary<string, string[]> Validate(SaveNotificationPreferenceRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!Enum.IsDefined(request.Channel))
        {
            errors["channel"] = [UnknownChannelMessage];
        }

        if (request.DaysBefore is < MinDaysBefore or > MaxDaysBefore)
        {
            errors["daysBefore"] = [DaysBeforeMessage];
        }

        return errors;
    }

    public static string Subject(string title)
    {
        return $"Relovia reminder: {title}";
    }

    public static string Text(
        string relocationName,
        ReminderKind kind,
        string title,
        DateOnly occursOn,
        string? link)
    {
        var when = occursOn.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
        var line = kind switch
        {
            ReminderKind.TaskDue => $"{title} is due on {when}.",
            ReminderKind.DocumentExpiration => $"{title} expires on {when}.",
            ReminderKind.Appointment => $"{title}, an appointment, is on {when}.",
            _ => $"{title}, a milestone, is on {when}.",
        };
        var text = $"""
            Reminder for {relocationName}.

            {line}
            """;
        if (!string.IsNullOrWhiteSpace(link))
        {
            text += $"\n\nOpen this move in Relovia:\n{link}";
        }

        return text;
    }

    public static string ClipError(string message)
    {
        var trimmed = message.Trim();
        return trimmed.Length <= ErrorMaxLength ? trimmed : trimmed[..ErrorMaxLength];
    }
}
