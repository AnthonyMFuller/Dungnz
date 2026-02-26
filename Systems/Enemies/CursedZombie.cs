namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A rotting, cursed corpse. On death it curses the player with Weakened (3 turns).
/// </summary>
public class CursedZombie : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private CursedZombie() { }

    /// <summary>Creates a Cursed Zombie with optional data-driven stats.</summary>
    public CursedZombie(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Cursed Zombie";
            HP = MaxHP = 32;
            Attack = 9;
            Defense = 6;
            XPValue = 28;
            IsUndead = true;
            LootTable = new LootTable(minGold: 4, maxGold: 12);
        }

        OnDeathEffect = StatusEffect.Weakened;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var pelt = itemConfig?.FirstOrDefault(i => i.Name == "Bone Fragment");
        if (pelt != null) LootTable.AddDrop(ItemConfig.CreateItem(pelt), 0.40);
        var vial = itemConfig?.FirstOrDefault(i => i.Name == "BloodVial");
        if (vial != null) LootTable.AddDrop(ItemConfig.CreateItem(vial), 0.25);
    }
}
