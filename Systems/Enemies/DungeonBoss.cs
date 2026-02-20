namespace Dungnz.Systems.Enemies;
using Dungnz.Models;

public class DungeonBoss : Enemy
{
    public DungeonBoss()
    {
        Name = "Dungeon Boss";
        HP = MaxHP = 100;
        Attack = 22;
        Defense = 15;
        XPValue = 100;
        LootTable = new LootTable(minGold: 50, maxGold: 100);
        LootTable.AddDrop(new Item 
        { 
            Name = "Boss Key", 
            Type = ItemType.Consumable, 
            Description = "Proof of your victory.", 
            StatModifier = 0 
        }, 1.0);
    }
}
