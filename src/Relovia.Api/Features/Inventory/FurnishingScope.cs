namespace Relovia.Api.Features.Inventory;

/// <summary>Which starting points a room list belongs to. <see cref="Both"/> is included in either.</summary>
public enum FurnishingScope
{
    Furnished,
    Unfurnished,
    Both,
}
