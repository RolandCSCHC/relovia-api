namespace Relovia.Api.Features.Access;

/// <summary>
/// Resend account used to email the developer. Leave <see cref="ApiKey"/> empty to disable email.
/// Mail goes over HTTPS because some hosts, including Render's free tier, block outbound SMTP.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string? ApiKey { get; set; }

    /// <summary>
    /// Sender. Resend's test sender can only deliver to the address that owns the Resend account.
    /// Use an address on a verified domain to send to anyone else.
    /// </summary>
    public string From { get; set; } = "Relovia <onboarding@resend.dev>";
}
