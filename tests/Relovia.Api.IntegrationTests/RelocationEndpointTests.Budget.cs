using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task Anonymous_budget_requests_are_unauthorized()
    {
        using var client = CreateClient().Client;
        var relocationId = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/relocations/{relocationId}/budget")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/relocations/{relocationId}/expenses")).StatusCode);
    }

    [Fact]
    public async Task The_owner_sets_up_a_budget_and_compares_estimates_with_expenses()
    {
        var owner = await SignInAsync("budget-owner@example.com");
        using var client = CreateClient();
        var relocationId = await CreateRelocationAsync(client, owner.Token, "Budgeted");
        var path = $"/api/relocations/{relocationId}/budget";

        var empty = await SendAsync<BudgetResponse>(client, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Equal(HttpStatusCode.OK, empty.Status);
        Assert.Null(empty.Body!.CurrencyCode);
        Assert.Empty(empty.Body.Categories);

        var needsCurrency = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/categories",
            new { name = "Pets" });
        Assert.Equal(HttpStatusCode.Conflict, needsCurrency.Status);

        var badCurrency = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Put,
            path,
            new { currencyCode = "euros" });
        Assert.Equal(HttpStatusCode.BadRequest, badCurrency.Status);

        var setUp = await SendAsync<BudgetResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            path,
            new { currencyCode = " eur " });
        Assert.Equal(HttpStatusCode.OK, setUp.Status);
        Assert.Equal("EUR", setUp.Body!.CurrencyCode);
        Assert.Equal(StarterBudgetCategories.Names.Order(StringComparer.OrdinalIgnoreCase), setUp.Body.Categories.Select(c => c.Name));

        var housing = setUp.Body.Categories.Single(category => category.Name == "Housing");
        var moving = setUp.Body.Categories.Single(category => category.Name == "Moving");

        var deposit = await SendAsync<BudgetEstimateResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = " Deposit ", amount = 2000, currencyCode = "eur" });
        Assert.Equal(HttpStatusCode.Created, deposit.Status);
        Assert.Equal("Deposit", deposit.Body!.Label);

        var rent = await SendAsync<BudgetEstimateResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = "First month", amount = 1500.5, currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.Created, rent.Status);

        var zero = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = "Free", amount = 0, currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.BadRequest, zero.Status);

        var sofa = await SendAsync<BudgetEstimateResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new
            {
                categoryId = setUp.Body.Categories.Single(category => category.Name == "Furnishing").Id,
                label = "Sofa",
                amount = 100,
                currencyCode = "nzd",
            });
        Assert.Equal(HttpStatusCode.Created, sofa.Status);
        Assert.Equal("NZD", sofa.Body!.CurrencyCode);
        Assert.Equal(100m, sofa.Body.Amount);
        Assert.Equal(25m, sofa.Body.ConvertedAmount);

        var expensesPath = $"/api/relocations/{relocationId}/expenses";
        var paid = await SendAsync<ExpenseResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            expensesPath,
            new
            {
                categoryId = housing.Id,
                description = "Deposit transfer",
                amount = 1800,
                currencyCode = "eur",
                paidOn = "2026-11-02",
                notes = "  Wire  ",
            });
        Assert.Equal(HttpStatusCode.Created, paid.Status);
        Assert.Equal("EUR", paid.Body!.CurrencyCode);
        Assert.Equal("Wire", paid.Body.Notes);

        var foreign = await SendAsync<ExpenseResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            expensesPath,
            new
            {
                categoryId = moving.Id,
                description = "Movers in Santiago",
                amount = 900000,
                currencyCode = "CLP",
                paidOn = "2026-10-20",
            });
        Assert.Equal(HttpStatusCode.Created, foreign.Status);
        Assert.Equal(900000m, foreign.Body!.Amount);
        Assert.Equal("CLP", foreign.Body.CurrencyCode);
        Assert.Equal(450m, foreign.Body.ConvertedAmount);
        Assert.Equal(1800m, paid.Body.ConvertedAmount);

        var compared = await SendAsync<BudgetResponse>(client, owner.Token, HttpMethod.Get, path, body: null);
        var comparedHousing = compared.Body!.Categories.Single(category => category.Name == "Housing");
        Assert.Equal(3500.5m, comparedHousing.Estimated);
        Assert.Equal(1800m, comparedHousing.Actual);
        Assert.Equal(1700.5m, comparedHousing.Remaining);
        Assert.Equal(450m, compared.Body.Categories.Single(category => category.Name == "Moving").Actual);
        Assert.Equal(25m, compared.Body.Categories.Single(category => category.Name == "Furnishing").Estimated);
        Assert.Equal(3525.5m, compared.Body.Totals.Estimated);
        Assert.Equal(2250m, compared.Body.Totals.Actual);
        Assert.Equal(1275.5m, compared.Body.Totals.Remaining);
        Assert.Empty(compared.Body.Unconverted);
        Assert.Equal(FakeRateProvider.UpdatedAt, compared.Body.RatesAsOf);
        Assert.False(compared.Body.RatesUnavailable);

        var listed = await SendAsync<List<ExpenseResponse>>(client, owner.Token, HttpMethod.Get, expensesPath, body: null);
        Assert.Equal([paid.Body.Id, foreign.Body.Id], listed.Body!.Select(expense => expense.Id));
        Assert.Equal([1800m, 450m], listed.Body!.Select(expense => expense.ConvertedAmount));
        Assert.Equal([1800m, 900000m], listed.Body!.Select(expense => expense.Amount));

        var updated = await SendAsync<ExpenseResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            $"{expensesPath}/{paid.Body.Id}",
            new
            {
                categoryId = housing.Id,
                description = "Deposit transfer",
                amount = 2000,
                currencyCode = "EUR",
                paidOn = "2026-11-02",
            });
        Assert.Equal(HttpStatusCode.OK, updated.Status);
        Assert.Null(updated.Body!.Notes);

        var renamed = await SendAsync<BudgetCategoryResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            $"{path}/categories/{housing.Id}",
            new { name = "Home" });
        Assert.Equal(HttpStatusCode.OK, renamed.Status);
        Assert.Equal(2000m, renamed.Body!.Actual);

        var duplicate = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/categories",
            new { name = " MOVING " });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.Status);
        Assert.Equal(
            "A category with this name already exists.",
            duplicate.Body!.GetProperty("errors").GetProperty("name")[0].GetString());

        var inUse = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/categories/{housing.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.Conflict, inUse.Status);

        var removedExpense = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"{expensesPath}/{paid.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removedExpense.Status);

        var removedCategory = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/categories/{housing.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removedCategory.Status);

        await using (var db = _postgres.CreateContext())
        {
            // Only the Furnishing estimate is left. The deleted category took its own estimates with it.
            var left = await db.BudgetEstimates.Where(estimate => estimate.RelocationId == relocationId).ToListAsync();
            Assert.Equal(["Sofa"], left.Select(estimate => estimate.Label));
        }

        var afterDelete = await SendAsync<BudgetResponse>(client, owner.Token, HttpMethod.Get, path, body: null);
        Assert.DoesNotContain(afterDelete.Body!.Categories, category => category.Id == housing.Id);
        Assert.Equal(25m, afterDelete.Body.Totals.Estimated);

        var changedCurrency = await SendAsync<BudgetResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            path,
            new { currencyCode = "USD" });
        Assert.Equal(HttpStatusCode.OK, changedCurrency.Status);
        Assert.Equal("USD", changedCurrency.Body!.CurrencyCode);

        // The currency is only what totals are shown in: the NZD estimate and CLP expense convert to USD.
        Assert.Equal(50m, changedCurrency.Body.Totals.Estimated);
        Assert.Equal(900m, changedCurrency.Body.Totals.Actual);
        Assert.Empty(changedCurrency.Body.Unconverted);

        var stored = await SendAsync<List<ExpenseResponse>>(client, owner.Token, HttpMethod.Get, expensesPath, body: null);
        var original = Assert.Single(stored.Body!);
        Assert.Equal((900000m, "CLP", 900m), (original.Amount, original.CurrencyCode, original.ConvertedAmount));
    }

    [Fact]
    public async Task Amounts_without_a_rate_are_reported_instead_of_guessed()
    {
        var owner = await SignInAsync("budget-norates@example.com");
        using var client = CreateClient();
        var relocationId = await CreateRelocationAsync(client, owner.Token, "No rates");
        var path = $"/api/relocations/{relocationId}/budget";

        // The fake provider does not know ZZZ, so nothing can be converted into it.
        var setUp = await SendAsync<BudgetResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            path,
            new { currencyCode = "ZZZ" });
        var housing = setUp.Body!.Categories.Single(category => category.Name == "Housing");

        await SendAsync<BudgetEstimateResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = "Local", amount = 100, currencyCode = "ZZZ" });
        await SendAsync<BudgetEstimateResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = "Abroad", amount = 40, currencyCode = "EUR" });
        var spent = await SendAsync<ExpenseResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/expenses",
            new
            {
                categoryId = housing.Id,
                description = "Abroad",
                amount = 10,
                currencyCode = "EUR",
                paidOn = "2026-11-02",
            });
        Assert.Equal(HttpStatusCode.Created, spent.Status);
        Assert.Null(spent.Body!.ConvertedAmount);

        var budget = await SendAsync<BudgetResponse>(client, owner.Token, HttpMethod.Get, path, body: null);
        Assert.True(budget.Body!.RatesUnavailable);
        Assert.Null(budget.Body.RatesAsOf);
        Assert.Equal(100m, budget.Body.Totals.Estimated);
        Assert.Equal(0m, budget.Body.Totals.Actual);
        var row = Assert.Single(budget.Body.Unconverted);
        Assert.Equal(("EUR", 1, 10m, 1, 40m, 0, 0m, 0m), (row.CurrencyCode, row.ExpenseCount, row.ExpenseTotal, row.EstimateCount, row.EstimateTotal, row.ShoppingCount, row.ShoppingEstimated, row.ShoppingSpent));
    }

    [Fact]
    public async Task Budget_data_is_private_to_the_owner_and_stays_inside_one_relocation()
    {
        var owner = await SignInAsync("budget-private-owner@example.com");
        var other = await SignInAsync("budget-private-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocationId = await CreateRelocationAsync(ownerClient, owner.Token, "Mine");
        var otherRelocationId = await CreateRelocationAsync(ownerClient, owner.Token, "Also mine");
        var path = $"/api/relocations/{relocationId}/budget";
        var expensesPath = $"/api/relocations/{relocationId}/expenses";

        var setUp = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            path,
            new { currencyCode = "EUR" });
        var housing = setUp.Body!.Categories.Single(category => category.Name == "Housing");
        var elsewhere = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{otherRelocationId}/budget",
            new { currencyCode = "EUR" });
        var foreignCategory = elsewhere.Body!.Categories.First();

        var estimate = await SendAsync<BudgetEstimateResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = "Deposit", amount = 100, currencyCode = "EUR" });
        var expense = await SendAsync<ExpenseResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            expensesPath,
            new
            {
                categoryId = housing.Id,
                description = "Deposit",
                amount = 100,
                currencyCode = "EUR",
                paidOn = "2026-11-02",
            });
        Assert.Equal(HttpStatusCode.Created, expense.Status);

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync<BudgetResponse>(otherClient, other.Token, HttpMethod.Get, path, body: null)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync<BudgetResponse>(otherClient, other.Token, HttpMethod.Put, path, new { currencyCode = "USD" })).Status);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync<BudgetEstimateResponse>(
                otherClient,
                other.Token,
                HttpMethod.Put,
                $"{path}/estimates/{estimate.Body!.Id}",
                new { categoryId = housing.Id, label = "Hijack", amount = 1, currencyCode = "EUR" })).Status);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync<JsonElement>(otherClient, other.Token, HttpMethod.Delete, $"{expensesPath}/{expense.Body!.Id}", body: null)).Status);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync<List<ExpenseResponse>>(otherClient, other.Token, HttpMethod.Get, expensesPath, body: null)).Status);

        var crossCategory = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            expensesPath,
            new
            {
                categoryId = foreignCategory.Id,
                description = "Wrong budget",
                amount = 5,
                currencyCode = "EUR",
                paidOn = "2026-11-02",
            });
        Assert.Equal(HttpStatusCode.BadRequest, crossCategory.Status);
        Assert.Equal(
            "Choose a category from this budget.",
            crossCategory.Body!.GetProperty("errors").GetProperty("categoryId")[0].GetString());

        var crossEstimate = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = foreignCategory.Id, label = "Wrong budget", amount = 5, currencyCode = "EUR" });
        Assert.Equal(HttpStatusCode.BadRequest, crossEstimate.Status);

        var ownerTask = await AddTaskAsync(ownerClient, owner.Token, otherRelocationId);
        var crossTask = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            expensesPath,
            new
            {
                categoryId = housing.Id,
                taskId = ownerTask,
                description = "Task from another relocation",
                amount = 5,
                currencyCode = "EUR",
                paidOn = "2026-11-02",
            });
        Assert.Equal(HttpStatusCode.BadRequest, crossTask.Status);
        Assert.Equal(
            "Choose a task from this relocation.",
            crossTask.Body!.GetProperty("errors").GetProperty("taskId")[0].GetString());
    }

    [Fact]
    public async Task Deleting_a_linked_task_keeps_the_expense_and_deleting_the_relocation_removes_the_budget()
    {
        var owner = await SignInAsync("budget-task-owner@example.com");
        using var client = CreateClient();
        var relocationId = await CreateRelocationAsync(client, owner.Token, "Linked");
        var path = $"/api/relocations/{relocationId}/budget";

        var setUp = await SendAsync<BudgetResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            path,
            new { currencyCode = "EUR" });
        var housing = setUp.Body!.Categories.Single(category => category.Name == "Housing");
        var taskId = await AddTaskAsync(client, owner.Token, relocationId);

        var expense = await SendAsync<ExpenseResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/expenses",
            new
            {
                categoryId = housing.Id,
                taskId,
                description = "Packing boxes",
                amount = 35.5,
                currencyCode = "EUR",
                paidOn = "2026-11-02",
            });
        Assert.Equal(HttpStatusCode.Created, expense.Status);
        Assert.Equal(taskId, expense.Body!.TaskId);

        var deletedTask = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}/tasks/{taskId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deletedTask.Status);

        var listed = await SendAsync<List<ExpenseResponse>>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/expenses",
            body: null);
        var kept = Assert.Single(listed.Body!);
        Assert.Null(kept.TaskId);
        Assert.Equal(35.5m, kept.Amount);

        await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/estimates",
            new { categoryId = housing.Id, label = "Deposit", amount = 100, currencyCode = "EUR" });

        var deleted = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);

        await using var db = _postgres.CreateContext();
        Assert.False(await db.Budgets.AnyAsync(budget => budget.RelocationId == relocationId));
        Assert.False(await db.BudgetCategories.AnyAsync(category => category.RelocationId == relocationId));
        Assert.False(await db.BudgetEstimates.AnyAsync(estimate => estimate.RelocationId == relocationId));
        Assert.False(await db.Expenses.AnyAsync(candidate => candidate.RelocationId == relocationId));
    }

    private async Task<Guid> CreateRelocationAsync(ClientScope client, string token, string name)
    {
        var created = await SendAsync<RelocationResponse>(
            client,
            token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: name, targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!.Id;
    }

    private async Task<Guid> AddTaskAsync(ClientScope client, string token, Guid relocationId)
    {
        var task = await SendAsync<RelocationTaskResponse>(
            client,
            token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/tasks",
            new { title = "Buy boxes", dueDate = (DateOnly?)null, phase = "Moving", priority = "Normal" });
        Assert.Equal(HttpStatusCode.Created, task.Status);
        return task.Body!.Id;
    }
}
