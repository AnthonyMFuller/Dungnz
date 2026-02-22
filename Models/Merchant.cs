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
    /// Creates a new <see cref="Merchant"/> with a randomly chosen subset of three items from
    /// the predefined pool of available goods.
    /// </summary>
    /// <param name="rng">The random number generator used to select the stock.</param>
    /// <returns>A new <see cref="Merchant"/> instance with three randomly chosen items.</returns>
    public static Merchant CreateRandom(Random rng)
    {
        var items = new List<MerchantItem>
        {
            new() { Item = new Item { Name="Health Potion", Type=ItemType.Consumable, HealAmount=20, Description="A murky red liquid in a stoppered vial. Smells terrible. Works anyway.", Tier=ItemTier.Common }, Price = 25 },
            new() { Item = new Item { Name="Mana Potion", Type=ItemType.Consumable, HealAmount=0, ManaRestore=20, Description="Faintly luminescent blue liquid. The arcane energy inside makes your fingers tingle.", Tier=ItemTier.Common }, Price = 20 },
            new() { Item = new Item { Name="Iron Sword", Type=ItemType.Weapon, AttackBonus=4, IsEquippable=true, Description="A battered blade, nicked from hard use. It has drawn blood before and will draw it again.", Tier=ItemTier.Common }, Price = 50 },
            new() { Item = new Item { Name="Iron Shield", Type=ItemType.Armor, DefenseBonus=4, IsEquippable=true, Description="Dented iron, dull as old pewter. It has stopped worse than whatever is down here.", Tier=ItemTier.Common }, Price = 45 },
            new() { Item = new Item { Name="Elixir of Strength", Type=ItemType.Consumable, HealAmount=0, Description="A thick amber fluid. Warriors swear by it. It tastes like iron and lightning.", AttackBonus=2, Tier=ItemTier.Uncommon }, Price = 80 },
        };
        // Pick 3 random items to stock
        var stock = new List<MerchantItem>();
        var indices = Enumerable.Range(0, items.Count).OrderBy(_ => rng.Next()).Take(3).ToList();
        foreach (var i in indices) stock.Add(items[i]);
        return new Merchant { Stock = stock };
    }
}
