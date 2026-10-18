namespace Relovia.Api.Features.Inventory;

/// <summary>
/// Room lists for a home. Lists scoped <see cref="FurnishingScope.Both"/> are the things a furnished
/// place still needs. Unfurnished lists add the furniture and appliances an empty home needs.
/// </summary>
public static class StarterInventoryLists
{
    public const string BathroomBasicsCode = "bathroom-basics";

    public const string BedroomBasicsCode = "bedroom-basics";

    public const string KitchenBasicsCode = "kitchen-basics";

    public const string BedroomFurnitureCode = "bedroom-furniture";

    public const string KitchenAppliancesCode = "kitchen-appliances";

    public const string LivingRoomCode = "living-room";

    public const string DiningRoomCode = "dining-room";

    public const string OfficeCode = "office";

    public static readonly DateTimeOffset PublishedAt = new(2026, 10, 18, 15, 30, 0, TimeSpan.Zero);

    public static readonly Guid BathroomBasicsId = Guid.Parse("019a18b0-7c00-7000-8000-000000000001");

    public static readonly Guid BedroomBasicsId = Guid.Parse("019a18b0-7c00-7000-8000-000000000002");

    public static readonly Guid KitchenBasicsId = Guid.Parse("019a18b0-7c00-7000-8000-000000000003");

    public static readonly Guid BedroomFurnitureId = Guid.Parse("019a18b0-7c00-7000-8000-000000000004");

    public static readonly Guid KitchenAppliancesId = Guid.Parse("019a18b0-7c00-7000-8000-000000000005");

    public static readonly Guid LivingRoomId = Guid.Parse("019a18b0-7c00-7000-8000-000000000006");

    public static readonly Guid DiningRoomId = Guid.Parse("019a18b0-7c00-7000-8000-000000000007");

    public static readonly Guid OfficeId = Guid.Parse("019a18b0-7c00-7000-8000-000000000008");

    public static IReadOnlyList<InventoryListTemplate> Templates { get; } =
    [
        List(BathroomBasicsId, BathroomBasicsCode, "Bathroom basics", ItemRoom.Bathroom, FurnishingScope.Both),
        List(BedroomBasicsId, BedroomBasicsCode, "Bedroom basics", ItemRoom.Bedroom, FurnishingScope.Both),
        List(KitchenBasicsId, KitchenBasicsCode, "Kitchen basics", ItemRoom.Kitchen, FurnishingScope.Both),
        List(BedroomFurnitureId, BedroomFurnitureCode, "Bedroom furniture", ItemRoom.Bedroom, FurnishingScope.Unfurnished),
        List(KitchenAppliancesId, KitchenAppliancesCode, "Kitchen appliances", ItemRoom.Kitchen, FurnishingScope.Unfurnished),
        List(LivingRoomId, LivingRoomCode, "Living room", ItemRoom.LivingRoom, FurnishingScope.Unfurnished),
        List(DiningRoomId, DiningRoomCode, "Dining room", ItemRoom.DiningRoom, FurnishingScope.Unfurnished),
        List(OfficeId, OfficeCode, "Office", ItemRoom.Office, FurnishingScope.Unfurnished),
    ];

