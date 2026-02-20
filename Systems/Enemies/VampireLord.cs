namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class VampireLord : Enemy
{
    public VampireLord(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        LifestealPercent = 0.50f; // heals 50% of damage dealt
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
            Name = "Vampire Lord";
            HP = MaxHP = 80;
            Attack = 16;
            Defense = 12;
            XPValue = 60;
            LootTable = new LootTable(minGold: 15, maxGold: 30);
        }
    }
}
