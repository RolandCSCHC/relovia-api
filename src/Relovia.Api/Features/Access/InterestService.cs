using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Access;

/// <summary>Emails the developer once when someone without access signs in.</summary>
public sealed class InterestService(
    ReloviaDbContext db,
    AccessPolicy access,
    IInterestNotifier notifier,
    TimeProvider time,
    ILogger<InterestService> logger)
{
    public async Task RecordSignInAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
            if (user is null || user.InterestNotifiedAt is not null || access.HasAccess(user.Email))
            {
                return;
            }

            var now = time.GetUtcNow();
            var delivered = await notifier.NotifyAsync(
                new InterestedPerson(user.DisplayName, user.Email, now),
                cancellationToken);
            if (!delivered)
            {
                return;
            }

            user.InterestNotifiedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A broken mail server must never stop someone from signing in.
            // InterestNotifiedAt stays empty, so the next sign-in tries again.
            logger.LogError(exception, "Could not tell the developer that user {UserId} is interested.", userId);
        }
    }
}
