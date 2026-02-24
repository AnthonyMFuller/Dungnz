namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// An incorporeal undead spirit with a 30% flat dodge chance that bypasses the normal
/// defense-based dodge formula, making it difficult to reliably damage despite low base defense.
/// </summary>
public class Wraith : Enemy
{
    /// <summary>
    /// Initialises the Wraith using either the provided external stats from config
    /// or built-in fallback defaults. Sets <see cref="Enemy.FlatDodgeChance"/> to 30%
    /// to represent its ethereal, hard-to-hit nature.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (35 HP, 18 ATK, 2 DEF, 35 XP, 8–20 gold).
    /// </param>
    /// <param name="itemConfig">Item configuration reserved for future loot table expansion; currently unused.</param>
    [System.Text.Json.Serialization.JsonConstructor]
    private Wraith() { }

    /// <summary>Initialises a Wraith with the given stats and item configuration, or falls back to hard-coded defaults. Sets 30% flat dodge chance.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration; currently unused for this enemy.</param>
    public Wraith(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        FlatDodgeChance = 0.30f; // 30% flat dodge, ignores DEF-based formula
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
            Name = "Wraith";
            HP = MaxHP = 35;
            Attack = 18;
            Defense = 2;
            XPValue = 35;
            LootTable = new LootTable(minGold: 8, maxGold: 20);
        }
        if (itemConfig != null)
        {
            var shadowEssence = itemConfig.FirstOrDefault(i => i.Name == "Shadow Essence");
            if (shadowEssence != null)
                LootTable.AddDrop(ItemConfig.CreateItem(shadowEssence), 0.20);
        }
        else
        {
            LootTable.AddDrop(new Item
            {
                Name = "Shadow Essence",
                Type = ItemType.Consumable,
                HealAmount = 20,
                Description = "Distilled wraith energy. Cold to the touch but restorative.",
                Tier = ItemTier.Rare
            }, 0.20);
        }
    }
}
