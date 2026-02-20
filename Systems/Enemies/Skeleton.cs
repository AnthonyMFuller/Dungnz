namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class Skeleton : Enemy
{
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
