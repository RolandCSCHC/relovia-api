using Microsoft.Extensions.Options;
using Relovia.Api.Features.Access;

namespace Relovia.Api.UnitTests;

public sealed class AccessPolicyTests
{
    [Fact]
    public void Restricted_policy_allows_only_listed_emails_ignoring_case_and_spaces()
    {
        var policy = Create(restricted: true, " Owner@Example.com ");

        Assert.True(policy.HasAccess("owner@example.com"));
        Assert.True(policy.HasAccess("  OWNER@EXAMPLE.COM"));
        Assert.False(policy.HasAccess("someone@example.com"));
    }

    [Fact]
    public void Restricted_policy_with_no_allowed_emails_lets_nobody_in()
    {
        var policy = Create(restricted: true);

        Assert.False(policy.HasAccess("owner@example.com"));
    }

    [Fact]
    public void Unrestricted_policy_lets_everyone_in()
    {
        var policy = Create(restricted: false, "owner@example.com");

        Assert.True(policy.HasAccess("someone@example.com"));
    }

    [Fact]
    public void Blank_entries_are_ignored_and_duplicates_collapse()
    {
        var policy = Create(restricted: true, "", "  ", "a@example.com", "A@example.com");

        Assert.Equal(["a@example.com"], policy.AllowedEmails);
    }

    private static AccessPolicy Create(bool restricted, params string[] allowed)
    {
        return new AccessPolicy(Options.Create(new AccessSettings
        {
            Restricted = restricted,
            AllowedEmails = allowed,
        }));
    }
}
