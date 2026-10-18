using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Inventory;

public sealed class ConfirmInventoryListRequest
{
    [JsonRequired]
    public Furnishing Furnishing { get; set; }

    public ItemRoom? Room { get; set; }

    [JsonRequired]
    public List<string> Codes { get; set; } = [];
}
