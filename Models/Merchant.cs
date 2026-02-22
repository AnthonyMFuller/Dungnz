namespace Dungnz.Models;

public class MerchantItem
{
    public Item Item { get; init; } = null!;
    public int Price { get; init; }
}

public class Merchant
{
    public string Name { get; init; } = "Wandering Merchant";
    public List<MerchantItem> Stock { get; init; } = new();

    public static Merchant CreateRandom(Random rng)
    {
        var items = new List<MerchantItem>
        {
            new() { Item = new Item { Name="Health Potion", Type=ItemType.Consumable, HealAmount=30, Description="A murky red liquid in a stoppered vial. Smells terrible. Works anyway." }, Price = 25 },
            new() { Item = new Item { Name="Mana Potion", Type=ItemType.Consumable, HealAmount=0, MaxManaBonus=20, Description="Faintly luminescent blue liquid. The arcane energy inside makes your fingers tingle." }, Price = 20 },
            new() { Item = new Item { Name="Iron Sword", Type=ItemType.Weapon, AttackBonus=4, IsEquippable=true, Description="A battered blade, nicked from hard use. It has drawn blood before and will draw it again." }, Price = 50 },
            new() { Item = new Item { Name="Iron Shield", Type=ItemType.Armor, DefenseBonus=4, IsEquippable=true, Description="Dented iron, dull as old pewter. It has stopped worse than whatever is down here." }, Price = 45 },
            new() { Item = new Item { Name="Elixir of Strength", Type=ItemType.Consumable, HealAmount=0, Description="A thick amber fluid. Warriors swear by it. It tastes like iron and lightning.", AttackBonus=2 }, Price = 80 },
        };
        // Pick 3 random items to stock
        var stock = new List<MerchantItem>();
        var indices = Enumerable.Range(0, items.Count).OrderBy(_ => rng.Next()).Take(3).ToList();
        foreach (var i in indices) stock.Add(items[i]);
        return new Merchant { Stock = stock };
    }
}
