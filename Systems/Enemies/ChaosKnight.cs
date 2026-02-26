namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A chaotic warrior with a 20% critical hit chance on every attack.
/// Completely immune to Stun; attempts to stun it display a flavour message.
/// </summary>
public class ChaosKnight : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private ChaosKnight() { }

    /// <summary>Creates a Chaos Knight with optional data-driven stats.</summary>
    public ChaosKnight(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Chaos Knight";
            HP = MaxHP = 85;
            Attack = 24;
            Defense = 16;
            XPValue = 80;
            LootTable = new LootTable(minGold: 35, maxGold: 60);
        }

        EnemyCritChance = 0.20f;
        IsStunImmune = true;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var sword = itemConfig?.FirstOrDefault(i => i.Name == "Mythril Blade");
        if (sword != null) LootTable.AddDrop(ItemConfig.CreateItem(sword), 0.20);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Large Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.25);
    }
}
