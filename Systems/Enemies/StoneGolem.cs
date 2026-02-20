namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class StoneGolem : Enemy
{
    public StoneGolem(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        IsImmuneToEffects = true;
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
            Name = "Stone Golem";
            HP = MaxHP = 90;
            Attack = 8;
            Defense = 20;
            XPValue = 50;
            LootTable = new LootTable(minGold: 10, maxGold: 25);
        }
    }
}
