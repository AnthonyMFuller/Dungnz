namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class Mimic : Enemy
{
    public Mimic(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        IsAmbush = true; // first-turn surprise: player cannot act
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
            Name = "Mimic";
            HP = MaxHP = 40;
            Attack = 14;
            Defense = 8;
            XPValue = 40;
            LootTable = new LootTable(minGold: 10, maxGold: 25);
        }
    }
}
