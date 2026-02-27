namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A whirling swordsman. When the player successfully dodges, 50% chance of a
/// full-ATK counter-attack.
/// </summary>
public class BladeDancer : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private BladeDancer() { }

    /// <summary>Creates a Blade Dancer with optional data-driven stats.</summary>
    public BladeDancer(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Blade Dancer";
            HP = MaxHP = 50;
            Attack = 24;
            Defense = 7;
            XPValue = 46;
            LootTable = new LootTable(minGold: 14, maxGold: 28);
        }

        OnDodgeCounterChance = 0.50f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var sword = itemConfig?.FirstOrDefault(i => i.Name == "Mythril Blade");
        if (sword != null) LootTable.AddDrop(ItemConfig.CreateItem(sword), 0.15);
        var cloak = itemConfig?.FirstOrDefault(i => i.Name == "Cloak of Shadows");
        if (cloak != null) LootTable.AddDrop(ItemConfig.CreateItem(cloak), 0.15);
    }
}
