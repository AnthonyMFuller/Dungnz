namespace Dungnz.Models;

/// <summary>
/// Defines the drop pool for a specific enemy, combining a configurable list of chance-based
/// item drops (e.g., boss keys) with a tiered random item system that scales with player level.
/// Gold is also rolled from a configurable min/max range.
/// </summary>
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
        new Item { Name = "Ring of Focus", Type = ItemType.Accessory, StatModifier = 0, Description = "+15 MaxMana, -20% ability cooldowns.", IsEquippable = true, MaxManaBonus = 15 },
        new Item { Name = "Cloak of Shadows", Type = ItemType.Accessory, Description = "+10% dodge chance.", IsEquippable = true, DodgeBonus = 0.10f }
    };

    /// <summary>
    /// Initialises a new <see cref="LootTable"/> with an optional random-number generator and
    /// a gold drop range.
    /// </summary>
    /// <param name="rng">
    /// The <see cref="Random"/> instance to use for all probability rolls. If <c>null</c>,
    /// a new <see cref="Random"/> is created automatically.
    /// </param>
    /// <param name="minGold">The minimum gold that can be dropped (inclusive). Defaults to 0.</param>
    /// <param name="maxGold">The maximum gold that can be dropped (inclusive). Defaults to 0.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minGold"/> is greater than <paramref name="maxGold"/>.</exception>
    public LootTable(Random? rng = null, int minGold = 0, int maxGold = 0)
    {
        if (minGold > maxGold)
            throw new ArgumentException($"minGold ({minGold}) must not exceed maxGold ({maxGold}).", nameof(minGold));
        _rng = rng ?? new Random();
        _minGold = minGold;
        _maxGold = maxGold;
    }

    /// <summary>
    /// Registers an item as a possible drop with the specified probability.
    /// Multiple items may be added; the first one whose probability roll succeeds is used.
    /// </summary>
    /// <param name="item">The item to add to the explicit drop pool.</param>
    /// <param name="chance">The probability [0.0, 1.0] that this item drops when <see cref="RollDrop"/> is called.</param>
    public void AddDrop(Item item, double chance) => _drops.Add((item, chance));

    /// <summary>
    /// Executes a full loot roll for a defeated enemy, returning any item that was selected and
    /// the gold amount to award. Explicit drops (registered via <see cref="AddDrop"/>) are tried
    /// first; if none trigger, a 30 % chance of a random tiered item applies based on
    /// <paramref name="playerLevel"/>. Elite enemies are guaranteed at least a tier-2 item.
    /// </summary>
    /// <param name="enemy">The defeated enemy, used to check <see cref="Enemy.IsElite"/> for tier escalation.</param>
    /// <param name="playerLevel">The player's current level, used to select the appropriate item tier pool.</param>
    /// <returns>A <see cref="LootResult"/> containing the optional item drop and the gold amount.</returns>
    public LootResult RollDrop(Enemy enemy, int playerLevel = 1)
    {
        int gold = _minGold == _maxGold ? _minGold : _rng.Next(_minGold, _maxGold + 1);

        Item? dropped = null;

        // Check configured drops first (boss key, etc.)
        foreach (var (item, chance) in _drops)
        {
            if (_rng.NextDouble() < chance) { dropped = item.Clone(); break; }
        }

        // 30% chance of a tiered item drop if none already rolled
        if (dropped == null && _rng.NextDouble() < 0.30)
        {
            var pool = playerLevel >= 7 ? Tier3Items
                     : playerLevel >= 4 ? Tier2Items
                     : Tier1Items;

            // Elite enemies guarantee tier-2+ drop
            if (enemy?.IsElite == true && pool == Tier1Items) pool = Tier2Items;

            dropped = pool[_rng.Next(pool.Count)].Clone();
        }

        return new LootResult { Item = dropped, Gold = gold };
    }
}
