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
            new() { Item = new Item { Name="Health Potion", Type=ItemType.Consumable, HealAmount=30, Description="Restores 30 HP." }, Price = 25 },
            new() { Item = new Item { Name="Mana Potion", Type=ItemType.Consumable, HealAmount=0, Description="Restores 20 mana." }, Price = 20 },
            new() { Item = new Item { Name="Iron Sword", Type=ItemType.Weapon, AttackBonus=4, IsEquippable=true, Description="A sturdy iron blade." }, Price = 50 },
            new() { Item = new Item { Name="Iron Shield", Type=ItemType.Armor, DefenseBonus=4, IsEquippable=true, Description="A basic shield." }, Price = 45 },
            new() { Item = new Item { Name="Elixir of Strength", Type=ItemType.Consumable, HealAmount=0, Description="Permanently +2 Attack.", AttackBonus=2 }, Price = 80 },
        };
        // Pick 3 random items to stock
        var stock = new List<MerchantItem>();
        var indices = Enumerable.Range(0, items.Count).OrderBy(_ => rng.Next()).Take(3).ToList();
        foreach (var i in indices) stock.Add(items[i]);
        return new Merchant { Stock = stock };
    }
}
