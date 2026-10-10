using System.Security.Claims;

namespace Relovia.Api.Features.Identity;

public sealed record GoogleIdentityRead(bool Succeeded, GoogleIdentity? Identity, string ErrorCode)
{
    public static GoogleIdentityRead Success(GoogleIdentity identity) => new(true, identity, string.Empty);

    public static GoogleIdentityRead Fail(string errorCode) => new(false, null, errorCode);
}

public static class GoogleIdentityReader
{
    public const string SubjectClaim = "google:sub";

    public const string EmailVerifiedClaim = "email_verified";

    public const string PictureClaim = "picture";

    public const int MaximumSubjectLength = 255;

    public const int MaximumEmailLength = 320;

    public const int MaximumDisplayNameLength = 200;

    public const int MaximumAvatarLength = 2048;

    public static GoogleIdentityRead Read(ClaimsPrincipal? principal)
    {
        var subject = principal?.FindFirst(SubjectClaim)?.Value?.Trim();
        if (string.IsNullOrEmpty(subject) || subject.Length > MaximumSubjectLength)
        {
            return GoogleIdentityRead.Fail(SignInErrors.InvalidProfile);
        }

        var verified = principal?.FindFirst(EmailVerifiedClaim)?.Value;
        if (!string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase))
        {
            return GoogleIdentityRead.Fail(SignInErrors.UnverifiedEmail);
        }

        var email = principal?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || email.Length > MaximumEmailLength || !email.Contains('@'))
        {
            return GoogleIdentityRead.Fail(SignInErrors.InvalidProfile);
        }

        var displayName = principal?.FindFirst(ClaimTypes.Name)?.Value?.Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = email;
        }

        if (displayName.Length > MaximumDisplayNameLength)
        {
            displayName = displayName[..MaximumDisplayNameLength];
        }

        return GoogleIdentityRead.Success(new GoogleIdentity(subject, email, displayName, NormalizeAvatar(principal)));
    }

    private static string? NormalizeAvatar(ClaimsPrincipal? principal)
    {
        var avatar = principal?.FindFirst(PictureClaim)?.Value?.Trim();
        if (string.IsNullOrEmpty(avatar) || avatar.Length > MaximumAvatarLength)
        {
            return null;
        }

        if (!Uri.TryCreate(avatar, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return null;
        }

        return avatar;
    }
}
