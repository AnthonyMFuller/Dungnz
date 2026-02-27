namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A warrior specialised in breaking defenses. When the player's DEF exceeds 15,
/// ignores 50% of their DEF when calculating damage.
/// </summary>
public class ShieldBreaker : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private ShieldBreaker() { }

    /// <summary>Creates a Shield Breaker with optional data-driven stats.</summary>
    public ShieldBreaker(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Shield Breaker";
            HP = MaxHP = 55;
            Attack = 21;
            Defense = 8;
            XPValue = 50;
            LootTable = new LootTable(minGold: 14, maxGold: 28);
        }

        ShieldBreakerDefThreshold = 15;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var armor = itemConfig?.FirstOrDefault(i => i.Name == "Chain Mail");
        if (armor != null) LootTable.AddDrop(ItemConfig.CreateItem(armor), 0.20);
        var sword = itemConfig?.FirstOrDefault(i => i.Name == "Iron Sword");
        if (sword != null) LootTable.AddDrop(ItemConfig.CreateItem(sword), 0.20);
    }
}
