using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Relovia.Api.Features.Access;

namespace Relovia.Api.Features.Notifications;

public sealed class ResendNotificationEmailSender(
    HttpClient http,
    IOptions<EmailSettings> options,
    ILogger<ResendNotificationEmailSender> logger) : INotificationEmailSender
{
    public async Task<bool> SendAsync(NotificationEmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("Email is not configured, so a reminder was left unsent.");
            return false;
        }

        var body = new
        {
            from = settings.From,
            to = new[] { message.ToEmail },
            subject = message.Subject,
            text = message.Text,
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey.ToString());

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Resend rejected the email with {(int)response.StatusCode}: {detail}",
                inner: null,
                response.StatusCode);
        }

        return true;
    }
}
