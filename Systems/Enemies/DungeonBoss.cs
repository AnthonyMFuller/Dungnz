namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

public class DungeonBoss : Enemy
{
    public bool IsEnraged { get; private set; }
    public bool IsCharging { get; set; }        // true = warn player this turn, deal 3x next turn
    public bool ChargeActive { get; set; }      // true = 3x damage this turn
    private readonly int _baseAttack;

    public DungeonBoss(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Dungeon Boss";
            HP = MaxHP = 100;
            Attack = 22;
            Defense = 15;
            XPValue = 100;
            LootTable = new LootTable(minGold: 50, maxGold: 100);
        }

        _baseAttack = Attack;

        if (itemConfig != null)
        {
            var bossKey = itemConfig.FirstOrDefault(i => i.Name == "Boss Key");
            if (bossKey != null)
                LootTable.AddDrop(ItemConfig.CreateItem(bossKey), 1.0);
        }
        else
        {
            LootTable.AddDrop(new Item
            {
                Name = "Boss Key",
                Type = ItemType.Consumable,
                Description = "Proof of your victory.",
                StatModifier = 0
            }, 1.0);
        }
    }

    public void CheckEnrage()
    {
        if (!IsEnraged && HP <= MaxHP * 0.4)
        {
            IsEnraged = true;
            Attack = (int)(Attack * 1.5);
        }
    }
}
