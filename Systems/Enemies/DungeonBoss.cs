namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class DungeonBoss : Enemy
{
    public DungeonBoss(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Dungeon Boss";
            HP = MaxHP = 100;
            Attack = 22;
            Defense = 15;
            XPValue = 100;
            LootTable = new LootTable(minGold: 50, maxGold: 100);
        }
        
        // Add item drops (use config if available)
        if (itemConfig != null)
        {
            var bossKey = itemConfig.FirstOrDefault(i => i.Name == "Boss Key");
            if (bossKey != null)
            {
                LootTable.AddDrop(ItemConfig.CreateItem(bossKey), 1.0);
            }
        }
        else
        {
            LootTable.AddDrop(new Item 
            { 
                Name = "Boss Key", 
                Type = ItemType.Consumable, 
                Description = "Proof of your victory.", 
                StatModifier = 0 
            }, 1.0);
        }
    }
}
