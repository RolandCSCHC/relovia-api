using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Housing;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task Monthly_net_income_is_a_share_of_each_homes_rent()
    {
        var owner = await SignInAsync("income-owner@example.com");
        var other = await SignInAsync("income-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocationId = await CreateRelocationAsync(ownerClient, owner.Token, "Income");
        var budgetPath = $"/api/relocations/{relocationId}/budget";
        var incomePath = $"{budgetPath}/income";
        var housingPath = $"/api/relocations/{relocationId}/housing";

        var anonymous = await CreateClient().Client.PutAsJsonAsync(
            incomePath,
            new { amount = 4000m, currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var hidden = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Put,
            incomePath,
            new { amount = 4000m, currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);

        var tooSoon = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            incomePath,
            new { amount = 4000m, currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.Conflict, tooSoon.Status);
        Assert.Contains(BudgetService.BudgetNotSetUpTitle, tooSoon.Body.ToString(), StringComparison.Ordinal);

        var setUp = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            budgetPath,
            new { currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.OK, setUp.Status);

        var amountOnly = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            incomePath,
            new { amount = 4000m });
        Assert.Equal(HttpStatusCode.BadRequest, amountOnly.Status);
        Assert.Contains(BudgetRules.IncomePairMessage, amountOnly.Body.ToString(), StringComparison.Ordinal);

        var saved = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            incomePath,
            new { amount = 4000m, currencyCode = " eur " });
        Assert.Equal(HttpStatusCode.OK, saved.Status);
        Assert.Equal(4000m, saved.Body!.MonthlyNetIncome);
        Assert.Equal("EUR", saved.Body.MonthlyNetIncomeCurrency);
        Assert.Equal(0m, saved.Body.Totals.Estimated);
        Assert.Equal(0m, saved.Body.Totals.Actual);
        Assert.All(saved.Body.Categories, category => Assert.Empty(category.Estimates));

        var changedCurrency = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            budgetPath,
            new { currencyCode = "USD" });
        Assert.Equal("USD", changedCurrency.Body!.CurrencyCode);
        Assert.Equal(4000m, changedCurrency.Body.MonthlyNetIncome);
        Assert.Equal("EUR", changedCurrency.Body.MonthlyNetIncomeCurrency);

        var monthly = await SendAsync<HousingOptionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new
            {
                name = "Kreuzberg flat",
                rent = 1200m,
                currencyCode = "EUR",
                rentPeriod = "Monthly",
                status = "Chosen",
            });
        Assert.Equal(HttpStatusCode.Created, monthly.Status);
        Assert.Equal(RentPeriod.Monthly, monthly.Body!.RentPeriod);
        Assert.Equal(30.0m, monthly.Body.RentPercentOfIncome);

        var afterChoice = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            budgetPath,
            body: null);
        Assert.Equal(0m, afterChoice.Body!.Totals.Estimated);
        Assert.All(afterChoice.Body.Categories, category => Assert.Empty(category.Estimates));
        var expenses = await SendAsync<List<ExpenseResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/expenses",
            body: null);
        Assert.Empty(expenses.Body!);

        var weekly = await SendAsync<HousingOptionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new
            {
                name = "Weekly room",
                rent = 300m,
                currencyCode = "USD",
                rentPeriod = "Weekly",
                status = "Considering",
            });
        Assert.Equal(16.3m, weekly.Body!.RentPercentOfIncome);

        var noRent = await SendAsync<HousingOptionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new { name = "A look", status = "Viewing" });
        Assert.Null(noRent.Body!.Rent);
        Assert.Null(noRent.Body.RentPeriod);
        Assert.Null(noRent.Body.RentPercentOfIncome);

        var missingRate = await SendAsync<HousingOptionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new
            {
                name = "Unknown currency",
                rent = 100m,
                currencyCode = "XTS",
                rentPeriod = "Monthly",
                status = "Dropped",
            });
        Assert.Equal(100m, missingRate.Body!.Rent);
        Assert.Null(missingRate.Body.RentPercentOfIncome);

        var missingPeriod = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            housingPath,
            new { name = "No period", rent = 100m, currencyCode = "EUR", status = "Considering" });
        Assert.Equal(HttpStatusCode.BadRequest, missingPeriod.Status);
        Assert.Contains(HousingRules.RentPeriodRequiredMessage, missingPeriod.Body.ToString(), StringComparison.Ordinal);

        var cleared = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            incomePath,
            new { amount = (decimal?)null, currencyCode = (string?)null });
        Assert.Null(cleared.Body!.MonthlyNetIncome);
        Assert.Null(cleared.Body.MonthlyNetIncomeCurrency);

        var listed = await SendAsync<List<HousingOptionResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            housingPath,
            body: null);
        Assert.All(listed.Body!, home => Assert.Null(home.RentPercentOfIncome));
        Assert.Equal(1200m, listed.Body!.Single(home => home.Name == "Kreuzberg flat").Rent);
        Assert.Equal(RentPeriod.Monthly, listed.Body!.Single(home => home.Name == "Kreuzberg flat").RentPeriod);
        Assert.Equal(RentPeriod.Weekly, listed.Body!.Single(home => home.Name == "Weekly room").RentPeriod);
    }
}
