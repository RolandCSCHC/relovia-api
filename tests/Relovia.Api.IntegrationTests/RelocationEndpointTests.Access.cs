using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Relovia.Api.Controllers;
using Relovia.Api.Features.Access;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    private const string AllowedEmail = "owner@relovia.test";

    [Fact]
    public async Task Restricted_app_lets_only_allowed_emails_use_the_api()
    {
        using var restricted = CreateRestrictedFactory();
        var owner = await SignInAsOwnerAsync();
        var stranger = await SignInAsync("stranger-access@example.com");
        using var ownerClient = RestrictedClient(restricted);
        using var strangerClient = RestrictedClient(restricted);

        var ownerRelocations = await SendAsync<List<RelocationResponse>>(
            ownerClient, owner.Token, HttpMethod.Get, "/api/relocations", body: null);
        var strangerRelocations = await SendAsync<List<RelocationResponse>>(
            strangerClient, stranger.Token, HttpMethod.Get, "/api/relocations", body: null);
        var strangerWrite = await SendAsync<RelocationResponse>(
            strangerClient,
            stranger.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Sneaky", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));

        Assert.Equal(HttpStatusCode.OK, ownerRelocations.Status);
        Assert.Equal(HttpStatusCode.Forbidden, strangerRelocations.Status);
        Assert.Equal(HttpStatusCode.Forbidden, strangerWrite.Status);
    }

    [Fact]
    public async Task Restricted_app_still_tells_a_stranger_who_they_are_and_that_they_have_no_access()
    {
        using var restricted = CreateRestrictedFactory();
        var stranger = await SignInAsync("stranger-me@example.com");
        using var strangerClient = RestrictedClient(restricted);
        using var anonymousClient = RestrictedClient(restricted);

        var me = await SendAsync<CurrentUserResponse>(
            strangerClient, stranger.Token, HttpMethod.Get, "/api/auth/me", body: null);
        var anonymous = await anonymousClient.Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.Status);
        Assert.Equal("stranger-me@example.com", me.Body!.Email);
        Assert.False(me.Body.HasAccess);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Open_app_lets_everyone_in()
    {
        var anyone = await SignInAsync("anyone-open@example.com");
        using var client = CreateClient();

        var relocations = await SendAsync<List<RelocationResponse>>(
            client, anyone.Token, HttpMethod.Get, "/api/relocations", body: null);
        var me = await SendAsync<CurrentUserResponse>(
            client, anyone.Token, HttpMethod.Get, "/api/auth/me", body: null);

        Assert.Equal(HttpStatusCode.OK, relocations.Status);
        Assert.True(me.Body!.HasAccess);
    }

    [Fact]
    public async Task A_stranger_who_signs_in_is_reported_once_and_the_owner_is_not()
    {
        using var restricted = CreateRestrictedFactory();
        var stranger = await SignInAsync("stranger-interest@example.com");
        await using var scope = restricted.Services.CreateAsyncScope();
        var notifier = restricted.Services.GetRequiredService<RecordingInterestNotifier>();
        var before = notifier.People.Count;
        var interest = scope.ServiceProvider.GetRequiredService<InterestService>();

        await interest.RecordSignInAsync(stranger.UserId, CancellationToken.None);
        await interest.RecordSignInAsync(stranger.UserId, CancellationToken.None);

        var reported = notifier.People.Skip(before).ToList();
        Assert.Single(reported);
        Assert.Equal("stranger-interest@example.com", reported[0].Email);
        await using var verify = _postgres.CreateContext();
        var saved = await verify.Users.AsNoTracking().SingleAsync(user => user.Id == stranger.UserId);
        Assert.NotNull(saved.InterestNotifiedAt);
    }

    [Fact]
    public async Task An_allowed_email_is_never_reported_as_interested()
    {
        using var restricted = CreateRestrictedFactory();
        var owner = await SignInAsOwnerAsync();
        await using var scope = restricted.Services.CreateAsyncScope();
        var notifier = restricted.Services.GetRequiredService<RecordingInterestNotifier>();
        var before = notifier.People.Count;

        await scope.ServiceProvider.GetRequiredService<InterestService>()
            .RecordSignInAsync(owner.UserId, CancellationToken.None);

        Assert.Equal(before, notifier.People.Count);
    }

    [Fact]
    public async Task A_failed_email_is_retried_on_the_next_sign_in()
    {
        using var restricted = CreateRestrictedFactory();
        var stranger = await SignInAsync("stranger-retry@example.com");
        var notifier = restricted.Services.GetRequiredService<RecordingInterestNotifier>();
        notifier.Deliver = false;
        await using var scope = restricted.Services.CreateAsyncScope();
        var interest = scope.ServiceProvider.GetRequiredService<InterestService>();

        await interest.RecordSignInAsync(stranger.UserId, CancellationToken.None);
        await using var first = _postgres.CreateContext();
        var afterFailure = await first.Users.AsNoTracking().SingleAsync(user => user.Id == stranger.UserId);

        notifier.Deliver = true;
        await using var retryScope = restricted.Services.CreateAsyncScope();
        await retryScope.ServiceProvider.GetRequiredService<InterestService>()
            .RecordSignInAsync(stranger.UserId, CancellationToken.None);
        await using var second = _postgres.CreateContext();
        var afterRetry = await second.Users.AsNoTracking().SingleAsync(user => user.Id == stranger.UserId);

        Assert.Null(afterFailure.InterestNotifiedAt);
        Assert.NotNull(afterRetry.InterestNotifiedAt);
    }

    // The allowed email is unique in the shared database, so tests reuse one owner row.
    private async Task<(Guid UserId, string Token)> SignInAsOwnerAsync()
    {
        await using var db = _postgres.CreateContext();
        var existing = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == AllowedEmail);
        if (existing is null)
        {
            return await SignInAsync(AllowedEmail);
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IAuthSessionStore>();
        var issued = await sessions.IssueAsync(existing.Id, CancellationToken.None);
        return (existing.Id, issued.Token);
    }

    private ReloviaApiFactory CreateRestrictedFactory()
    {
        return new ReloviaApiFactory(
            _postgres.ConnectionString,
            Path.Combine(_citiesDirectory!, "cities.txt"),
            Path.Combine(_citiesDirectory!, "admin1.txt"),
            restricted: true);
    }

    private static ClientScope RestrictedClient(ReloviaApiFactory factory)
    {
        var cookies = new CookieJar();
        return new ClientScope(factory.CreateDefaultClient(cookies), cookies);
    }

    public sealed class RecordingInterestNotifier : IInterestNotifier
    {
        public List<InterestedPerson> People { get; } = [];

        public bool Deliver { get; set; } = true;

        public Task<bool> NotifyAsync(InterestedPerson person, CancellationToken cancellationToken)
        {
            if (Deliver)
            {
                People.Add(person);
            }

            return Task.FromResult(Deliver);
        }
    }
}
