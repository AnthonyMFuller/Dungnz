namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A heavily armoured warrior. 30% chance to counter any player attack
/// for 50% of the damage dealt.
/// </summary>
public class IronGuard : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private IronGuard() { }

    /// <summary>Creates an Iron Guard with optional data-driven stats.</summary>
    public IronGuard(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Iron Guard";
            HP = MaxHP = 50;
            Attack = 18;
            Defense = 14;
            XPValue = 48;
            LootTable = new LootTable(minGold: 15, maxGold: 32);
        }

        CounterStrikeChance = 0.30f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var armor = itemConfig?.FirstOrDefault(i => i.Name == "Chain Mail");
        if (armor != null) LootTable.AddDrop(ItemConfig.CreateItem(armor), 0.25);
        var sword = itemConfig?.FirstOrDefault(i => i.Name == "Iron Sword");
        if (sword != null) LootTable.AddDrop(ItemConfig.CreateItem(sword), 0.20);
    }
}
