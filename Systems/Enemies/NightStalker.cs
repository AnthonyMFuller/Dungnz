namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// An ambush predator. First attack in combat deals 1.5× damage.
/// Has a flat 15% dodge chance.
/// </summary>
public class NightStalker : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private NightStalker() { }

    /// <summary>Creates a Night Stalker with optional data-driven stats.</summary>
    public NightStalker(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Night Stalker";
            HP = MaxHP = 55;
            Attack = 20;
            Defense = 8;
            XPValue = 58;
            LootTable = new LootTable(minGold: 18, maxGold: 38);
        }

        FirstAttackMultiplier = 1.5f;
        FlatDodgeChance = 0.15f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var cloak = itemConfig?.FirstOrDefault(i => i.Name == "Cloak of Shadows");
        if (cloak != null) LootTable.AddDrop(ItemConfig.CreateItem(cloak), 0.20);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.30);
    }
}
