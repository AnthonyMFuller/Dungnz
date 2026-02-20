namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class DarkKnight : Enemy
{
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
