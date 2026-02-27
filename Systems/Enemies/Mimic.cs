namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A treasure chest that has come to life and attacks when the player attempts to loot it.
/// Uses an ambush mechanic that prevents the player from acting on the first turn of combat.
/// </summary>
public class Mimic : Enemy
{
    private readonly Random _rng;

    /// <summary>
    /// Initialises the Mimic using either the provided external stats from config
    /// or built-in fallback defaults. Sets <see cref="Enemy.IsAmbush"/> to <see langword="true"/>
    /// so the player cannot act during the first turn of the encounter.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (40 HP, 14 ATK, 8 DEF, 40 XP, 10–25 gold).
    /// </param>
    /// <param name="itemConfig">Item configuration reserved for future loot table expansion; currently unused.</param>
    [System.Text.Json.Serialization.JsonConstructor]
    private Mimic() { _rng = new Random(); } // RNG-ok: JsonConstructor (deserialization only)

    /// <summary>Initialises a Mimic with the given stats and item configuration, or falls back to hard-coded defaults. Enables the ambush mechanic.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration; currently unused for this enemy.</param>
    /// <param name="rng">Random number generator; if null a new instance is created.</param>
    public Mimic(EnemyStats? stats = null, List<ItemStats>? itemConfig = null, Random? rng = null)
    {
        _rng = rng ?? new Random();
        IsAmbush = true; // first-turn surprise: player cannot act
        if (stats != null)
        {
            Name = stats.Name;
            HP = MaxHP = stats.MaxHP;
            Attack = stats.Attack;
            Defense = stats.Defense;
            XPValue = stats.XPValue;
            LootTable = new LootTable(minGold: stats.MinGold, maxGold: stats.MaxGold);
            AsciiArt = stats.AsciiArt;
        }
        else
        {
            Name = "Mimic";
            HP = MaxHP = 40;
            Attack = 14;
            Defense = 8;
            XPValue = 40;
            LootTable = new LootTable(minGold: 10, maxGold: 25);
        }
        // Mimic always drops a Rare item — reward for surviving the ambush
        if (itemConfig != null)
        {
            var rareItems = itemConfig.Where(i => i.Tier.Equals("Rare", StringComparison.OrdinalIgnoreCase)
                                                  && i.Name != "Boss Key").ToList();
            if (rareItems.Count > 0)
            {
                var pick = rareItems[_rng.Next(rareItems.Count)];
                LootTable.AddDrop(ItemConfig.CreateItem(pick), 1.0);
            }
        }
        else
        {
            LootTable.AddDrop(new Item
            {
                Name = "Phoenix Feather",
                Type = ItemType.Consumable,
                HealAmount = 60,
                Description = "Channels the fire of rebirth into your wounds.",
                Tier = ItemTier.Rare
            }, 1.0);
        }
    }
}
