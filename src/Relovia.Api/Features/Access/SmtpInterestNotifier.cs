using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Relovia.Api.Features.Access;

public sealed class SmtpInterestNotifier(
    IOptions<EmailSettings> options,
    AccessPolicy access,
    ILogger<SmtpInterestNotifier> logger) : IInterestNotifier
{
    public async Task<bool> NotifyAsync(InterestedPerson person, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.Host is not { Length: > 0 } host || string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("Email is not configured. {Email} is interested in Relovia, but nobody was told.", person.Email);
            return false;
        }

        if (access.AllowedEmails.Count == 0)
        {
            logger.LogWarning("Access:AllowedEmails is empty, so there is nobody to tell about {Email}.", person.Email);
            return false;
        }

        var sender = settings.From ?? settings.Username
            ?? throw new InvalidOperationException("Email:From or Email:Username must be set.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(sender));
        foreach (var owner in access.AllowedEmails)
        {
            message.To.Add(MailboxAddress.Parse(owner));
        }

        message.Subject = $"Relovia: {person.Name} is interested";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                Someone signed in to Relovia and is interested in using it.

                Name:  {person.Name}
                Email: {person.Email}
                When:  {person.SignedInAt:u}

                They can't use the app until you add their email to Access:AllowedEmails
                (or set Access:Restricted to false to open it to everyone).
                """,
        };
        if (MailboxAddress.TryParse(person.Email, out var replyTo))
        {
            message.ReplyTo.Add(replyTo);
        }

        using var client = new SmtpClient { Timeout = 15_000 };
        await client.ConnectAsync(host, settings.Port, SecureSocketOptions.Auto, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
        return true;
    }
}
