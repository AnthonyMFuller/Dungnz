namespace TextGame.Systems.Enemies;
using TextGame.Models;

public class Goblin : Enemy
{
    public Goblin()
    {
        Name = "Goblin";
        HP = MaxHP = 20;
        Attack = 8;
        Defense = 2;
        XPValue = 15;
        LootTable = new LootTable(minGold: 2, maxGold: 8);
    }
}
