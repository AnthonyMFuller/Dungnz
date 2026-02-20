namespace Dungnz.Systems.Enemies;
using Dungnz.Models;

public class DarkKnight : Enemy
{
    public DarkKnight()
    {
        Name = "Dark Knight";
        HP = MaxHP = 45;
        Attack = 18;
        Defense = 12;
        XPValue = 55;
        LootTable = new LootTable(minGold: 20, maxGold: 40);
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
