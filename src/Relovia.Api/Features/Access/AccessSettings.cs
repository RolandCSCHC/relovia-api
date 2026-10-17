namespace Relovia.Api.Features.Access;

public sealed class AccessSettings
{
    public const string SectionName = "Access";

    /// <summary>
    /// While true, only <see cref="AllowedEmails"/> can use the API. Everyone else can sign in
    /// to say they are interested, and nothing more. Set to false to open the app to everyone.
    /// </summary>
    public bool Restricted { get; set; } = true;

    /// <summary>
    /// Google accounts that can use the app while it is restricted. They also receive the
    /// "someone is interested" email.
    /// </summary>
    public string[] AllowedEmails { get; set; } = [];
}
