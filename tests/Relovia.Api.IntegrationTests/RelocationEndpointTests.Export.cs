using System.Net;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task The_owner_downloads_a_pdf_and_anyone_else_does_not()
    {
        var owner = await SignInAsync("export-owner@example.com");
        var other = await SignInAsync("export-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();

        var created = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Later", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var path = $"/api/relocations/{created.Body!.Id}/export";

        using var anonymous = CreateClient();
        var unsigned = await anonymous.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, unsigned.StatusCode);

        otherClient.Cookies.Add(SessionAuthenticationDefaults.CookieName, other.Token);
        var hidden = await otherClient.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        ownerClient.Cookies.Add(SessionAuthenticationDefaults.CookieName, owner.Token);
        var pdf = await ownerClient.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", pdf.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("no-store", pdf.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        Assert.Contains("Later.pdf", pdf.Content.Headers.ContentDisposition?.ToString(), StringComparison.Ordinal);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);

        var summaryPath = path + "/summary";
        var hiddenSummary = await otherClient.Client.GetAsync(summaryPath);
        Assert.Equal(HttpStatusCode.NotFound, hiddenSummary.StatusCode);
        var summary = await ownerClient.Client.GetAsync(summaryPath);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.Equal("application/pdf", summary.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Later summary.pdf", summary.Content.Headers.ContentDisposition?.ToString(), StringComparison.Ordinal);
        var summaryBytes = await summary.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), summaryBytes[..4]);
    }
}
