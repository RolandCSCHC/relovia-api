using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.Notifications;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task A_reminder_is_created_once_and_its_email_is_sent_once()
    {
        var owner = await SignInAsync("reminders-owner@example.com");
        var other = await SignInAsync("reminders-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var sender = _factory.Services.GetRequiredService<RecordingNotificationEmailSender>();
        sender.Reset();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var relocation = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Wellington", targetDate: today.AddDays(30), destinationCountry: "NZ"));
        var relocationId = relocation.Body!.Id;

        var anonymous = await CreateClient().Client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var defaults = await SendAsync<NotificationPreferenceResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            "/api/notifications/preferences",
            body: null);
        Assert.Equal(HttpStatusCode.OK, defaults.Status);
        Assert.Equal(NotificationChannel.InApp, defaults.Body!.Channel);
        Assert.Equal(3, defaults.Body.DaysBefore);

        var rejected = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            "/api/notifications/preferences",
            new { channel = "InApp", daysBefore = 91 });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.Status);

        var saved = await SendAsync<NotificationPreferenceResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            "/api/notifications/preferences",
            new { channel = "InApp", daysBefore = 2 });
        Assert.Equal(HttpStatusCode.OK, saved.Status);
        Assert.Equal(2, saved.Body!.DaysBefore);

        var inside = await AddTaskAsync(ownerClient, owner.Token, relocationId, "Give notice", today.AddDays(2));
        var outside = await AddTaskAsync(ownerClient, owner.Token, relocationId, "Book movers", today.AddDays(10));
        var past = await AddTaskAsync(ownerClient, owner.Token, relocationId, "Old form", today.AddDays(-1));
        var milestone = await SendAsync<CalendarEventResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/events",
            new
            {
                title = "Move day",
                kind = "Milestone",
                occursOn = today.AddDays(2),
                startsAt = (DateTimeOffset?)null,
                endsAt = (DateTimeOffset?)null,
                location = (string?)null,
            });
        Assert.Equal(HttpStatusCode.Created, milestone.Status);
        var uploaded = await UploadAsync<DocumentResponse>(
            ownerClient,
            owner.Token,
            $"/api/relocations/{relocationId}/documents",
            VisaPdf,
            "visa.pdf",
            "application/pdf");
        Assert.Equal(HttpStatusCode.Created, uploaded.Status);
        var dated = await SendAsync<DocumentResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/documents/{uploaded.Body!.Id}",
            new { category = "Visa", expiresOn = today.AddDays(2), taskIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.OK, dated.Status);

        await ScanAsync();

        var listed = await SendAsync<List<NotificationResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            "/api/notifications",
            body: null);
        Assert.Equal(HttpStatusCode.OK, listed.Status);
        Assert.Equal(["Give notice", "Move day", "visa.pdf"], listed.Body!.Select(notification => notification.Title).OrderBy(title => title));
        Assert.DoesNotContain(listed.Body!, notification => notification.Title is "Book movers" or "Old form");
        Assert.All(listed.Body!, notification => Assert.Null(notification.ReadAt));
        Assert.Empty(sender.Sent);
        await using (var db = _postgres.CreateContext())
        {
            Assert.Empty(await db.NotificationEmails
                .Where(email => email.Notification.RelocationId == relocationId)
                .ToListAsync());
        }

        await ScanAsync();
        var again = await SendAsync<List<NotificationResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            "/api/notifications",
            body: null);
        Assert.Equal(3, again.Body!.Count);

        var emailed = await SendAsync<NotificationPreferenceResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            "/api/notifications/preferences",
            new { channel = "Both", daysBefore = 2 });
        Assert.Equal(HttpStatusCode.OK, emailed.Status);
        var fresh = await AddTaskAsync(ownerClient, owner.Token, relocationId, "Collect keys", today.AddDays(1));
        await ScanAsync();

        var withEmail = await SendAsync<List<NotificationResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            "/api/notifications",
            body: null);
        Assert.Contains(withEmail.Body!, notification => notification.Title == "Collect keys");
        Assert.Equal(4, withEmail.Body!.Count);
        Guid emailId;
        await using (var db = _postgres.CreateContext())
        {
            var pending = await db.NotificationEmails
                .Include(email => email.Notification)
                .SingleAsync(email => email.Notification.RelocationId == relocationId);
            Assert.Equal(fresh, pending.Notification.SourceId);
            Assert.Equal(NotificationEmailStatus.Pending, pending.Status);
            emailId = pending.Id;
        }

        await DeliverAsync();
        Assert.Equal(emailId, sender.Sent.Single().IdempotencyKey);
        Assert.Contains("Collect keys is due", sender.Sent[0].Text, StringComparison.Ordinal);
        Assert.Contains("Wellington", sender.Sent[0].Text, StringComparison.Ordinal);
        await DeliverAsync();
        Assert.Single(sender.Sent);
        await using (var db = _postgres.CreateContext())
        {
            var sent = await db.NotificationEmails.SingleAsync(email => email.Id == emailId);
            Assert.Equal(NotificationEmailStatus.Sent, sent.Status);
            Assert.NotNull(sent.SentAt);
        }

        var hidden = await SendAsync<List<NotificationResponse>>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            "/api/notifications",
            body: null);
        Assert.Equal(HttpStatusCode.OK, hidden.Status);
        Assert.Empty(hidden.Body!);
        var stolen = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Post,
            $"/api/notifications/{withEmail.Body[0].Id}/read",
            new { });
        Assert.Equal(HttpStatusCode.NotFound, stolen.Status);

        var read = await SendAsync<NotificationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/notifications/{withEmail.Body.Single(notification => notification.Title == "Give notice").Id}/read",
            new { });
        Assert.Equal(HttpStatusCode.OK, read.Status);
        Assert.NotNull(read.Body!.ReadAt);
        var reread = await SendAsync<NotificationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/notifications/{read.Body.Id}/read",
            new { });
        Assert.Equal(read.Body.ReadAt, reread.Body!.ReadAt);

        var all = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/notifications/read",
            new { });
        Assert.Equal(HttpStatusCode.NoContent, all.Status);
        var done = await SendAsync<List<NotificationResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            "/api/notifications",
            body: null);
        Assert.All(done.Body!, notification => Assert.NotNull(notification.ReadAt));

        var removed = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removed.Status);
        await using var remaining = _postgres.CreateContext();
        Assert.Empty(await remaining.Notifications.Where(notification => notification.RelocationId == relocationId).ToListAsync());
        Assert.Empty(await remaining.NotificationEmails.Where(email => email.Id == emailId).ToListAsync());

        _ = inside;
        _ = outside;
        _ = past;
    }

    [Fact]
    public async Task A_failed_reminder_email_is_retried_and_then_stopped()
    {
        var owner = await SignInAsync("reminders-retry@example.com");
        using var client = CreateClient();
        var sender = _factory.Services.GetRequiredService<RecordingNotificationEmailSender>();
        sender.Reset();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var relocation = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Retry", targetDate: today.AddDays(30), destinationCountry: "DE"));
        await SendAsync<NotificationPreferenceResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            "/api/notifications/preferences",
            new { channel = "Email", daysBefore = 1 });
        await AddTaskAsync(client, owner.Token, relocation.Body!.Id, "Call the landlord", today);
        await ScanAsync();

        var hidden = await SendAsync<List<NotificationResponse>>(
            client,
            owner.Token,
            HttpMethod.Get,
            "/api/notifications",
            body: null);
        Assert.Empty(hidden.Body!);

        sender.FailuresRemaining = 1;
        await DeliverAsync();
        Assert.Equal(1, sender.Calls);
        Assert.Empty(sender.Sent);
        await DeliverAsync();
        Assert.Equal(1, sender.Calls);

        await using (var db = _postgres.CreateContext())
        {
            var pending = await db.NotificationEmails
                .SingleAsync(email => email.Notification.RelocationId == relocation.Body.Id);
            Assert.Equal(NotificationEmailStatus.Pending, pending.Status);
            Assert.Equal(1, pending.Attempts);
            pending.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        await DeliverAsync();
        Assert.Equal(2, sender.Calls);
        Assert.Single(sender.Sent);
        await DeliverAsync();
        Assert.Equal(2, sender.Calls);

        sender.Reset();
        sender.FailuresRemaining = 1;
        await using (var db = _postgres.CreateContext())
        {
            var sent = await db.NotificationEmails
                .SingleAsync(email => email.Notification.RelocationId == relocation.Body.Id);
            sent.Status = NotificationEmailStatus.Pending;
            sent.Attempts = ReminderRules.MaxEmailAttempts - 1;
            sent.SentAt = null;
            sent.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        await DeliverAsync();
        await using var remaining = _postgres.CreateContext();
        var failed = await remaining.NotificationEmails
            .SingleAsync(email => email.Notification.RelocationId == relocation.Body.Id);
        Assert.Equal(NotificationEmailStatus.Failed, failed.Status);
        Assert.Equal(ReminderRules.MaxEmailAttempts, failed.Attempts);
        Assert.Empty(sender.Sent);
    }

    private async Task ScanAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();
        await reminders.ScanAsync(CancellationToken.None);
    }

    private async Task DeliverAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();
        await reminders.DeliverAsync(CancellationToken.None);
    }

    private static async Task<Guid> AddTaskAsync(
        ClientScope scope,
        string token,
        Guid relocationId,
        string title,
        DateOnly dueDate)
    {
        var created = await SendAsync<RelocationTaskResponse>(
            scope,
            token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/tasks",
            new { title, dueDate, phase = "Housing", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!.Id;
    }
}

public sealed class RecordingNotificationEmailSender : INotificationEmailSender
{
    public List<NotificationEmailMessage> Sent { get; } = [];

    public int Calls { get; private set; }

    public int FailuresRemaining { get; set; }

    public void Reset()
    {
        Sent.Clear();
        Calls = 0;
        FailuresRemaining = 0;
    }

    public Task<bool> SendAsync(NotificationEmailMessage message, CancellationToken cancellationToken)
    {
        Calls++;
        if (FailuresRemaining > 0)
        {
            FailuresRemaining--;
            throw new HttpRequestException("Resend rejected the email.");
        }

        Sent.Add(message);
        return Task.FromResult(true);
    }
}
