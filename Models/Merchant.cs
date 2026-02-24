namespace Dungnz.Models;

using Dungnz.Systems;

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
    /// <returns>A new <see cref="Merchant"/> instance stocked for the given floor.</returns>
    public static Merchant CreateRandom(Random rng, int floor = 1, IReadOnlyList<Item>? allItems = null)
    {
        List<MerchantItem> stock;

        if (allItems is { Count: > 0 })
        {
            stock = MerchantInventoryConfig.GetStockForFloor(floor, allItems, rng);
        }
        else
        {
            stock = new List<MerchantItem>();
        }

        // Fallback: if JSON loading yielded nothing, use a minimal hardcoded set
        if (stock.Count == 0)
        {
            stock = GetFallbackStock();
        }

        return new Merchant { Stock = stock };
    }

    private static List<MerchantItem> GetFallbackStock() =>
    [
        new() { Item = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20, Description = "Restores 20 HP.", Tier = ItemTier.Common }, Price = 25 },
        new() { Item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true, Description = "A sturdy iron blade.", Tier = ItemTier.Common }, Price = 50 },
        new() { Item = new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 3, IsEquippable = true, Description = "Basic leather protection.", Tier = ItemTier.Common }, Price = 40 },
    ];
}
