using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Notifications;

/// <summary>
/// An email that still has to be sent for one reminder. The reminder id is unique, so a retry
/// cannot create a second message. <see cref="NotificationEmailStatus.Sent"/> is not sent again.
/// </summary>
public sealed class NotificationEmail : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid NotificationId { get; set; }

    public NotificationEmailStatus Status { get; set; } = NotificationEmailStatus.Pending;

    public int Attempts { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Notification Notification { get; set; } = null!;
}
