namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// An undead warrior enemy with moderate stats. May drop a Rusty Sword or Bone Fragment
/// on defeat, offering the player a chance at an early weapon or minor heal.
/// </summary>
public class Skeleton : Enemy
{
    /// <summary>
    /// Initialises the Skeleton using either the provided external stats from config
    /// or built-in fallback defaults, and populates the loot table from item config when available.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (30 HP, 12 ATK, 5 DEF, 25 XP, 5–15 gold).
    /// </param>
    /// <param name="itemConfig">
    /// The loaded item configuration used to source Rusty Sword and Bone Fragment drops,
    /// or <see langword="null"/> to create fallback inline items.
    /// </param>
    public Skeleton(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Skeleton";
            HP = MaxHP = 30;
            Attack = 12;
            Defense = 5;
            XPValue = 25;
            LootTable = new LootTable(minGold: 5, maxGold: 15);
        }
        
        // Add item drops (use config if available)
        if (itemConfig != null)
        {
            var rustySword = itemConfig.FirstOrDefault(i => i.Name == "Rusty Sword");
            if (rustySword != null)
            {
                LootTable.AddDrop(ItemConfig.CreateItem(rustySword), 0.3);
            }
            
            var boneFragment = itemConfig.FirstOrDefault(i => i.Name == "Bone Fragment");
            if (boneFragment != null)
            {
                LootTable.AddDrop(ItemConfig.CreateItem(boneFragment), 0.5);
            }
        }
        else
        {
            LootTable.AddDrop(new Item 
            { 
                Name = "Rusty Sword", 
                Type = ItemType.Weapon, 
                AttackBonus = 3, 
                Description = "A corroded blade." 
            }, 0.3);
            LootTable.AddDrop(new Item 
            { 
                Name = "Bone Fragment", 
                Type = ItemType.Consumable, 
                HealAmount = 5, 
                Description = "Shards from the dead." 
            }, 0.5);
        }
    }
}
