namespace Relovia.Api.Features.Access;

/// <summary>SMTP account used to email the developer. Leave <see cref="Host"/> empty to disable email.</summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string? Host { get; set; }

    /// <summary>587 negotiates STARTTLS and 465 uses implicit TLS.</summary>
    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>Sender address. Defaults to <see cref="Username"/>.</summary>
    public string? From { get; set; }
}
