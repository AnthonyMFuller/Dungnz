namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// The final boss of the dungeon. Gains an enrage buff at low health, boosting its attack
/// by 50%, and can perform a telegraphed charged attack that deals triple damage the following turn.
/// Always drops a Boss Key on defeat.
/// </summary>
public class DungeonBoss : Enemy
{
    /// <summary>
    /// Indicates whether the boss has entered its enraged phase, triggered when HP falls
    /// to 40% or below. While enraged the boss's attack is permanently increased by 50%.
    /// </summary>
    public bool IsEnraged { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the boss is winding up a charge attack this turn.
    /// The player receives a warning, and on the next turn <see cref="ChargeActive"/> fires
    /// for triple damage.
    /// </summary>
    public bool IsCharging { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the boss releases its charged attack this turn,
    /// dealing three times its normal damage. Set after <see cref="IsCharging"/> was
    /// <see langword="true"/> the previous turn.
    /// </summary>
    public bool ChargeActive { get; set; }

    private readonly int _baseAttack;

    /// <summary>
    /// Initialises the Dungeon Boss using either the provided external stats from config
    /// or built-in fallback defaults. Optionally populates the loot table with a Boss Key
    /// item from the item configuration.
    /// </summary>
    /// <param name="stats">
    /// External stats loaded from the enemy config file, or <see langword="null"/> to use
    /// hard-coded defaults (100 HP, 22 ATK, 15 DEF, 100 XP, 50–100 gold).
    /// </param>
    /// <param name="itemConfig">
    /// The loaded item configuration used to source the Boss Key drop,
    /// or <see langword="null"/> to create a fallback inline item.
    /// </param>
    [System.Text.Json.Serialization.JsonConstructor]
    private DungeonBoss() { }

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

    /// <summary>
    /// Checks whether the boss should enter the enraged phase and, if so, permanently
    /// increases its attack by 50%. Should be called after the boss takes damage.
    /// Has no effect if the boss is already enraged or its HP has not dropped to 40% or below.
    /// </summary>
    public void CheckEnrage()
    {
        if (!IsEnraged && HP <= MaxHP * 0.4)
        {
            IsEnraged = true;
            Attack = (int)(Attack * 1.5);
        }
    }
}
