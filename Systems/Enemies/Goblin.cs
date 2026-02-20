namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class Goblin : Enemy
{
    public Goblin(EnemyStats? stats = null)
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
            Name = "Goblin";
            HP = MaxHP = 20;
            Attack = 8;
            Defense = 2;
            XPValue = 15;
            LootTable = new LootTable(minGold: 2, maxGold: 8);
        }
    }
}
