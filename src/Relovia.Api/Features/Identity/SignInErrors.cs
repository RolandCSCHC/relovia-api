namespace Relovia.Api.Features.Identity;

public static class SignInErrors
{
    public const string EmailInUse = "email-in-use";

    public const string UnverifiedEmail = "unverified-email";

    public const string InvalidProfile = "invalid-profile";

    public const string GoogleDenied = "google-denied";

    public const string GoogleFailed = "google-sign-in-failed";
}
