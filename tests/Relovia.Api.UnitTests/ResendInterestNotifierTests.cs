using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relovia.Api.Features.Access;

namespace Relovia.Api.UnitTests;

public sealed class ResendInterestNotifierTests
{
    private static readonly InterestedPerson Person = new(
        "Ana Example",
        "ana@example.com",
        new DateTimeOffset(2026, 10, 17, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Sends_one_email_to_every_allowed_address_with_the_api_key()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        var notifier = Create(handler, apiKey: "re_key", "b@example.com", "a@example.com");

        var delivered = await notifier.NotifyAsync(Person, CancellationToken.None);

        Assert.True(delivered);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.resend.com/emails", request.Uri);
        Assert.Equal("Bearer re_key", request.Authorization);
        using var body = JsonDocument.Parse(request.Body);
        var root = body.RootElement;
        Assert.Equal(["a@example.com", "b@example.com"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("ana@example.com", root.GetProperty("reply_to").GetString());
        Assert.Contains("Ana Example", root.GetProperty("subject").GetString());
        Assert.Contains("ana@example.com", root.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Does_nothing_and_reports_not_delivered_without_an_api_key()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        var notifier = Create(handler, apiKey: null, "a@example.com");

        var delivered = await notifier.NotifyAsync(Person, CancellationToken.None);

        Assert.False(delivered);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Does_nothing_when_nobody_is_allowed()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        var notifier = Create(handler, apiKey: "re_key");

        var delivered = await notifier.NotifyAsync(Person, CancellationToken.None);

        Assert.False(delivered);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Throws_with_the_provider_message_when_Resend_rejects_the_email()
    {
        var handler = new StubHandler(HttpStatusCode.Forbidden, "{\"message\":\"domain not verified\"}");
        var notifier = Create(handler, apiKey: "re_key", "a@example.com");

        var failure = await Assert.ThrowsAsync<HttpRequestException>(
            () => notifier.NotifyAsync(Person, CancellationToken.None));

        Assert.Contains("403", failure.Message);
        Assert.Contains("domain not verified", failure.Message);
    }

    private static ResendInterestNotifier Create(StubHandler handler, string? apiKey, params string[] allowed)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var access = new AccessPolicy(Options.Create(new AccessSettings { AllowedEmails = allowed }));
        var email = Options.Create(new EmailSettings { ApiKey = apiKey });
        return new ResendInterestNotifier(http, email, access, NullLogger<ResendInterestNotifier>.Instance);
    }

    private sealed record Captured(string Uri, string? Authorization, string Body);

    private sealed class StubHandler(HttpStatusCode status, string content = "{}") : HttpMessageHandler
    {
        public List<Captured> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new Captured(request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));
            return new HttpResponseMessage(status) { Content = new StringContent(content) };
        }
    }
}
