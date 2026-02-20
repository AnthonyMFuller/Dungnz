namespace Dungnz.Models;

public class LootTable
{
    private readonly List<(Item item, double chance)> _drops = new();
    private readonly int _minGold;
    private readonly int _maxGold;
    private readonly Random _rng;

    // Tiered item pools by player level
    private static readonly List<Item> Tier1Items = new()
    {
        new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, Description = "A basic blade.", IsEquippable = true },
        new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 5, Description = "Light protection.", IsEquippable = true }
    };
    private static readonly List<Item> Tier2Items = new()
    {
        new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Description = "A quality weapon.", IsEquippable = true },
        new Item { Name = "Chain Mail", Type = ItemType.Armor, DefenseBonus = 10, Description = "Solid protection.", IsEquippable = true },
        new Item { Name = "Sword of Flames", Type = ItemType.Weapon, AttackBonus = 5, Description = "Burns with inner fire. Applies Bleed on hit.", IsEquippable = true, AppliesBleedOnHit = true },
        new Item { Name = "Armor of the Turtle", Type = ItemType.Armor, DefenseBonus = 15, Description = "Heavy shell. Grants Poison immunity.", IsEquippable = true, PoisonImmunity = true }
    };
    private static readonly List<Item> Tier3Items = new()
    {
        new Item { Name = "Mythril Blade", Type = ItemType.Weapon, AttackBonus = 8, Description = "Razor-sharp alloy.", IsEquippable = true },
        new Item { Name = "Plate Armor", Type = ItemType.Armor, DefenseBonus = 15, Description = "Near-impenetrable.", IsEquippable = true },
        new Item { Name = "Ring of Focus", Type = ItemType.Accessory, StatModifier = 15, Description = "+15 MaxMana, -20% ability cooldowns.", IsEquippable = true, MaxManaBonus = 15 },
        new Item { Name = "Cloak of Shadows", Type = ItemType.Accessory, Description = "+10% dodge chance.", IsEquippable = true, DodgeBonus = 0.10f }
    };

    public LootTable(Random? rng = null, int minGold = 0, int maxGold = 0)
    {
        _rng = rng ?? new Random();
        _minGold = minGold;
        _maxGold = maxGold;
    }

    public void AddDrop(Item item, double chance) => _drops.Add((item, chance));

    public LootResult RollDrop(Enemy enemy, int playerLevel = 1)
    {
        int gold = _minGold == _maxGold ? _minGold : _rng.Next(_minGold, _maxGold + 1);

        Item? dropped = null;

        // Check configured drops first (boss key, etc.)
        foreach (var (item, chance) in _drops)
        {
            if (_rng.NextDouble() < chance) { dropped = item; break; }
        }

        // 30% chance of a tiered item drop if none already rolled
        if (dropped == null && _rng.NextDouble() < 0.30)
        {
            var pool = playerLevel >= 7 ? Tier3Items
                     : playerLevel >= 4 ? Tier2Items
                     : Tier1Items;

            // Elite enemies guarantee tier-2+ drop
            if (enemy.IsElite && pool == Tier1Items) pool = Tier2Items;

            dropped = pool[_rng.Next(pool.Count)];
        }

        return new LootResult { Item = dropped, Gold = gold };
    }
}
