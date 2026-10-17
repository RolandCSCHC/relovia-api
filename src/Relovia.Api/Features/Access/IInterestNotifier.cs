namespace Relovia.Api.Features.Access;

public sealed record InterestedPerson(string Name, string Email, DateTimeOffset SignedInAt);

public interface IInterestNotifier
{
    /// <summary>
    /// Tells the developer that someone is interested. Returns false when nothing could be sent
    /// because email is not configured, so the caller tries again on a later sign-in.
    /// </summary>
    Task<bool> NotifyAsync(InterestedPerson person, CancellationToken cancellationToken);
}
