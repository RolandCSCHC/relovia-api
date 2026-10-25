namespace Relovia.Api.Features.Notifications;

public sealed record NotificationEmailMessage(
    Guid IdempotencyKey,
    string ToEmail,
    string Subject,
    string Text);

public interface INotificationEmailSender
{
    /// <summary>
    /// Sends one reminder. Returns false when email is not configured, so the caller tries again later.
    /// Throws when the provider rejects the message.
    /// </summary>
    Task<bool> SendAsync(NotificationEmailMessage message, CancellationToken cancellationToken);
}
