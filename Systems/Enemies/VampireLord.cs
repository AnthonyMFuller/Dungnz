namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A powerful undead noble that heals itself for 50% of every hit's damage, sustaining
/// itself through combat. Its lifesteal makes sustained fights increasingly dangerous.
/// </summary>
public class VampireLord : Enemy
{
    /// <summary>
    /// Initialises the Vampire Lord using either the provided external stats from config
    /// or built-in fallback defaults. Sets <see cref="Enemy.LifestealPercent"/> to 50%
    /// so the Vampire Lord restores HP equal to half the damage it deals each attack.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (80 HP, 16 ATK, 12 DEF, 60 XP, 15–30 gold).
    /// </param>
    /// <param name="itemConfig">Item configuration reserved for future loot table expansion; currently unused.</param>
    [System.Text.Json.Serialization.JsonConstructor]
    private VampireLord() { }

    /// <summary>Initialises a Vampire Lord with the given stats and item configuration, or falls back to hard-coded defaults. Sets 50% lifesteal.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration; currently unused for this enemy.</param>
    public VampireLord(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        LifestealPercent = 0.50f; // heals 50% of damage dealt
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
            Name = "Vampire Lord";
            HP = MaxHP = 80;
            Attack = 16;
            Defense = 12;
            XPValue = 60;
            LootTable = new LootTable(minGold: 15, maxGold: 30);
        }
    }
}
