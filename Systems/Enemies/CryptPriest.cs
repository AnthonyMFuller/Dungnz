namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// An undead healer-type enemy that self-heals 10 HP every 2 turns.
/// </summary>
public class CryptPriest : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private CryptPriest() { }

    /// <summary>Creates a Crypt Priest with optional data-driven stats.</summary>
    public CryptPriest(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Crypt Priest";
            HP = MaxHP = 52;
            Attack = 16;
            Defense = 8;
            XPValue = 45;
            IsUndead = true;
            LootTable = new LootTable(minGold: 12, maxGold: 26);
        }

        SelfHealAmount = 10;
        SelfHealEveryTurns = 2;
        SelfHealCooldown = 1; // first heal fires on turn 2 (decrement-first: 1→0=fire)

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var soul = itemConfig?.FirstOrDefault(i => i.Name == "SoulGem");
        if (soul != null) LootTable.AddDrop(ItemConfig.CreateItem(soul), 0.15);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Large Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.25);
    }
}
