using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Identity;

public sealed class ExternalAccountService(ReloviaDbContext db)
{
    public async Task<ExternalAccountResult> LinkGoogleAsync(GoogleIdentity identity, CancellationToken cancellationToken)
    {
        var subject = identity.Subject.Trim();
        var email = identity.Email.Trim().ToLowerInvariant();
        var existing = await db.ExternalLogins.AsNoTracking().SingleOrDefaultAsync(
            login => login.Provider == ExternalProviders.Google && login.ProviderSubject == subject,
            cancellationToken);
        var emailOwner = await db.Users.AsNoTracking().SingleOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);
        var emailOwnedByAnotherUser = emailOwner is not null && (existing is null || emailOwner.Id != existing.UserId);
        var decision = ExternalAccountLink.Decide(existing is not null, emailOwnedByAnotherUser);

        if (decision == ExternalAccountDecision.Existing)
        {
            return ExternalAccountResult.SignedIn(existing!.UserId);
        }

        if (decision == ExternalAccountDecision.RejectEmailInUse)
        {
            return ExternalAccountResult.Rejected(SignInErrors.EmailInUse);
        }

        var user = new User
        {
            Email = email,
            DisplayName = identity.DisplayName,
            AvatarUrl = identity.AvatarUrl,
        };
        db.Users.Add(user);
        db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = ExternalProviders.Google,
            ProviderSubject = subject,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            db.ChangeTracker.Clear();
            var linked = await db.ExternalLogins.AsNoTracking().SingleOrDefaultAsync(
                login => login.Provider == ExternalProviders.Google && login.ProviderSubject == subject,
                cancellationToken);
            if (linked is not null)
            {
                return ExternalAccountResult.SignedIn(linked.UserId);
            }

            return ExternalAccountResult.Rejected(SignInErrors.EmailInUse);
        }

        return ExternalAccountResult.SignedIn(user.Id);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
