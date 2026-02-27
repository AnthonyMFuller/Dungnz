namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A robed spellcaster. 25% chance per attack to apply Weakened to the player
/// instead of dealing damage.
/// </summary>
public class DarkSorcerer : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private DarkSorcerer() { }

    /// <summary>Creates a Dark Sorcerer with optional data-driven stats.</summary>
    public DarkSorcerer(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Dark Sorcerer";
            HP = MaxHP = 45;
            Attack = 18;
            Defense = 6;
            XPValue = 40;
            LootTable = new LootTable(minGold: 10, maxGold: 22);
        }

        WeakenOnAttackChance = 0.25f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var staff = itemConfig?.FirstOrDefault(i => i.Name == "Staff of Domination") ?? itemConfig?.FirstOrDefault(i => i.Name == "StaffOfDomination");
        if (staff != null) LootTable.AddDrop(ItemConfig.CreateItem(staff), 0.10);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Large Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.20);
    }
}
