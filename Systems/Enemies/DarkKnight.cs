namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A powerful armoured warrior with high attack and defense. May drop a Dark Blade weapon
/// or Knight's Armor, making it a high-risk, high-reward mid-game encounter.
/// </summary>
public class DarkKnight : Enemy
{
    /// <summary>
    /// Initialises the Dark Knight using either the provided external stats from config
    /// or built-in fallback defaults, and populates the loot table from item config when available.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (45 HP, 18 ATK, 12 DEF, 55 XP, 20–40 gold).
    /// </param>
    /// <param name="itemConfig">
    /// The loaded item configuration used to source Dark Blade and Knight's Armor drops,
    /// or <see langword="null"/> to create fallback inline items.
    /// </param>
    [System.Text.Json.Serialization.JsonConstructor]
    private DarkKnight() { }

    /// <summary>Initialises a Dark Knight with the given stats and item configuration, or falls back to hard-coded defaults.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration used to source loot drops, or <see langword="null"/> to use inline fallbacks.</param>
    public DarkKnight(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Dark Knight";
            HP = MaxHP = 45;
            Attack = 18;
            Defense = 12;
            XPValue = 55;
            LootTable = new LootTable(minGold: 20, maxGold: 40);
        }
        
        // Add item drops (use config if available)
        if (itemConfig != null)
        {
            var darkBlade = itemConfig.FirstOrDefault(i => i.Name == "Dark Blade");
            if (darkBlade != null)
            {
                LootTable.AddDrop(ItemConfig.CreateItem(darkBlade), 0.5);
            }
            
            var knightArmor = itemConfig.FirstOrDefault(i => i.Name == "Knight's Armor");
            if (knightArmor != null)
            {
                LootTable.AddDrop(ItemConfig.CreateItem(knightArmor), 0.4);
            }
        }
        else
        {
            LootTable.AddDrop(new Item 
            { 
                Name = "Dark Blade", 
                Type = ItemType.Weapon, 
                AttackBonus = 5, 
                Description = "A blade forged in shadow." 
            }, 0.5);
            LootTable.AddDrop(new Item 
            { 
                Name = "Knight's Armor", 
                Type = ItemType.Armor, 
                DefenseBonus = 6, 
                Description = "Heavy plated armor." 
            }, 0.4);
        }
    }
}
