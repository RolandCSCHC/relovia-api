using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Relovia.Api.Features.Access;

public sealed class ResendInterestNotifier(
    HttpClient http,
    IOptions<EmailSettings> options,
    AccessPolicy access,
    ILogger<ResendInterestNotifier> logger) : IInterestNotifier
{
    public async Task<bool> NotifyAsync(InterestedPerson person, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("Email is not configured. {Email} is interested in Relovia, but nobody was told.", person.Email);
            return false;
        }

        if (access.AllowedEmails.Count == 0)
        {
            logger.LogWarning("Access:AllowedEmails is empty, so there is nobody to tell about {Email}.", person.Email);
            return false;
        }

        var body = new
        {
            from = settings.From,
            to = access.AllowedEmails,
            reply_to = person.Email,
            subject = $"Relovia: {person.Name} is interested",
            text = $"""
                Someone signed in to Relovia and is interested in using it.

                Name:  {person.Name}
                Email: {person.Email}
                When:  {person.SignedInAt:u}

                They can't use the app until you add their email to Access:AllowedEmails
                (or set Access:Restricted to false to open it to everyone).
                """,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

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
