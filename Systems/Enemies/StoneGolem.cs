namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A magically animated stone construct with extremely high defense and HP. Completely
/// immune to all status effects, making it resistant to poison, stun, and other debuffs.
/// </summary>
public class StoneGolem : Enemy
{
    /// <summary>
    /// Initialises the Stone Golem using either the provided external stats from config
    /// or built-in fallback defaults. Sets <see cref="Enemy.IsImmuneToEffects"/> to prevent
    /// any status effects from being applied.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (90 HP, 8 ATK, 20 DEF, 50 XP, 10–25 gold).
    /// </param>
    /// <param name="itemConfig">Item configuration reserved for future loot table expansion; currently unused.</param>
    [System.Text.Json.Serialization.JsonConstructor]
    private StoneGolem() { }

    public StoneGolem(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        IsImmuneToEffects = true;
        if (stats != null)
        {
            Name = stats.Name;
            HP = MaxHP = stats.MaxHP;
            Attack = stats.Attack;
            Defense = stats.Defense;
            XPValue = stats.XPValue;
            LootTable = new LootTable(minGold: stats.MinGold, maxGold: stats.MaxGold);
        }
        else
        {
            Name = "Stone Golem";
            HP = MaxHP = 90;
            Attack = 8;
            Defense = 20;
            XPValue = 50;
            LootTable = new LootTable(minGold: 10, maxGold: 25);
        }
    }
}
