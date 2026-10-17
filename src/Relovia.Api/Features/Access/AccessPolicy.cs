using Microsoft.Extensions.Options;

namespace Relovia.Api.Features.Access;

/// <summary>
/// Decides who may use the app while it is still being built.
/// Google only reports verified emails here, so the email is a safe key.
/// </summary>
public sealed class AccessPolicy
{
    private readonly HashSet<string> _allowed;

    public AccessPolicy(IOptions<AccessSettings> options)
    {
        var settings = options.Value;
        Restricted = settings.Restricted;
        _allowed = settings.AllowedEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(Normalize)
            .ToHashSet(StringComparer.Ordinal);
        AllowedEmails = _allowed.Order(StringComparer.Ordinal).ToArray();
    }

    public bool Restricted { get; }

    public IReadOnlyList<string> AllowedEmails { get; }

    public bool HasAccess(string email)
    {
        return !Restricted || _allowed.Contains(Normalize(email));
    }

    private static string Normalize(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
