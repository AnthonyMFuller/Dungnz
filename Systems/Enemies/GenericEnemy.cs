namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A concrete enemy constructed entirely from data-driven <see cref="EnemyStats"/>,
/// used for enemy types that do not have a dedicated subclass.
/// </summary>
public class GenericEnemy : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private GenericEnemy() { }

    /// <summary>Initialises a GenericEnemy from the provided stats, or falls back to bare defaults.</summary>
    public GenericEnemy(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        if (stats != null)
        {
            Name = stats.Name;
            HP = MaxHP = stats.MaxHP;
            Attack = stats.Attack;
            Defense = stats.Defense;
            XPValue = stats.XPValue;
            IsUndead = stats.IsUndead;
            LootTable = new LootTable(minGold: stats.MinGold, maxGold: stats.MaxGold);
            AsciiArt = stats.AsciiArt;
        }
        else
        {
            Name = "Unknown Enemy";
            HP = MaxHP = 20;
            Attack = 8;
            Defense = 2;
            XPValue = 10;
            LootTable = new LootTable(minGold: 1, maxGold: 5);
        }
    }
}