    public static IReadOnlyList<InventoryTemplateItem> Items { get; } =
    [
        Item("019a18b0-7c00-7000-8000-000000000101", BathroomBasicsId, "bath-towels", "Bath towels", ItemCategory.Linens, 4, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000102", BathroomBasicsId, "hand-towels", "Hand towels", ItemCategory.Linens, 2, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000103", BathroomBasicsId, "toilet-brush", "Toilet brush", ItemCategory.Other, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000104", BathroomBasicsId, "bath-mat", "Bath mat", ItemCategory.Linens, 1, ItemPriority.Low),
        Item("019a18b0-7c00-7000-8000-000000000111", BedroomBasicsId, "bed-sheets", "Bed sheets", ItemCategory.Linens, 2, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000112", BedroomBasicsId, "duvet", "Duvet", ItemCategory.Linens, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000113", BedroomBasicsId, "pillows", "Pillows", ItemCategory.Linens, 2, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000114", BedroomBasicsId, "hangers", "Hangers", ItemCategory.Other, 20, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000115", BedroomBasicsId, "laundry-basket", "Laundry basket", ItemCategory.Other, 1, ItemPriority.Low),
        Item("019a18b0-7c00-7000-8000-000000000121", KitchenBasicsId, "frying-pan", "Frying pan", ItemCategory.Cookware, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000122", KitchenBasicsId, "saucepan", "Saucepan", ItemCategory.Cookware, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000123", KitchenBasicsId, "knife", "Chef's knife", ItemCategory.Cookware, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000124", KitchenBasicsId, "cutting-board", "Cutting board", ItemCategory.Cookware, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000125", KitchenBasicsId, "plates", "Plates", ItemCategory.Cookware, 4, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000126", KitchenBasicsId, "bowls", "Bowls", ItemCategory.Cookware, 4, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000127", KitchenBasicsId, "glasses", "Glasses", ItemCategory.Cookware, 4, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000128", KitchenBasicsId, "cutlery", "Cutlery", ItemCategory.Cookware, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000129", KitchenBasicsId, "kettle", "Kettle", ItemCategory.Appliances, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000130", KitchenBasicsId, "bin", "Bin", ItemCategory.Other, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000131", KitchenBasicsId, "tea-towels", "Tea towels", ItemCategory.Linens, 2, ItemPriority.Low),
        Item("019a18b0-7c00-7000-8000-000000000141", BedroomFurnitureId, "bed-frame", "Bed frame", ItemCategory.Furniture, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000142", BedroomFurnitureId, "mattress", "Mattress", ItemCategory.Furniture, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000143", BedroomFurnitureId, "wardrobe", "Wardrobe", ItemCategory.Furniture, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000151", KitchenAppliancesId, "fridge", "Fridge", ItemCategory.Appliances, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000152", KitchenAppliancesId, "cooker", "Cooker", ItemCategory.Appliances, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000153", KitchenAppliancesId, "microwave", "Microwave", ItemCategory.Appliances, 1, ItemPriority.Low),
        Item("019a18b0-7c00-7000-8000-000000000161", LivingRoomId, "sofa", "Sofa", ItemCategory.Furniture, 1, ItemPriority.High),
        Item("019a18b0-7c00-7000-8000-000000000162", LivingRoomId, "coffee-table", "Coffee table", ItemCategory.Furniture, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000163", LivingRoomId, "floor-lamp", "Floor lamp", ItemCategory.Decor, 1, ItemPriority.Low),
        Item("019a18b0-7c00-7000-8000-000000000164", LivingRoomId, "curtains", "Curtains", ItemCategory.Decor, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000171", DiningRoomId, "dining-table", "Dining table", ItemCategory.Furniture, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000172", DiningRoomId, "dining-chairs", "Dining chairs", ItemCategory.Furniture, 4, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000181", OfficeId, "desk", "Desk", ItemCategory.Furniture, 1, ItemPriority.Normal),
        Item("019a18b0-7c00-7000-8000-000000000182", OfficeId, "desk-chair", "Desk chair", ItemCategory.Furniture, 1, ItemPriority.Normal),
    ];

    private static InventoryListTemplate List(Guid id, string code, string title, ItemRoom room, FurnishingScope scope)
    {
        return new InventoryListTemplate
        {
            Id = id,
            Code = code,
            Version = 1,
            Title = title,
            Room = room,
            Scope = scope,
            PublishedAt = PublishedAt,
            CreatedAt = PublishedAt,
            UpdatedAt = PublishedAt,
        };
    }

    private static InventoryTemplateItem Item(
        string id,
        Guid templateId,
        string code,
        string name,
        ItemCategory category,
        int quantity,
        ItemPriority priority)
    {
        return new InventoryTemplateItem
        {
            Id = Guid.Parse(id),
            InventoryListTemplateId = templateId,
            Code = code,
            Name = name,
            Category = category,
            Quantity = quantity,
            Priority = priority,
            CreatedAt = PublishedAt,
            UpdatedAt = PublishedAt,
        };
    }
}
