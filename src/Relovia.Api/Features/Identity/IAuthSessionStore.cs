namespace Relovia.Api.Features.Identity;

public sealed record IssuedSession(string Token, DateTimeOffset AbsoluteExpiresAt);

public interface IAuthSessionStore
{
    Task<IssuedSession> IssueAsync(Guid userId, CancellationToken cancellationToken);

    Task<Guid?> AuthenticateAsync(string token, CancellationToken cancellationToken);

    Task RevokeAsync(string token, CancellationToken cancellationToken);
}
