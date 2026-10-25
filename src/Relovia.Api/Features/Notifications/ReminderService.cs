using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Common.Security;
using Relovia.Api.Features.Calendar;

namespace Relovia.Api.Features.Notifications;

public sealed class ReminderService(
    ReloviaDbContext db,
    TimeProvider time,
    INotificationEmailSender sender,
    WebSettings web,
    ILogger<ReminderService> logger)
{
    public async Task<int> ScanAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var preferences = await db.NotificationPreferences.AsNoTracking()
            .ToDictionaryAsync(preference => preference.UserId, cancellationToken);
        var seen = (await db.Notifications.AsNoTracking()
            .Select(notification => new { notification.UserId, notification.Kind, notification.SourceId })
            .ToListAsync(cancellationToken))
            .Select(notification => (notification.UserId, notification.Kind, notification.SourceId))
            .ToHashSet();

        var created = 0;
        foreach (var candidate in await CandidatesAsync(cancellationToken))
        {
            var key = (candidate.UserId, candidate.Kind, candidate.SourceId);
            if (string.IsNullOrWhiteSpace(candidate.Title) || seen.Contains(key))
            {
                continue;
            }

            var preference = preferences.GetValueOrDefault(candidate.UserId);
            var channel = preference?.Channel ?? ReminderRules.DefaultChannel;
            var daysBefore = preference?.DaysBefore ?? ReminderRules.DefaultDaysBefore;
            if (channel == NotificationChannel.None || !ReminderRules.IsInsideWindow(candidate.OccursOn, today, daysBefore))
            {
                continue;
            }

            seen.Add(key);

            var notification = new Notification
            {
                Id = Guid.CreateVersion7(),
                UserId = candidate.UserId,
                RelocationId = candidate.RelocationId,
                Kind = candidate.Kind,
                SourceId = candidate.SourceId,
                Title = candidate.Title.Trim(),
                OccursOn = candidate.OccursOn,
                ShowInApp = ReminderRules.ShowsInApp(channel),
            };
            db.Notifications.Add(notification);
            if (ReminderRules.SendsEmail(channel))
            {
                db.NotificationEmails.Add(new NotificationEmail
                {
                    Id = Guid.CreateVersion7(),
                    NotificationId = notification.Id,
                });
            }

            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created {Count} reminders.", created);
        }

        return created;
    }

    public async Task DeliverAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var due = await db.NotificationEmails.AsNoTracking()
            .Where(email => email.Status == NotificationEmailStatus.Pending
                && email.Attempts < ReminderRules.MaxEmailAttempts
                && (email.NextAttemptAt == null || email.NextAttemptAt <= now))
            .OrderBy(email => email.CreatedAt)
            .ThenBy(email => email.Id)
            .Select(email => email.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var id in due)
        {
            await DeliverOneAsync(id, cancellationToken);
        }
    }

    private async Task DeliverOneAsync(Guid id, CancellationToken cancellationToken)
    {
        var email = await db.NotificationEmails
            .Include(candidate => candidate.Notification)
            .ThenInclude(notification => notification.Relocation)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.Status == NotificationEmailStatus.Pending,
                cancellationToken);
        var now = time.GetUtcNow();
        if (email is null
            || email.Attempts >= ReminderRules.MaxEmailAttempts
            || (email.NextAttemptAt is { } next && next > now))
        {
            return;
        }

        var attempt = email.Attempts + 1;
        email.Attempts = attempt;
        email.NextAttemptAt = ReminderRules.NextAttempt(now, attempt);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == email.Notification.UserId, cancellationToken);
        var origin = web.Origins.FirstOrDefault()?.TrimEnd('/');
        var link = origin is null
            ? null
            : $"{origin}/relocations/{email.Notification.RelocationId}";
        var message = new NotificationEmailMessage(
            email.Id,
            user.Email,
            ReminderRules.Subject(email.Notification.Title),
            ReminderRules.Text(
                email.Notification.Relocation.Name,
                email.Notification.Kind,
                email.Notification.Title,
                email.Notification.OccursOn,
                link));

        try
        {
            var sent = await sender.SendAsync(message, cancellationToken);
            if (!sent)
            {
                email.Attempts = attempt - 1;
                email.NextAttemptAt = now.Add(ReminderRules.UnconfiguredDelay);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            email.Status = NotificationEmailStatus.Sent;
            email.SentAt = time.GetUtcNow();
            email.LastError = null;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            email.LastError = ReminderRules.ClipError(exception.Message);
            if (attempt >= ReminderRules.MaxEmailAttempts)
            {
                email.Status = NotificationEmailStatus.Failed;
            }

            logger.LogWarning(exception, "A reminder email could not be sent.");
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<ReminderCandidate>> CandidatesAsync(CancellationToken cancellationToken)
    {
        var tasks = await db.RelocationTasks.AsNoTracking()
            .Where(task => task.DueDate != null)
            .Select(task => new ReminderCandidate(
                task.Relocation.UserId,
                task.RelocationId,
                ReminderKind.TaskDue,
                task.Id,
                task.Title,
                task.DueDate!.Value))
            .ToListAsync(cancellationToken);
        var storedEvents = await db.CalendarEvents.AsNoTracking()
            .Select(calendarEvent => new
            {
                calendarEvent.Relocation.UserId,
                calendarEvent.RelocationId,
                calendarEvent.Kind,
                calendarEvent.Id,
                calendarEvent.Title,
                calendarEvent.OccursOn,
                calendarEvent.StartsAt,
            })
            .ToListAsync(cancellationToken);
        var events = new List<ReminderCandidate>();
        foreach (var calendarEvent in storedEvents)
        {
            var occursOn = calendarEvent.Kind == CalendarEventKind.Milestone
                ? calendarEvent.OccursOn
                : calendarEvent.StartsAt is { } startsAt
                    ? DateOnly.FromDateTime(startsAt.UtcDateTime)
                    : null;
            if (occursOn is null)
            {
                continue;
            }

            events.Add(new ReminderCandidate(
                calendarEvent.UserId,
                calendarEvent.RelocationId,
                calendarEvent.Kind == CalendarEventKind.Milestone ? ReminderKind.Milestone : ReminderKind.Appointment,
                calendarEvent.Id,
                calendarEvent.Title,
                occursOn.Value));
        }
        var documents = await db.Documents.AsNoTracking()
            .Where(document => document.ExpiresOn != null)
            .Select(document => new ReminderCandidate(
                document.Relocation.UserId,
                document.RelocationId,
                ReminderKind.DocumentExpiration,
                document.Id,
                document.FileName,
                document.ExpiresOn!.Value))
            .ToListAsync(cancellationToken);

        return [.. tasks, .. events, .. documents];
    }

    private sealed record ReminderCandidate(
        Guid UserId,
        Guid RelocationId,
        ReminderKind Kind,
        Guid SourceId,
        string Title,
        DateOnly OccursOn);
}
