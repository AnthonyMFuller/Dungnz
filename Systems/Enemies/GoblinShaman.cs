namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class GoblinShaman : Enemy
{
    public GoblinShaman(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        AppliesPoisonOnHit = true;
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
            Name = "Goblin Shaman";
            HP = MaxHP = 25;
            Attack = 10;
            Defense = 4;
            XPValue = 25;
            LootTable = new LootTable(minGold: 5, maxGold: 15);
        }
    }
}
