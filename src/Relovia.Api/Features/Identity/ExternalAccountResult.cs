namespace Relovia.Api.Features.Identity;

public sealed record ExternalAccountResult(bool Succeeded, Guid UserId, string? ErrorCode)
{
    public static ExternalAccountResult SignedIn(Guid userId) => new(true, userId, null);

    public static ExternalAccountResult Rejected(string errorCode) => new(false, Guid.Empty, errorCode);
}
