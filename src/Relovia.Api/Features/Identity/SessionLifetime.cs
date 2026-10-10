namespace Relovia.Api.Features.Identity;

public readonly record struct SessionDecision(bool IsActive, DateTimeOffset ExpiresAt)
{
    public static SessionDecision Inactive => new(false, default);
}

public static class SessionLifetime
{
    public static SessionDecision Evaluate(
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        DateTimeOffset absoluteExpiresAt,
        DateTimeOffset? revokedAt,
        TimeSpan slidingLifetime)
    {
        if (revokedAt is not null || now >= expiresAt || now >= absoluteExpiresAt)
        {
            return SessionDecision.Inactive;
        }

        var remaining = expiresAt - now;
        var half = new TimeSpan(slidingLifetime.Ticks / 2);
        if (remaining > half)
        {
            return new SessionDecision(true, expiresAt);
        }

        var refreshed = now + slidingLifetime;
        if (refreshed > absoluteExpiresAt)
        {
            refreshed = absoluteExpiresAt;
        }

        if (refreshed <= expiresAt)
        {
            return new SessionDecision(true, expiresAt);
        }

        return new SessionDecision(true, refreshed);
    }
}
