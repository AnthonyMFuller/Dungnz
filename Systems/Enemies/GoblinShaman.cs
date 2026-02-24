namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A magic-wielding goblin that inflicts poison on every successful hit, dealing additional
/// damage over time. Its spellcraft makes it more dangerous than its modest HP suggests.
/// </summary>
public class GoblinShaman : Enemy
{
    /// <summary>
    /// Initialises the Goblin Shaman using either the provided external stats from config
    /// or built-in fallback defaults. Sets <see cref="Enemy.AppliesPoisonOnHit"/> to enable
    /// per-hit poison application during combat.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (25 HP, 10 ATK, 4 DEF, 25 XP, 5–15 gold).
    /// </param>
    /// <param name="itemConfig">Item configuration reserved for future loot table expansion; currently unused.</param>
    [System.Text.Json.Serialization.JsonConstructor]
    private GoblinShaman() { }

    /// <summary>Initialises a Goblin Shaman with the given stats and item configuration, or falls back to hard-coded defaults. Enables poison-on-hit.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration; currently unused for this enemy.</param>
    public GoblinShaman(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        AppliesPoisonOnHit = true;
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
            Name = "Goblin Shaman";
            HP = MaxHP = 25;
            Attack = 10;
            Defense = 4;
            XPValue = 25;
            LootTable = new LootTable(minGold: 5, maxGold: 15);
        }

        if (itemConfig != null)
        {
            var antidote = itemConfig.FirstOrDefault(i => i.Name == "Antidote");
            if (antidote != null)
                LootTable.AddDrop(ItemConfig.CreateItem(antidote), 0.4);
        }
        else
        {
            LootTable.AddDrop(new Item
            {
                Name = "Antidote",
                Type = ItemType.Consumable,
                HealAmount = 8,
                Description = "A bitter green liquid that neutralises toxins.",
                Tier = ItemTier.Common
            }, 0.4);
        }
    }
}
