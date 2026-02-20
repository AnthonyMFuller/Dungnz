namespace TextGame.Systems.Enemies;
using TextGame.Models;

public class Troll : Enemy
{
    public Troll()
    {
        Name = "Troll";
        HP = MaxHP = 60;
        Attack = 10;
        Defense = 8;
        XPValue = 40;
        LootTable = new LootTable(minGold: 10, maxGold: 25);
        LootTable.AddDrop(new Item 
        { 
            Name = "Troll Hide", 
            Type = ItemType.Armor, 
            DefenseBonus = 4, 
            Description = "Thick, resilient leather." 
        }, 0.4);
    }
}
