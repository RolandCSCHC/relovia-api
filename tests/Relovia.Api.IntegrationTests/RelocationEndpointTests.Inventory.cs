using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task Anonymous_inventory_requests_are_unauthorized()
    {
        using var client = CreateClient().Client;
        var relocationId = Guid.CreateVersion7();

        var response = await client.GetAsync($"/api/relocations/{relocationId}/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_owner_records_belongings_for_shopping_and_moving()
    {
        var owner = await SignInAsync("inventory-owner@example.com");
        using var client = CreateClient();
        var relocationId = await CreateRelocationAsync(client, owner.Token, "Inventory");
        var path = $"/api/relocations/{relocationId}/inventory";

        var empty = await SendAsync<List<InventoryItemResponse>>(client, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Equal(HttpStatusCode.OK, empty.Status);
        Assert.Empty(empty.Body!);

        var sofa = await SendAsync<InventoryItemResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(
                name: " Sofa ",
                room: "LivingRoom",
                quantity: 1,
                acquisition: "Owned",
                action: "Bring",
                priority: "High",
                estimated: 1200,
                currency: " eur "));
        Assert.Equal(HttpStatusCode.Created, sofa.Status);
        Assert.EndsWith($"/inventory/{sofa.Body!.Id}", sofa.Response.Headers.Location?.OriginalString);
        Assert.Equal("Sofa", sofa.Body.Name);
        Assert.Equal(ItemRoom.LivingRoom, sofa.Body.Room);
        Assert.Equal(AcquisitionStatus.Owned, sofa.Body.Acquisition);
        Assert.Equal(MovingAction.Bring, sofa.Body.MovingAction);
        Assert.Equal(PackingStatus.NotPacked, sofa.Body.PackingStatus);
        Assert.Equal(ItemPriority.High, sofa.Body.Priority);
        Assert.Equal(1200m, sofa.Body.EstimatedPrice);
        Assert.Null(sofa.Body.ActualPrice);
        Assert.Equal("EUR", sofa.Body.CurrencyCode);

        var setUp = await SendAsync<BudgetResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/budget",
            new { currencyCode = "EUR" });
        var furnishing = setUp.Body!.Categories.Single(category => category.Name == "Furnishing");

        var lamp = await SendAsync<InventoryItemResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(
                name: "Lamp",
                category: "Appliances",
                room: "Bedroom",
                quantity: 2,
                acquisition: "ToPurchase",
                priority: "Normal",
                actual: 45.5m,
                currency: "USD",
                budgetCategoryId: furnishing.Id,
                notes: "  Warm light  "));
        Assert.Equal(HttpStatusCode.Created, lamp.Status);
        Assert.Equal(ItemCategory.Appliances, lamp.Body!.Category);
        Assert.Equal(ItemRoom.Bedroom, lamp.Body.Room);
        Assert.Null(lamp.Body.MovingAction);
        Assert.Null(lamp.Body.PackingStatus);
        Assert.Equal(45.5m, lamp.Body.ActualPrice);
        Assert.Equal("USD", lamp.Body.CurrencyCode);
        Assert.Equal(furnishing.Id, lamp.Body.BudgetCategoryId);
        Assert.Equal("Warm light", lamp.Body.Notes);

        var compared = await SendAsync<BudgetResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/budget",
            body: null);
        var comparedFurnishing = compared.Body!.Categories.Single(category => category.Name == "Furnishing");
        Assert.Equal(0m, comparedFurnishing.Estimated);
        Assert.Equal(22.75m, comparedFurnishing.Actual);
        Assert.Equal("Lamp", Assert.Single(comparedFurnishing.Shopping).Name);

        var packed = await SendAsync<InventoryItemResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{sofa.Body.Id}",
            Item(
                name: "Sofa",
                room: "LivingRoom",
                quantity: 1,
                acquisition: "Owned",
                action: "Bring",
                packing: "Packed",
                priority: "High",
                estimated: 1200,
                actual: 1100,
                currency: "EUR"));
        Assert.Equal(HttpStatusCode.OK, packed.Status);
        Assert.Equal(PackingStatus.Packed, packed.Body!.PackingStatus);
        Assert.Equal(1100m, packed.Body.ActualPrice);
        Assert.Equal(1200m, packed.Body.EstimatedPrice);

        var selling = await SendAsync<InventoryItemResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{sofa.Body.Id}",
            Item(
                name: "Sofa",
                room: "LivingRoom",
                quantity: 1,
                acquisition: "Owned",
                action: "Sell",
                priority: "Normal"));
        Assert.Equal(HttpStatusCode.OK, selling.Status);
        Assert.Equal(MovingAction.Sell, selling.Body!.MovingAction);
        Assert.Null(selling.Body.PackingStatus);
        Assert.Null(selling.Body.EstimatedPrice);
        Assert.Null(selling.Body.CurrencyCode);

        var listed = await SendAsync<List<InventoryItemResponse>>(client, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Equal([lamp.Body.Id, selling.Body.Id], listed.Body!.Select(item => item.Id));

        var packingOnASale = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(name: "Desk", acquisition: "Owned", action: "Donate", packing: "Packed", priority: "Low"));
        Assert.Equal(HttpStatusCode.BadRequest, packingOnASale.Status);
        Assert.Equal(
            "Packing status applies only when you are bringing the item.",
            packingOnASale.Body!.GetProperty("errors").GetProperty("packingStatus")[0].GetString());

        var priceWithoutCurrency = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(name: "Desk", acquisition: "ToPurchase", priority: "Low", estimated: 80));
        Assert.Equal(HttpStatusCode.BadRequest, priceWithoutCurrency.Status);
        Assert.Equal(
            "Enter a three-letter currency code.",
            priceWithoutCurrency.Body!.GetProperty("errors").GetProperty("currencyCode")[0].GetString());

        var priceWithoutCategory = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(name: "Desk", acquisition: "ToPurchase", priority: "Low", estimated: 80, currency: "EUR"));
        Assert.Equal(HttpStatusCode.BadRequest, priceWithoutCategory.Status);
        Assert.Equal(
            "Choose a budget category.",
            priceWithoutCategory.Body!.GetProperty("errors").GetProperty("budgetCategoryId")[0].GetString());

        var inUse = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}/budget/categories/{furnishing.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.Conflict, inUse.Status);

        var removed = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/{lamp.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removed.Status);

        var missing = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/{lamp.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, missing.Status);

        await using var db = _postgres.CreateContext();
        var stored = await db.InventoryItems.SingleAsync(item => item.RelocationId == relocationId);
        Assert.Equal("Sofa", stored.Name);
        Assert.Equal(MovingAction.Sell, stored.MovingAction);
        Assert.Null(stored.PackingStatus);
    }

    [Fact]
    public async Task Inventory_is_private_to_the_owner_and_goes_with_the_relocation()
    {
        var owner = await SignInAsync("inventory-private-owner@example.com");
        var other = await SignInAsync("inventory-private-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocationId = await CreateRelocationAsync(ownerClient, owner.Token, "Mine");
        var path = $"/api/relocations/{relocationId}/inventory";

        var setUp = await SendAsync<BudgetResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/budget",
            new { currencyCode = "EUR" });
        var furnishing = setUp.Body!.Categories.Single(category => category.Name == "Furnishing");
        var created = await SendAsync<InventoryItemResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(
                name: "Boxes",
                acquisition: "ToPurchase",
                priority: "Normal",
                estimated: 20,
                currency: "EUR",
                budgetCategoryId: furnishing.Id));
        Assert.Equal(HttpStatusCode.Created, created.Status);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync<List<InventoryItemResponse>>(otherClient, other.Token, HttpMethod.Get, path, body: null)).Status);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync<InventoryItemResponse>(
                otherClient,
                other.Token,
                HttpMethod.Put,
                $"{path}/{created.Body!.Id}",
                Item(name: "Hijack", acquisition: "Owned", priority: "Low"))).Status);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync<JsonElement>(
                otherClient,
                other.Token,
                HttpMethod.Delete,
                $"{path}/{created.Body.Id}",
                body: null)).Status);

        var deleted = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);

        await using var db = _postgres.CreateContext();
        Assert.False(await db.InventoryItems.AnyAsync(item => item.RelocationId == relocationId));
    }

    private static object Item(
        string name,
        string acquisition,
        string priority,
        string? category = null,
        string? room = null,
        int quantity = 1,
        string? action = null,
        string? packing = null,
        decimal? estimated = null,
        decimal? actual = null,
        string? currency = null,
        Guid? budgetCategoryId = null,
        string? notes = null)
    {
        return new
        {
            name,
            category,
            room,
            quantity,
            acquisition,
            movingAction = action,
            packingStatus = packing,
            priority,
            estimatedPrice = estimated,
            actualPrice = actual,
            currencyCode = currency,
            budgetCategoryId,
            notes,
        };
    }
}
