namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class Troll : Enemy
{
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
