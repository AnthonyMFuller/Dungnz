namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A large, slow, heavily armoured creature with high HP and defense. Has a chance to
/// drop Troll Hide armour, rewarding players who can overcome its resilience.
/// </summary>
public class Troll : Enemy
{
    /// <summary>
    /// Initialises the Troll using either the provided external stats from config
    /// or built-in fallback defaults, and populates the loot table from item config when available.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (60 HP, 10 ATK, 8 DEF, 40 XP, 10–25 gold).
    /// </param>
    /// <param name="itemConfig">
    /// The loaded item configuration used to source the Troll Hide drop,
    /// or <see langword="null"/> to create a fallback inline item.
    /// </param>
    [System.Text.Json.Serialization.JsonConstructor]
    private Troll() { }

    public Troll(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        if (stats != null)
        {
            Name = stats.Name;
            HP = MaxHP = stats.MaxHP;
            Attack = stats.Attack;
            Defense = stats.Defense;
            XPValue = stats.XPValue;
            LootTable = new LootTable(minGold: stats.MinGold, maxGold: stats.MaxGold);
        }
        else
        {
            // Fallback defaults if config not loaded
            Name = "Troll";
            HP = MaxHP = 60;
            Attack = 10;
            Defense = 8;
            XPValue = 40;
            LootTable = new LootTable(minGold: 10, maxGold: 25);
        }
        
        // Add item drops (use config if available)
        if (itemConfig != null)
        {
            var trollHide = itemConfig.FirstOrDefault(i => i.Name == "Troll Hide");
            if (trollHide != null)
            {
                LootTable.AddDrop(ItemConfig.CreateItem(trollHide), 0.4);
            }
        }
        else
        {
            LootTable.AddDrop(new Item 
            { 
                Name = "Troll Hide", 
                Type = ItemType.Armor, 
                DefenseBonus = 4, 
                Description = "Thick, resilient leather." 
            }, 0.4);
        }
    }
}
