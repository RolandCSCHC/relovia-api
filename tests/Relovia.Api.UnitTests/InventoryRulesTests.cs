using Relovia.Api.Features.Inventory;

namespace Relovia.Api.UnitTests;

public sealed class InventoryRulesTests
{
    [Fact]
    public void A_name_is_required_and_limited()
    {
        Assert.Equal("Name is required.", InventoryRules.Validate(Item(name: "  "))["name"][0]);
        Assert.Equal(
            "Name must be 200 characters or fewer.",
            InventoryRules.Validate(Item(name: new string('a', 201)))["name"][0]);
        Assert.Empty(InventoryRules.Validate(Item(name: new string('a', 200))));
    }

    [Fact]
    public void Category_and_room_can_be_left_empty_and_unknown_values_are_rejected()
    {
        Assert.Empty(InventoryRules.Validate(Item()));

        var item = Item();
        item.Category = (ItemCategory)9;
        item.Room = (ItemRoom)9;

        var errors = InventoryRules.Validate(item);
        Assert.Equal("Choose a category.", errors["category"][0]);
        Assert.Equal("Choose a room.", errors["room"][0]);
    }

    [Fact]
    public void A_price_on_a_purchase_needs_a_budget_category_and_an_owned_item_does_not_take_one()
    {
        var missing = InventoryRules.Validate(Item(
            acquisition: AcquisitionStatus.ToPurchase,
            estimated: 40,
            currency: "EUR"));
        Assert.Equal("Choose a budget category.", missing["budgetCategoryId"][0]);

        var owned = Item(estimated: 40, currency: "EUR");
        owned.BudgetCategoryId = Guid.CreateVersion7();
        Assert.Equal(
            "A budget category applies only when you still need to buy the item.",
            InventoryRules.Validate(owned)["budgetCategoryId"][0]);

        var buying = Item(acquisition: AcquisitionStatus.ToPurchase, estimated: 40, currency: "EUR");
        buying.BudgetCategoryId = Guid.CreateVersion7();
        Assert.Empty(InventoryRules.Validate(buying));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100_001)]
    public void Quantity_is_a_whole_number_from_1_to_100_000(int quantity)
    {
        var errors = InventoryRules.Validate(Item(quantity: quantity));

        Assert.Equal("Quantity must be a whole number from 1 to 100,000.", errors["quantity"][0]);
        Assert.Empty(InventoryRules.Validate(Item(quantity: 1)));
        Assert.Empty(InventoryRules.Validate(Item(quantity: 100_000)));
    }

    [Fact]
    public void Packing_status_applies_only_when_the_item_is_being_brought()
    {
        var selling = Item(action: MovingAction.Sell, packing: PackingStatus.Packed);
        Assert.Equal(
            "Packing status applies only when you are bringing the item.",
            InventoryRules.Validate(selling)["packingStatus"][0]);

        var undecided = Item(action: null, packing: PackingStatus.NotPacked);
        Assert.Equal(
            "Packing status applies only when you are bringing the item.",
            InventoryRules.Validate(undecided)["packingStatus"][0]);

        Assert.Empty(InventoryRules.Validate(Item(action: MovingAction.Bring, packing: null)));
        Assert.Empty(InventoryRules.Validate(Item(action: MovingAction.Bring, packing: PackingStatus.Packed)));
        Assert.Empty(InventoryRules.Validate(Item(action: MovingAction.Donate, packing: null)));
        Assert.Empty(InventoryRules.Validate(Item(action: null, packing: null)));
    }

    [Fact]
    public void A_price_needs_a_currency_and_a_currency_needs_a_price()
    {
        var noCurrency = Item(estimated: 10m, currency: null);
        Assert.Equal("Enter a three-letter currency code.", InventoryRules.Validate(noCurrency)["currencyCode"][0]);

        var currencyOnly = Item(currency: "EUR");
        Assert.Equal(
            "Enter a price, or leave the currency empty.",
            InventoryRules.Validate(currencyOnly)["currencyCode"][0]);

        Assert.Empty(InventoryRules.Validate(Item(estimated: 10m, currency: " eur ")));
        Assert.Empty(InventoryRules.Validate(Item(actual: 12.5m, currency: "USD")));
        Assert.Empty(InventoryRules.Validate(Item()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void A_price_must_be_greater_than_zero(int amount)
    {
        var errors = InventoryRules.Validate(Item(estimated: amount, currency: "EUR"));

        Assert.Equal("Amount must be greater than zero.", errors["estimatedPrice"][0]);
    }

    [Fact]
    public void A_price_keeps_at_most_four_decimal_places()
    {
        var tooPrecise = InventoryRules.Validate(Item(actual: 10.12345m, currency: "EUR"));

        Assert.Equal("Amount can have at most 4 decimal places.", tooPrecise["actualPrice"][0]);
        Assert.Empty(InventoryRules.Validate(Item(actual: 10.1234m, currency: "EUR")));
    }

    [Fact]
    public void Notes_are_optional_and_limited()
    {
        var item = Item();
        item.Notes = new string('n', 1001);

        Assert.Equal("Notes must be 1000 characters or fewer.", InventoryRules.Validate(item)["notes"][0]);
        item.Notes = "  ";
        Assert.Empty(InventoryRules.Validate(item));
    }

    [Fact]
    public void Unknown_status_values_are_rejected()
    {
        var item = Item();
        item.Acquisition = (AcquisitionStatus)9;
        item.MovingAction = (MovingAction)9;
        item.Priority = (ItemPriority)9;

        var errors = InventoryRules.Validate(item);

        Assert.Equal("Choose whether you own this or still need to buy it.", errors["acquisition"][0]);
        Assert.Equal("Choose what to do with this item.", errors["movingAction"][0]);
        Assert.Equal("Choose a priority.", errors["priority"][0]);
    }

    private static SaveInventoryItemRequest Item(
        string name = "Sofa",
        ItemCategory? category = null,
        ItemRoom? room = null,
        int quantity = 1,
        AcquisitionStatus acquisition = AcquisitionStatus.Owned,
        MovingAction? action = null,
        PackingStatus? packing = null,
        decimal? estimated = null,
        decimal? actual = null,
        string? currency = null)
    {
        return new SaveInventoryItemRequest
        {
            Name = name,
            Category = category,
            Room = room,
            Quantity = quantity,
            Acquisition = acquisition,
            MovingAction = action,
            PackingStatus = packing,
            Priority = ItemPriority.Normal,
            EstimatedPrice = estimated,
            ActualPrice = actual,
            CurrencyCode = currency,
        };
    }
}
