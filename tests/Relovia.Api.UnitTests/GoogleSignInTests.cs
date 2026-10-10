using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.UnitTests;

public sealed class GoogleSignInTests
{
    [Fact]
    public void Verified_google_profile_uses_the_subject_and_normalized_email()
    {
        var read = GoogleIdentityReader.Read(Principal(
            subject: "google-subject-1",
            email: "  Person@Example.com ",
            emailVerified: "true",
            name: "Person Example",
            picture: "https://example.com/avatar.png"));

        Assert.True(read.Succeeded);
        Assert.Equal("google-subject-1", read.Identity!.Subject);
        Assert.Equal("person@example.com", read.Identity.Email);
        Assert.Equal("Person Example", read.Identity.DisplayName);
        Assert.Equal("https://example.com/avatar.png", read.Identity.AvatarUrl);
    }

    [Fact]
    public void Name_identifier_without_the_subject_claim_is_rejected()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "legacy-id"),
                new Claim(ClaimTypes.Email, "person@example.com"),
                new Claim(GoogleIdentityReader.EmailVerifiedClaim, "true"),
            ],
            "Google");

        var read = GoogleIdentityReader.Read(new ClaimsPrincipal(identity));

        Assert.False(read.Succeeded);
        Assert.Equal(SignInErrors.InvalidProfile, read.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("False")]
    public void Unverified_email_is_rejected(string? emailVerified)
    {
        var read = GoogleIdentityReader.Read(Principal("subject", "person@example.com", emailVerified, "Person", null));

        Assert.False(read.Succeeded);
        Assert.Equal(SignInErrors.UnverifiedEmail, read.ErrorCode);
    }

    [Fact]
    public void Unsafe_or_oversized_avatar_is_dropped()
    {
        var script = GoogleIdentityReader.Read(Principal(
            "subject",
            "person@example.com",
            "true",
            "Person",
            "javascript:alert(1)"));
        var oversized = GoogleIdentityReader.Read(Principal(
            "subject",
            "person@example.com",
            "true",
            "Person",
            "https://example.com/" + new string('a', GoogleIdentityReader.MaximumAvatarLength)));

        Assert.Null(script.Identity!.AvatarUrl);
        Assert.Null(oversized.Identity!.AvatarUrl);
    }

    [Fact]
    public void Long_display_name_is_truncated()
    {
        var read = GoogleIdentityReader.Read(Principal(
            "subject",
            "person@example.com",
            "true",
            new string('n', 250),
            null));

        Assert.Equal(GoogleIdentityReader.MaximumDisplayNameLength, read.Identity!.DisplayName.Length);
    }

    [Fact]
    public void Known_subject_signs_in_again()
    {
        var decision = ExternalAccountLink.Decide(subjectAlreadyLinked: true, emailOwnedByAnotherUser: false);

        Assert.Equal(ExternalAccountDecision.Existing, decision);
    }

    [Fact]
    public void Different_subject_with_the_same_email_is_not_linked()
    {
        var decision = ExternalAccountLink.Decide(subjectAlreadyLinked: false, emailOwnedByAnotherUser: true);

        Assert.Equal(ExternalAccountDecision.RejectEmailInUse, decision);
    }

    [Fact]
    public void Known_subject_is_not_treated_as_an_email_collision()
    {
        var decision = ExternalAccountLink.Decide(subjectAlreadyLinked: true, emailOwnedByAnotherUser: true);

        Assert.Equal(ExternalAccountDecision.Existing, decision);
    }

    [Fact]
    public void New_subject_and_email_create_an_account()
    {
        var decision = ExternalAccountLink.Decide(subjectAlreadyLinked: false, emailOwnedByAnotherUser: false);

        Assert.Equal(ExternalAccountDecision.Create, decision);
    }

    [Fact]
    public void Provider_tokens_are_removed_before_the_session_is_issued()
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "access-secret" },
            new AuthenticationToken { Name = "id_token", Value = "id-secret" },
            new AuthenticationToken { Name = "refresh_token", Value = "refresh-secret" },
        ]);

        GoogleSignIn.DiscardProviderTokens(properties);

        Assert.Empty(properties.GetTokens());
    }

    [Fact]
    public void Failure_redirect_stays_on_the_app_origin()
    {
        Assert.Equal(
            "http://localhost:4200/sign-in?error=email-in-use",
            SignInRedirect.Failure("http://localhost:4200/", SignInErrors.EmailInUse));
        Assert.Equal("http://localhost:4200/", SignInRedirect.Success("http://localhost:4200"));
    }

    private static ClaimsPrincipal Principal(
        string subject,
        string email,
        string? emailVerified,
        string? name,
        string? picture)
    {
        var claims = new List<Claim>
        {
            new(GoogleIdentityReader.SubjectClaim, subject),
            new(ClaimTypes.Email, email),
        };
        if (emailVerified is not null)
        {
            claims.Add(new Claim(GoogleIdentityReader.EmailVerifiedClaim, emailVerified));
        }

        if (name is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        if (picture is not null)
        {
            claims.Add(new Claim(GoogleIdentityReader.PictureClaim, picture));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Google"));
    }
}
