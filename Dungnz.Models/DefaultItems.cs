namespace Dungnz.Models;

using Dungnz.Models;

/// <summary>
/// Centralised fallback item definitions used when JSON-based item catalogs are unavailable.
/// Referenced by <see cref="LootTable"/> and <see cref="Merchant"/> so that hardcoded data
/// lives in the Data layer rather than in Models.
/// </summary>
public static class DefaultItems
{
    /// <summary>Fallback tier-1 (Common) items for loot rolls.</summary>
    public static readonly IReadOnlyList<Item> FallbackTier1 = new List<Item>
    {
        new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, Description = "A basic blade.", IsEquippable = true, Tier = ItemTier.Common },
        new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 5, Description = "Light protection.", IsEquippable = true, Tier = ItemTier.Common }
    };

    /// <summary>Fallback tier-2 (Uncommon) items for loot rolls.</summary>
    public static readonly IReadOnlyList<Item> FallbackTier2 = new List<Item>
    {
        new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Description = "A quality weapon.", IsEquippable = true, Tier = ItemTier.Uncommon },
        new Item { Name = "Chain Mail", Type = ItemType.Armor, DefenseBonus = 10, Description = "Solid protection.", IsEquippable = true, Tier = ItemTier.Uncommon },
        new Item { Name = "Sword of Flames", Type = ItemType.Weapon, AttackBonus = 5, Description = "Burns with inner fire. Applies Bleed on hit.", IsEquippable = true, AppliesBleedOnHit = true, Tier = ItemTier.Uncommon },
        new Item { Name = "Armor of the Turtle", Type = ItemType.Armor, DefenseBonus = 15, Description = "Heavy shell. Grants Poison immunity.", IsEquippable = true, PoisonImmunity = true, Tier = ItemTier.Uncommon }
    };

    /// <summary>Fallback tier-3 (Rare) items for loot rolls.</summary>
    public static readonly IReadOnlyList<Item> FallbackTier3 = new List<Item>
    {
        new Item { Name = "Mythril Blade", Type = ItemType.Weapon, AttackBonus = 8, Description = "Razor-sharp alloy.", IsEquippable = true, Tier = ItemTier.Rare },
        new Item { Name = "Plate Armor", Type = ItemType.Armor, DefenseBonus = 15, Description = "Near-impenetrable.", IsEquippable = true, Tier = ItemTier.Rare },
        new Item { Name = "Ring of Focus", Type = ItemType.Accessory, StatModifier = 0, Description = "+15 MaxMana, -20% ability cooldowns.", IsEquippable = true, MaxManaBonus = 15, Tier = ItemTier.Rare },
        new Item { Name = "Cloak of Shadows", Type = ItemType.Accessory, Description = "+10% dodge chance.", IsEquippable = true, DodgeBonus = 0.10f, Tier = ItemTier.Rare }
    };

    /// <summary>Fallback Epic items (empty — no hardcoded epic items).</summary>
    public static readonly IReadOnlyList<Item> FallbackEpic = new List<Item>();

    /// <summary>Fallback Legendary items (empty — no hardcoded legendary items).</summary>
    public static readonly IReadOnlyList<Item> FallbackLegendary = new List<Item>();

    /// <summary>Fallback merchant stock used when merchant-inventory.json cannot be loaded.</summary>
    public static List<MerchantItem> GetFallbackMerchantStock(DifficultySettings? difficulty = null)
    {
        var multiplier = difficulty?.MerchantPriceMultiplier ?? 1.0f;
        return
        [
            new() { Item = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20, Description = "Restores 20 HP.", Tier = ItemTier.Common }, Price = Math.Max(1, (int)(25 * multiplier)) },
            new() { Item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true, Description = "A sturdy iron blade.", Tier = ItemTier.Common }, Price = Math.Max(1, (int)(50 * multiplier)) },
            new() { Item = new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 3, IsEquippable = true, Description = "Basic leather protection.", Tier = ItemTier.Common }, Price = Math.Max(1, (int)(40 * multiplier)) },
        ];
    }
}
