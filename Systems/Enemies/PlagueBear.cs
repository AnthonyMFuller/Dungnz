namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A plague-infested bear. Poisons the player at combat start (3 turns).
/// On death: 40% chance to reapply Poison (3 turns).
/// </summary>
public class PlagueBear : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private PlagueBear() { }

    /// <summary>Creates a Plague Bear with optional data-driven stats.</summary>
    public PlagueBear(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Plague Bear";
            HP = MaxHP = 48;
            Attack = 19;
            Defense = 7;
            XPValue = 44;
            LootTable = new LootTable(minGold: 12, maxGold: 26);
        }

        PoisonOnCombatStart = true;
        PoisonOnDeathChance = 0.40f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var pelt = itemConfig?.FirstOrDefault(i => i.Name == "Troll Hide");
        if (pelt != null) LootTable.AddDrop(ItemConfig.CreateItem(pelt), 0.25);
        var vial = itemConfig?.FirstOrDefault(i => i.Name == "BloodVial");
        if (vial != null) LootTable.AddDrop(ItemConfig.CreateItem(vial), 0.30);
    }
}
