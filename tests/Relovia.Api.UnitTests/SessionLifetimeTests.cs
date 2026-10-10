using Relovia.Api.Features.Identity;

namespace Relovia.Api.UnitTests;

public sealed class SessionLifetimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Sliding = TimeSpan.FromDays(14);
    private static readonly DateTimeOffset Absolute = Now.AddDays(30);

    [Fact]
    public void Fresh_session_stays_active_without_a_new_expiry()
    {
        var expiresAt = Now.AddDays(14);

        var decision = SessionLifetime.Evaluate(Now, expiresAt, Absolute, revokedAt: null, Sliding);

        Assert.True(decision.IsActive);
        Assert.Equal(expiresAt, decision.ExpiresAt);
    }

    [Fact]
    public void Session_past_half_its_window_slides_forward()
    {
        var later = Now.AddDays(8);
        var expiresAt = Now.AddDays(14);

        var decision = SessionLifetime.Evaluate(later, expiresAt, Absolute, revokedAt: null, Sliding);

        Assert.True(decision.IsActive);
        Assert.Equal(later.AddDays(14), decision.ExpiresAt);
    }

    [Fact]
    public void Sliding_stops_at_the_absolute_expiry()
    {
        var near = Absolute.AddDays(-1);
        var expiresAt = near.AddHours(1);

        var decision = SessionLifetime.Evaluate(near, expiresAt, Absolute, revokedAt: null, Sliding);

        Assert.True(decision.IsActive);
        Assert.Equal(Absolute, decision.ExpiresAt);
    }

    [Fact]
    public void Expired_session_is_inactive()
    {
        var decision = SessionLifetime.Evaluate(Now, Now, Absolute, revokedAt: null, Sliding);

        Assert.False(decision.IsActive);
    }

    [Fact]
    public void Absolute_expiry_ends_the_session_even_when_the_sliding_expiry_is_later()
    {
        var decision = SessionLifetime.Evaluate(
            Absolute,
            Absolute.AddHours(1),
            Absolute,
            revokedAt: null,
            Sliding);

        Assert.False(decision.IsActive);
    }

    [Fact]
    public void Revoked_session_is_inactive()
    {
        var decision = SessionLifetime.Evaluate(
            Now,
            Now.AddDays(14),
            Absolute,
            revokedAt: Now.AddMinutes(-1),
            Sliding);

        Assert.False(decision.IsActive);
    }
}
