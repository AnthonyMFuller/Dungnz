namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A leech that drains 8 mana per hit. When player mana reaches 0,
/// gains +25% ATK.
/// </summary>
public class ManaLeech : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private ManaLeech() { }

    /// <summary>Creates a Mana Leech with optional data-driven stats.</summary>
    public ManaLeech(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Mana Leech";
            HP = MaxHP = 42;
            Attack = 14;
            Defense = 5;
            XPValue = 38;
            LootTable = new LootTable(minGold: 10, maxGold: 22);
        }

        ManaDrainPerHit = 8;
        ZeroManaAtkBonus = 0.25f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var gem = itemConfig?.FirstOrDefault(i => i.Name == "Ring of Focus");
        if (gem != null) LootTable.AddDrop(ItemConfig.CreateItem(gem), 0.10);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.20);
    }
}
