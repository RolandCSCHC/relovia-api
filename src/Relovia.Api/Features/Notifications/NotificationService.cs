using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Notifications;

public sealed class NotificationService(ReloviaDbContext db, TimeProvider time)
{
    public const string NotFoundTitle = "Reminder was not found.";

    public async Task<IReadOnlyList<NotificationResponse>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId && notification.ShowInApp)
            .OrderBy(notification => notification.ReadAt != null)
            .ThenBy(notification => notification.OccursOn)
            .ThenBy(notification => notification.Id)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.RelocationId,
                notification.Relocation.Name,
                notification.Kind,
                notification.Title,
                notification.OccursOn,
                notification.CreatedAt,
                notification.ReadAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationResult<NotificationResponse>> MarkReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await db.Notifications
            .Include(candidate => candidate.Relocation)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == notificationId && candidate.UserId == userId && candidate.ShowInApp,
                cancellationToken);
        if (notification is null)
        {
            return new NotificationResult<NotificationResponse>.NotFound(NotFoundTitle);
        }

        notification.ReadAt ??= time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return new NotificationResult<NotificationResponse>.Success(ToResponse(notification));
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var unread = await db.Notifications
            .Where(notification => notification.UserId == userId && notification.ShowInApp && notification.ReadAt == null)
            .ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.ReadAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationPreferenceResponse> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await db.NotificationPreferences.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        return preference is null
            ? NotificationPreferenceResponse.Default
            : NotificationPreferenceResponse.From(preference);
    }

    public async Task<NotificationResult<NotificationPreferenceResponse>> UpdatePreferenceAsync(
        Guid userId,
        SaveNotificationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ReminderRules.Validate(request);
        if (errors.Count > 0)
        {
            return new NotificationResult<NotificationPreferenceResponse>.Invalid(errors);
        }

        var preference = await db.NotificationPreferences
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        if (preference is null)
        {
            preference = new NotificationPreference { UserId = userId };
            db.NotificationPreferences.Add(preference);
        }

        preference.Channel = request.Channel;
        preference.DaysBefore = request.DaysBefore;
        await db.SaveChangesAsync(cancellationToken);
        return new NotificationResult<NotificationPreferenceResponse>.Success(
            NotificationPreferenceResponse.From(preference));
    }

    private static NotificationResponse ToResponse(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.RelocationId,
            notification.Relocation.Name,
            notification.Kind,
            notification.Title,
            notification.OccursOn,
            notification.CreatedAt,
            notification.ReadAt);
    }
}
