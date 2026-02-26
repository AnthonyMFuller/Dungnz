namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A small chaos demon. When surrounded by others (simulated as 33% chance per hit),
/// reduces incoming damage by 3.
/// </summary>
public class ShadowImp : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private ShadowImp() { }

    /// <summary>Creates a Shadow Imp with optional data-driven stats.</summary>
    public ShadowImp(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Shadow Imp";
            HP = MaxHP = 22;
            Attack = 10;
            Defense = 3;
            XPValue = 18;
            LootTable = new LootTable(minGold: 3, maxGold: 10);
        }

        GroupDamageReduction = 3;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.25);
    }
}
