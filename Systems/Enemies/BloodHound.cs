namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A savage hound that applies Bleed on each hit (40% chance, 2 turns).
/// </summary>
public class BloodHound : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private BloodHound() { }

    /// <summary>Creates a Blood Hound with optional data-driven stats.</summary>
    public BloodHound(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Blood Hound";
            HP = MaxHP = 42;
            Attack = 16;
            Defense = 5;
            XPValue = 38;
            LootTable = new LootTable(minGold: 10, maxGold: 22);
        }

        BleedOnHitChance = 0.40f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var vial = itemConfig?.FirstOrDefault(i => i.Name == "BloodVial");
        if (vial != null) LootTable.AddDrop(ItemConfig.CreateItem(vial), 0.50);
        var pelt = itemConfig?.FirstOrDefault(i => i.Name == "Troll Hide");
        if (pelt != null) LootTable.AddDrop(ItemConfig.CreateItem(pelt), 0.20);
    }
}
