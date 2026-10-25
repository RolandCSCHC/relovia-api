using Relovia.Api.Features.Notifications;

namespace Relovia.Api.UnitTests;

public sealed class ReminderRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 1);

    [Fact]
    public void A_reminder_covers_the_lead_day_through_the_day_itself()
    {
        Assert.True(ReminderRules.IsInsideWindow(Today, Today, daysBefore: 0));
        Assert.True(ReminderRules.IsInsideWindow(Today.AddDays(3), Today, daysBefore: 3));
        Assert.False(ReminderRules.IsInsideWindow(Today.AddDays(4), Today, daysBefore: 3));
        Assert.False(ReminderRules.IsInsideWindow(Today.AddDays(-1), Today, daysBefore: 3));
    }

    [Fact]
    public void In_app_and_email_are_chosen_separately()
    {
        Assert.True(ReminderRules.ShowsInApp(NotificationChannel.InApp));
        Assert.False(ReminderRules.SendsEmail(NotificationChannel.InApp));
        Assert.False(ReminderRules.ShowsInApp(NotificationChannel.Email));
        Assert.True(ReminderRules.SendsEmail(NotificationChannel.Email));
        Assert.True(ReminderRules.ShowsInApp(NotificationChannel.Both));
        Assert.True(ReminderRules.SendsEmail(NotificationChannel.Both));
        Assert.False(ReminderRules.ShowsInApp(NotificationChannel.None));
        Assert.False(ReminderRules.SendsEmail(NotificationChannel.None));
    }

    [Fact]
    public void A_retry_waits_longer_each_time()
    {
        var failedAt = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(failedAt.AddMinutes(1), ReminderRules.NextAttempt(failedAt, attempts: 1));
        Assert.Equal(failedAt.AddMinutes(5), ReminderRules.NextAttempt(failedAt, attempts: 2));
        Assert.Equal(failedAt.AddMinutes(15), ReminderRules.NextAttempt(failedAt, attempts: 3));
        Assert.Equal(failedAt.AddHours(1), ReminderRules.NextAttempt(failedAt, attempts: 4));
    }

    [Fact]
    public void A_lead_time_outside_0_to_90_days_is_rejected()
    {
        var tooSoon = ReminderRules.Validate(new SaveNotificationPreferenceRequest
        {
            Channel = NotificationChannel.InApp,
            DaysBefore = -1,
        });
        var tooFar = ReminderRules.Validate(new SaveNotificationPreferenceRequest
        {
            Channel = NotificationChannel.Both,
            DaysBefore = 91,
        });
        var unknown = ReminderRules.Validate(new SaveNotificationPreferenceRequest
        {
            Channel = (NotificationChannel)99,
            DaysBefore = 3,
        });
        var accepted = ReminderRules.Validate(new SaveNotificationPreferenceRequest
        {
            Channel = NotificationChannel.Email,
            DaysBefore = 0,
        });

        Assert.Equal([ReminderRules.DaysBeforeMessage], tooSoon["daysBefore"]);
        Assert.Equal([ReminderRules.DaysBeforeMessage], tooFar["daysBefore"]);
        Assert.Equal([ReminderRules.UnknownChannelMessage], unknown["channel"]);
        Assert.Empty(accepted);
    }

    [Fact]
    public void The_email_names_the_move_and_the_date()
    {
        var text = ReminderRules.Text(
            "Wellington move",
            ReminderKind.TaskDue,
            "Give notice",
            new DateOnly(2026, 11, 3),
            "https://relovia.example/relocations/1");

        Assert.Contains("Reminder for Wellington move.", text, StringComparison.Ordinal);
        Assert.Contains("Give notice is due on Tuesday, 3 November 2026.", text, StringComparison.Ordinal);
        Assert.Contains("Open this move in Relovia:\nhttps://relovia.example/relocations/1", text, StringComparison.Ordinal);
        Assert.Equal("Relovia reminder: Give notice", ReminderRules.Subject("Give notice"));

        var milestone = ReminderRules.Text(
            "asdf",
            ReminderKind.Milestone,
            "asdf",
            new DateOnly(2026, 10, 30),
            link: null);
        Assert.Contains("asdf, a milestone, is on Friday, 30 October 2026.", milestone, StringComparison.Ordinal);
    }
}
