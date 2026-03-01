namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>Defines an HP-threshold phase for a boss, triggering a named ability once.</summary>
/// <param name="HpPercent">Fraction of max HP (0.0–1.0) at or below which the ability fires.</param>
/// <param name="AbilityName">The ability identifier passed to the boss phase ability handler in <see cref="Dungnz.Engine.CombatEngine"/>.</param>
public record BossPhase(double HpPercent, string AbilityName);

/// <summary>
/// The final boss of the dungeon. Gains an enrage buff at low health, boosting its attack
/// by 50%, and can perform a telegraphed charged attack that deals triple damage the following turn.
/// Always drops a Boss Key on defeat.
/// </summary>
public class DungeonBoss : Enemy
{
    /// <summary>Flavour description of this boss's special ability (used in enemy info display).</summary>
    public string SpecialAbilityDescription { get; protected set; } = string.Empty;

    /// <summary>The dungeon floor this boss guards (1–8).</summary>
    public int FloorNumber { get; protected set; }

    /// <summary>
    /// Indicates whether the boss has entered its enraged phase, triggered when HP falls
    /// to 40% or below. While enraged the boss's attack is permanently increased by 50%.
    /// </summary>
    public bool IsEnraged { get; internal set; }

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

    /// <summary>HP-threshold phases for this boss. Each entry fires its ability once when HP falls to or below the threshold.</summary>
    public List<BossPhase> Phases { get; } = new();

    /// <summary>Tracks which phase ability names have already fired this combat, preventing duplicate triggers.</summary>
    public HashSet<string> FiredPhases { get; } = new();

    /// <summary>
    /// Periodic turn-based abilities. Key = turn interval (fires when <c>TurnCount % key == 0</c>),
    /// value = ability name passed to the boss phase ability handler in <see cref="Engine.CombatEngine"/>.
    /// </summary>
    public Dictionary<int, string> TurnActions { get; } = new();

    private readonly int _baseAttack;

    /// <summary>
    /// Initialises the Dungeon Boss using either the provided external stats from config
    /// or built-in fallback defaults. Optionally populates the loot table with a Boss Key
    /// item from the item configuration.
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    private DungeonBoss() { }

    /// <summary>Initialises the Dungeon Boss with the given stats and item configuration, or falls back to hard-coded defaults.</summary>
    /// <param name="stats">External stats from config, or <see langword="null"/> to use defaults.</param>
    /// <param name="itemConfig">Item configuration used to source the Boss Key drop, or <see langword="null"/> to use an inline fallback.</param>
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
            AsciiArt = stats.AsciiArt;
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
                StatModifier = 0,
                Tier = ItemTier.Rare
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
