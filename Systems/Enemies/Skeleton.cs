namespace Dungnz.Systems.Enemies;
using Dungnz.Models;

public class Skeleton : Enemy
{
    public Skeleton()
    {
        Name = "Skeleton";
        HP = MaxHP = 30;
        Attack = 12;
        Defense = 5;
        XPValue = 25;
        LootTable = new LootTable(minGold: 5, maxGold: 15);
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
