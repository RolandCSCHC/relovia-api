using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task A_room_list_becomes_items_to_buy_and_skips_codes_already_stored()
    {
        var owner = await SignInAsync("inventory-list-owner@example.com");
        var other = await SignInAsync("inventory-list-other@example.com");
        using var client = CreateClient();
        using var otherClient = CreateClient();
        var relocationId = await CreateRelocationAsync(client, owner.Token, "Lists");
        var path = $"/api/relocations/{relocationId}/inventory";
        var suggestionPath = $"{path}/suggestion?furnishing=Unfurnished";

        var anonymous = await CreateClient().Client.GetAsync(suggestionPath);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var missingHome = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"{path}/suggestion",
            body: null);
        Assert.Equal(HttpStatusCode.BadRequest, missingHome.Status);
        Assert.Equal(
            "Choose a furnished or unfurnished home.",
            missingHome.Body!.GetProperty("errors").GetProperty("furnishing")[0].GetString());

        var furnished = await SendAsync<InventorySuggestionResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"{path}/suggestion?furnishing=Furnished",
            body: null);
        Assert.Equal(HttpStatusCode.OK, furnished.Status);
        Assert.Contains(furnished.Body!.Items, item => item.Code == "frying-pan");
        Assert.DoesNotContain(furnished.Body.Items, item => item.Code is "sofa" or "fridge");

        var kitchen = await SendAsync<InventorySuggestionResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"{path}/suggestion?furnishing=Unfurnished&room=Kitchen",
            body: null);
        Assert.Equal(HttpStatusCode.OK, kitchen.Status);
        Assert.Equal(ItemRoom.Kitchen, kitchen.Body!.Room);
        Assert.All(kitchen.Body.Items, item => Assert.Equal(ItemRoom.Kitchen, item.Room));
        Assert.Contains(kitchen.Body.Items, item => item.Code == "fridge");
        Assert.DoesNotContain(kitchen.Body.Items, item => item.Code == "sofa");

        var byHand = await SendAsync<InventoryItemResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            path,
            Item(name: "Frying pan", acquisition: "Owned", priority: "Normal"));
        Assert.Equal(HttpStatusCode.Created, byHand.Status);
        Assert.Null(byHand.Body!.Code);

        var stillSuggested = await SendAsync<InventorySuggestionResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            suggestionPath,
            body: null);
        Assert.Contains(stillSuggested.Body!.Items, item => item.Code == "frying-pan");

        var empty = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/suggestion",
            new { furnishing = "Unfurnished", codes = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, empty.Status);
        Assert.Equal(
            "Select at least one item.",
            empty.Body!.GetProperty("errors").GetProperty("codes")[0].GetString());

        var unknown = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/suggestion",
            new { furnishing = "Unfurnished", codes = new[] { "not-a-list-item" } });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.Status);
        Assert.Equal(
            "Choose items from this list.",
            unknown.Body!.GetProperty("errors").GetProperty("codes")[0].GetString());

        var otherUser = await SendAsync<InventorySuggestionResponse>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            suggestionPath,
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, otherUser.Status);

        var confirmed = await SendAsync<List<InventoryItemResponse>>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/suggestion",
            new { furnishing = "Unfurnished", room = "Kitchen", codes = new[] { " frying-pan ", "fridge" } });
        Assert.Equal(HttpStatusCode.OK, confirmed.Status);
        Assert.Equal(["frying-pan", "fridge"], confirmed.Body!.Select(item => item.Code));
        Assert.All(confirmed.Body!, item =>
        {
            Assert.Equal(AcquisitionStatus.ToPurchase, item.Acquisition);
            Assert.Null(item.MovingAction);
            Assert.Null(item.EstimatedPrice);
            Assert.Null(item.CurrencyCode);
            Assert.Null(item.BudgetCategoryId);
            Assert.NotNull(item.SourceTemplateItemId);
        });
        Assert.Equal(ItemPriority.High, confirmed.Body!.Single(item => item.Code == "frying-pan").Priority);
        Assert.Equal("Frying pan", confirmed.Body!.Single(item => item.Code == "frying-pan").Name);

        var again = await SendAsync<InventorySuggestionResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"{path}/suggestion?furnishing=Unfurnished&room=Kitchen",
            body: null);
        Assert.DoesNotContain(again.Body!.Items, item => item.Code is "frying-pan" or "fridge");
        Assert.Contains(again.Body.Items, item => item.Code == "plates");

        var skipped = await SendAsync<List<InventoryItemResponse>>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"{path}/suggestion",
            new { furnishing = "Unfurnished", room = "Kitchen", codes = new[] { "frying-pan", "plates" } });
        Assert.Equal(HttpStatusCode.OK, skipped.Status);
        Assert.Equal(["plates"], skipped.Body!.Select(item => item.Code));

        var pan = confirmed.Body!.Single(item => item.Code == "frying-pan");
        var renamed = await SendAsync<InventoryItemResponse>(
            client,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{pan.Id}",
            Item(name: "Skillet", room: "Kitchen", quantity: 1, acquisition: "ToPurchase", priority: "High"));
        Assert.Equal(HttpStatusCode.OK, renamed.Status);
        Assert.Equal("Skillet", renamed.Body!.Name);
        Assert.Equal("frying-pan", renamed.Body.Code);

        await using var db = _postgres.CreateContext();
        var templateItem = await db.InventoryTemplateItems.SingleAsync(item => item.Code == "frying-pan");
        Assert.Equal("Frying pan", templateItem.Name);
        Assert.Equal(templateItem.Id, renamed.Body.SourceTemplateItemId);

        var removed = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/{pan.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removed.Status);

        var restored = await SendAsync<InventorySuggestionResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"{path}/suggestion?furnishing=Furnished&room=Kitchen",
            body: null);
        Assert.Contains(restored.Body!.Items, item => item.Code == "frying-pan");
    }
}
