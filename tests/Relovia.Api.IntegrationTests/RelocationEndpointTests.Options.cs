using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Applications;
using Relovia.Api.Features.Housing;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task Housing_and_applications_belong_to_an_international_move()
    {
        var owner = await SignInAsync("options-owner@example.com");
        var other = await SignInAsync("options-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocation = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Berlin", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        var relocationId = relocation.Body!.Id;
        var housingPath = $"/api/relocations/{relocationId}/housing";
        var applicationsPath = $"/api/relocations/{relocationId}/applications";

        var anonymous = await CreateClient().Client.GetAsync(housingPath);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var hidden = await SendAsync<JsonElement>(otherClient, other.Token, HttpMethod.Get, housingPath, body: null);
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);
        var hiddenApplications = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            applicationsPath,
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, hiddenApplications.Status);

        var first = await SendAsync<HousingOptionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new
            {
                name = " Mitte room ",
                area = "  ",
                rent = (decimal?)null,
                deposit = (decimal?)null,
                currencyCode = (string?)null,
                size = (string?)null,
                furnishing = (string?)null,
                availableOn = (DateOnly?)null,
                status = "Considering",
                notes = "  ",
            });
        Assert.Equal(HttpStatusCode.Created, first.Status);
        Assert.Equal($"/api/relocations/{relocationId}/housing/{first.Body!.Id}", first.Response.Headers.Location?.OriginalString);
        Assert.Equal("Mitte room", first.Body.Name);
        Assert.Null(first.Body.Area);
        Assert.Null(first.Body.Notes);
        Assert.Equal(HousingStatus.Considering, first.Body.Status);

        var chosen = await SendAsync<HousingOptionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new
            {
                name = " Kreuzberg flat ",
                area = " Kreuzberg ",
                rent = 1200m,
                deposit = 2400m,
                currencyCode = "eur",
                size = " 2 bedrooms ",
                furnishing = "Furnished",
                availableOn = new DateOnly(2026, 11, 1),
                status = "Chosen",
                notes = " Pets allowed ",
            });
        Assert.Equal(HttpStatusCode.Created, chosen.Status);
        Assert.Equal("Kreuzberg flat", chosen.Body!.Name);
        Assert.Equal("Kreuzberg", chosen.Body.Area);
        Assert.Equal(1200m, chosen.Body.Rent);
        Assert.Equal(2400m, chosen.Body.Deposit);
        Assert.Equal("EUR", chosen.Body.CurrencyCode);
        Assert.Equal("2 bedrooms", chosen.Body.Size);
        Assert.Equal(HousingFurnishing.Furnished, chosen.Body.Furnishing);
        Assert.Equal(new DateOnly(2026, 11, 1), chosen.Body.AvailableOn);
        Assert.Equal(HousingStatus.Chosen, chosen.Body.Status);
        Assert.Equal("Pets allowed", chosen.Body.Notes);

        var secondChosen = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"{housingPath}/{first.Body.Id}",
            new
            {
                name = "Mitte room",
                status = "Chosen",
            });
        Assert.Equal(HttpStatusCode.BadRequest, secondChosen.Status);
        Assert.Contains(HousingRules.AlreadyChosenMessage, secondChosen.Body.ToString(), StringComparison.Ordinal);

        var listedHomes = await SendAsync<List<HousingOptionResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            housingPath,
            body: null);
        Assert.Equal(HttpStatusCode.OK, listedHomes.Status);
        Assert.Equal([chosen.Body.Id, first.Body.Id], listedHomes.Body!.Select(home => home.Id));

        var application = await SendAsync<JobApplicationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            applicationsPath,
            new
            {
                employer = " Northwind ",
                role = " Engineer ",
                location = " Berlin ",
                appliedOn = new DateOnly(2026, 10, 1),
                nextOn = new DateOnly(2026, 11, 15),
                status = "Interviewing",
                includesSupport = true,
                notes = " Second round ",
            });
        Assert.Equal(HttpStatusCode.Created, application.Status);
        Assert.Equal("Northwind", application.Body!.Employer);
        Assert.Equal("Engineer", application.Body.Role);
        Assert.Equal("Berlin", application.Body.Location);
        Assert.Equal(new DateOnly(2026, 10, 1), application.Body.AppliedOn);
        Assert.Equal(new DateOnly(2026, 11, 15), application.Body.NextOn);
        Assert.Equal(JobApplicationStatus.Interviewing, application.Body.Status);
        Assert.True(application.Body.IncludesSupport);
        Assert.Equal("Second round", application.Body.Notes);

        var later = await SendAsync<JobApplicationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            applicationsPath,
            new
            {
                employer = "Contoso",
                role = "Analyst",
                status = "Applied",
                nextOn = new DateOnly(2026, 12, 1),
            });
        Assert.Equal(HttpStatusCode.Created, later.Status);

        var listedApplications = await SendAsync<List<JobApplicationResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            applicationsPath,
            body: null);
        Assert.Equal(
            [application.Body.Id, later.Body!.Id],
            listedApplications.Body!.Select(item => item.Id));

        var calendar = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/calendar",
            body: null);
        Assert.Equal(HttpStatusCode.OK, calendar.Status);
        Assert.Equal(0, calendar.Body.GetProperty("events").GetArrayLength());

        var removed = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"{applicationsPath}/{later.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removed.Status);

        var otherDelete = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Delete,
            $"{housingPath}/{chosen.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, otherDelete.Status);

        var domestic = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(
                name: "Austin",
                targetDate: new DateOnly(2026, 12, 1),
                destinationCountry: "US",
                destinationCity: "Chicago"));
        Assert.False(domestic.Body!.IsInternational);
        var domesticHousing = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{domestic.Body.Id}/housing",
            new { name = "Chicago flat", status = "Considering" });
        Assert.Equal(HttpStatusCode.BadRequest, domesticHousing.Status);
        Assert.Contains(HousingRules.InternationalOnlyMessage, domesticHousing.Body.ToString(), StringComparison.Ordinal);
        var domesticApplications = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{domestic.Body.Id}/applications",
            new { employer = "Contoso", role = "Analyst", status = "Applied" });
        Assert.Equal(HttpStatusCode.BadRequest, domesticApplications.Status);
        Assert.Contains(
            JobApplicationRules.InternationalOnlyMessage,
            domesticApplications.Body.ToString(),
            StringComparison.Ordinal);
        var domesticList = await SendAsync<List<HousingOptionResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{domestic.Body.Id}/housing",
            body: null);
        Assert.Equal(HttpStatusCode.OK, domesticList.Status);
        Assert.Empty(domesticList.Body!);

        var deleted = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);
        await using var db = _postgres.CreateContext();
        Assert.False(await db.HousingOptions.AnyAsync(option => option.RelocationId == relocationId));
        Assert.False(await db.JobApplications.AnyAsync(applicationRow => applicationRow.RelocationId == relocationId));
    }
}
