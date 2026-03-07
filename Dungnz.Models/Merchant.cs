namespace Dungnz.Models;

/// <summary>Represents a single item in a merchant's stock along with its sale price.</summary>
public class MerchantItem
{
    /// <summary>Gets the item being sold.</summary>
    public Item Item { get; init; } = null!;

    /// <summary>Gets the gold cost the player must pay to purchase this item.</summary>
    public int Price { get; init; }
}

/// <summary>
/// Represents a wandering merchant the player can encounter in dungeon rooms.
/// Holds a randomly selected subset of items that the player can purchase with gold.
/// </summary>
public class Merchant
{
    /// <summary>Gets the merchant's display name shown in the UI.</summary>
    public string Name { get; init; } = "Wandering Merchant";

    /// <summary>Gets the list of items currently available for purchase from this merchant.</summary>
    public List<MerchantItem> Stock { get; init; } = new();

    /// <summary>
    /// Creates a new <see cref="Merchant"/> with stock loaded from merchant-inventory.json for
    /// the given floor. Falls back to a basic hardcoded set if the JSON cannot be loaded.
    /// </summary>
    /// <param name="rng">The random number generator used to select the stock.</param>
    /// <param name="floor">The dungeon floor number (1–5), used to select appropriate items.</param>
    /// <param name="allItems">All available items for resolving IDs; pass empty/null to trigger fallback.</param>
    /// <param name="difficulty">Optional DifficultySettings for applying merchant price multiplier.</param>
    /// <returns>A new <see cref="Merchant"/> instance stocked for the given floor.</returns>
    public static Merchant CreateRandom(Random rng, int floor = 1, IReadOnlyList<Item>? allItems = null, DifficultySettings? difficulty = null)
    {
        List<MerchantItem> stock;

        if (allItems is { Count: > 0 })
        {
            stock = MerchantInventoryConfig.GetStockForFloor(floor, allItems, rng, difficulty);
        }
        else
        {
            stock = new List<MerchantItem>();
        }

        // Fallback: if JSON loading yielded nothing, use a minimal hardcoded set
        if (stock.Count == 0)
        {
            stock = DefaultItems.GetFallbackMerchantStock(difficulty);
        }

        return new Merchant { Stock = stock };
    }
}
