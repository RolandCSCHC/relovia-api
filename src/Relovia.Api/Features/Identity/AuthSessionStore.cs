using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Identity;

public sealed class AuthSessionStore(
    ReloviaDbContext db,
    TimeProvider time,
    IOptions<SessionSettings> settings) : IAuthSessionStore
{
    public async Task<IssuedSession> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var lifetime = settings.Value;
        var absoluteExpiresAt = now + lifetime.AbsoluteLifetime;
        var expiresAt = now + lifetime.SlidingLifetime;
        if (expiresAt > absoluteExpiresAt)
        {
            expiresAt = absoluteExpiresAt;
        }

        var token = SessionToken.Create();
        db.AuthSessions.Add(new AuthSession
        {
            UserId = userId,
            TokenHash = SessionToken.TryHash(token)
                ?? throw new InvalidOperationException("A new session token could not be hashed."),
            ExpiresAt = expiresAt,
            AbsoluteExpiresAt = absoluteExpiresAt,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new IssuedSession(token, absoluteExpiresAt);
    }

    public async Task<Guid?> AuthenticateAsync(string token, CancellationToken cancellationToken)
    {
        var session = await FindAsync(token, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var decision = SessionLifetime.Evaluate(
            time.GetUtcNow(),
            session.ExpiresAt,
            session.AbsoluteExpiresAt,
            session.RevokedAt,
            settings.Value.SlidingLifetime);
        if (!decision.IsActive)
        {
            return null;
        }

        if (decision.ExpiresAt != session.ExpiresAt)
        {
            session.ExpiresAt = decision.ExpiresAt;
            await db.SaveChangesAsync(cancellationToken);
        }

        return session.UserId;
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var session = await FindAsync(token, cancellationToken);
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        session.RevokedAt = time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthSession?> FindAsync(string token, CancellationToken cancellationToken)
    {
        var hash = SessionToken.TryHash(token);
        if (hash is null)
        {
            return null;
        }

        return await db.AuthSessions.SingleOrDefaultAsync(
            session => session.TokenHash == hash,
            cancellationToken);
    }
}
