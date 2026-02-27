namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A massive ogre with thick hide. The first 3 hits it receives are reduced by 5 damage each.
/// </summary>
public class SiegeOgre : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private SiegeOgre() { }

    /// <summary>Creates a Siege Ogre with optional data-driven stats.</summary>
    public SiegeOgre(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Siege Ogre";
            HP = MaxHP = 65;
            Attack = 23;
            Defense = 10;
            XPValue = 58;
            LootTable = new LootTable(minGold: 16, maxGold: 32);
        }

        ThickHideHitsRemaining = 3;
        ThickHideDamageReduction = 5;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var armor = itemConfig?.FirstOrDefault(i => i.Name == "Plate Armor");
        if (armor != null) LootTable.AddDrop(ItemConfig.CreateItem(armor), 0.20);
        var pelt = itemConfig?.FirstOrDefault(i => i.Name == "Troll Hide");
        if (pelt != null) LootTable.AddDrop(ItemConfig.CreateItem(pelt), 0.25);
    }
}
