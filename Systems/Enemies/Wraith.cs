namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class Wraith : Enemy
{
    public Wraith(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        FlatDodgeChance = 0.30f; // 30% flat dodge, ignores DEF-based formula
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
            Name = "Wraith";
            HP = MaxHP = 35;
            Attack = 18;
            Defense = 2;
            XPValue = 35;
            LootTable = new LootTable(minGold: 8, maxGold: 20);
        }
    }
}
