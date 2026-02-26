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

    // Shared tier pools loaded from item-stats.json — set once via SetTierPools().
    // When null, RollDrop falls back to the small hardcoded lists below.
    private static IReadOnlyList<Item>? _sharedTier1;
    private static IReadOnlyList<Item>? _sharedTier2;
    private static IReadOnlyList<Item>? _sharedTier3;
    private static IReadOnlyList<Item>? _sharedLegendary;

    private static readonly IReadOnlyList<Item> FallbackLegendary = new List<Item>();

    private static readonly IReadOnlyList<Item> FallbackTier1 = new List<Item>
    {
        new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, Description = "A basic blade.", IsEquippable = true, Tier = ItemTier.Common },
        new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 5, Description = "Light protection.", IsEquippable = true, Tier = ItemTier.Common }
    };
    private static readonly IReadOnlyList<Item> FallbackTier2 = new List<Item>
    {
        new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Description = "A quality weapon.", IsEquippable = true, Tier = ItemTier.Uncommon },
        new Item { Name = "Chain Mail", Type = ItemType.Armor, DefenseBonus = 10, Description = "Solid protection.", IsEquippable = true, Tier = ItemTier.Uncommon },
        new Item { Name = "Sword of Flames", Type = ItemType.Weapon, AttackBonus = 5, Description = "Burns with inner fire. Applies Bleed on hit.", IsEquippable = true, AppliesBleedOnHit = true, Tier = ItemTier.Uncommon },
        new Item { Name = "Armor of the Turtle", Type = ItemType.Armor, DefenseBonus = 15, Description = "Heavy shell. Grants Poison immunity.", IsEquippable = true, PoisonImmunity = true, Tier = ItemTier.Uncommon }
    };
    private static readonly IReadOnlyList<Item> FallbackTier3 = new List<Item>
    {
        new Item { Name = "Mythril Blade", Type = ItemType.Weapon, AttackBonus = 8, Description = "Razor-sharp alloy.", IsEquippable = true, Tier = ItemTier.Rare },
        new Item { Name = "Plate Armor", Type = ItemType.Armor, DefenseBonus = 15, Description = "Near-impenetrable.", IsEquippable = true, Tier = ItemTier.Rare },
        new Item { Name = "Ring of Focus", Type = ItemType.Accessory, StatModifier = 0, Description = "+15 MaxMana, -20% ability cooldowns.", IsEquippable = true, MaxManaBonus = 15, Tier = ItemTier.Rare },
        new Item { Name = "Cloak of Shadows", Type = ItemType.Accessory, Description = "+10% dodge chance.", IsEquippable = true, DodgeBonus = 0.10f, Tier = ItemTier.Rare }
    };

    /// <summary>
    /// Populates the shared tier pools from the loaded item catalog so that all
    /// <see cref="LootTable"/> instances pick from the full item set rather than the small
    /// fallback lists. Should be called once from <c>EnemyFactory.Initialize()</c>.
    /// Boss Key must already be excluded from the supplied lists.
    /// </summary>
    public static void SetTierPools(IReadOnlyList<Item> tier1, IReadOnlyList<Item> tier2, IReadOnlyList<Item> tier3,
        IReadOnlyList<Item>? legendary = null)
    {
        _sharedTier1 = tier1;
        _sharedTier2 = tier2;
        _sharedTier3 = tier3;
        _sharedLegendary = legendary ?? Array.Empty<Item>();
    }

    /// <summary>
    /// Picks a random item of the specified tier from the shared tier pools.
    /// Returns <see langword="null"/> if the tier pool is empty.
    /// </summary>
    /// <param name="tier">The desired item tier.</param>
    /// <returns>A randomly selected item, or <see langword="null"/> if the pool is empty.</returns>
    public static Item? RollTier(ItemTier tier)
    {
        IReadOnlyList<Item>? pool = tier switch
        {
            ItemTier.Common    => _sharedTier1 ?? FallbackTier1,
            ItemTier.Uncommon  => _sharedTier2 ?? FallbackTier2,
            ItemTier.Rare      => _sharedTier3 ?? FallbackTier3,
            ItemTier.Legendary => _sharedLegendary ?? FallbackLegendary,
            _                  => _sharedTier2 ?? FallbackTier2
        };
        if (pool.Count == 0) return null;
        return pool[Random.Shared.Next(pool.Count)].Clone();
    }

    /// <summary>
    /// Picks a random <see cref="ItemType.Armor"/> item of the specified tier from the shared
    /// tier pools. Falls back to any item in the tier when no armor items are available.
    /// Returns <see langword="null"/> if the tier pool is entirely empty.
    /// </summary>
    /// <param name="tier">The desired item tier.</param>
    /// <returns>A randomly selected armor item (or any item if no armor exists in that tier), or <see langword="null"/>.</returns>
    public static Item? RollArmorTier(ItemTier tier)
    {
        IReadOnlyList<Item>? fullPool = tier switch
        {
            ItemTier.Common    => _sharedTier1 ?? FallbackTier1,
            ItemTier.Uncommon  => _sharedTier2 ?? FallbackTier2,
            ItemTier.Rare      => _sharedTier3 ?? FallbackTier3,
            ItemTier.Legendary => _sharedLegendary ?? FallbackLegendary,
            _                  => _sharedTier2 ?? FallbackTier2
        };
        if (fullPool.Count == 0) return null;

        var armorPool = fullPool.Where(i => i.Type == ItemType.Armor).ToList();
        var pool = armorPool.Count > 0 ? (IList<Item>)armorPool : (IList<Item>)fullPool.ToList();
        return pool[Random.Shared.Next(pool.Count)].Clone();
    }

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
    /// Bosses (DungeonBoss or subclass) always drop one Legendary item when the Legendary pool is loaded.
    /// Floors 6-8 chest/room loot has a 5% Legendary chance.
    /// </summary>
    /// <param name="enemy">The defeated enemy, used to check <see cref="Enemy.IsElite"/> for tier escalation.</param>
    /// <param name="playerLevel">The player's current level, used to select the appropriate item tier pool.</param>
    /// <param name="isBossRoom">When <see langword="true"/>, guarantees a Legendary drop from the boss pool.</param>
    /// <param name="dungeoonFloor">Current dungeon floor (1-8); floors 6-8 have a 5% Legendary chance.</param>
    /// <returns>A <see cref="LootResult"/> containing the optional item drop and the gold amount.</returns>
    public LootResult RollDrop(Enemy enemy, int playerLevel = 1, bool isBossRoom = false, int dungeoonFloor = 1)
    {
        int gold = _minGold == _maxGold ? _minGold : _rng.Next(_minGold, _maxGold + 1);

        Item? dropped = null;

        // Check configured drops first (boss key, etc.)
        foreach (var (item, chance) in _drops)
        {
            if (_rng.NextDouble() < chance) { dropped = item.Clone(); break; }
        }

        // Boss rooms: guaranteed Legendary drop (overrides tiered roll if Legendary pool available)
        var legendaryPool = _sharedLegendary ?? FallbackLegendary;
        if (dropped == null && isBossRoom && legendaryPool.Count > 0)
        {
            dropped = legendaryPool[_rng.Next(legendaryPool.Count)].Clone();
        }

        // Floors 6-8: 5% Legendary chance
        if (dropped == null && dungeoonFloor >= 6 && legendaryPool.Count > 0 && _rng.NextDouble() < 0.05)
        {
            dropped = legendaryPool[_rng.Next(legendaryPool.Count)].Clone();
        }

        // 30% chance of a tiered item drop if none already rolled
        if (dropped == null && _rng.NextDouble() < 0.30)
        {
            var pool = playerLevel >= 7 ? (_sharedTier3 ?? FallbackTier3)
                     : playerLevel >= 4 ? (_sharedTier2 ?? FallbackTier2)
                     : (_sharedTier1 ?? FallbackTier1);

            // Elite enemies guarantee tier-2+ drop
            if (enemy?.IsElite == true && playerLevel < 4) pool = _sharedTier2 ?? FallbackTier2;

            dropped = pool[_rng.Next(pool.Count)].Clone();
        }

        return new LootResult { Item = dropped, Gold = gold };
    }
}
