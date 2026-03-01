namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A weak, fast-moving dungeon creature with low HP and defense but respectable
/// damage for an early-game enemy. Provides a small amount of gold on defeat.
/// </summary>
public class Goblin : Enemy
{
    /// <summary>
    /// Initialises the Goblin using either the provided external stats from config
    /// or built-in fallback defaults.
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    private Goblin() { }

    /// <summary>Initialises a Goblin with the given stats and item configuration, or falls back to hard-coded defaults.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration for loot tables; currently unused for this enemy.</param>
    public Goblin(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
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
            // Fallback defaults if config not loaded
            Name = "Goblin";
            HP = MaxHP = 20;
            Attack = 8;
            Defense = 2;
            XPValue = 15;
            LootTable = new LootTable(minGold: 2, maxGold: 8);
        }
    }
}
