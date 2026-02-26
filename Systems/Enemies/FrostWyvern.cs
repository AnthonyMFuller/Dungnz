namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// An ice-breathing wyvern. Every 3rd attack is a Frost Breath:
/// ignores player DEF and applies Slow (2 turns).
/// </summary>
public class FrostWyvern : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private FrostWyvern() { }

    /// <summary>Creates a Frost Wyvern with optional data-driven stats.</summary>
    public FrostWyvern(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Frost Wyvern";
            HP = MaxHP = 75;
            Attack = 22;
            Defense = 12;
            XPValue = 70;
            LootTable = new LootTable(minGold: 25, maxGold: 50);
        }

        FrostBreathEvery = 3;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var fang = itemConfig?.FirstOrDefault(i => i.Name == "WyvernFang");
        if (fang != null) LootTable.AddDrop(ItemConfig.CreateItem(fang), 0.50);
        var armor = itemConfig?.FirstOrDefault(i => i.Name == "FrostScaleArmor");
        if (armor != null) LootTable.AddDrop(ItemConfig.CreateItem(armor), 0.15);
    }
}
