namespace Dungnz.Models;

/// <summary>
/// Game-wide constants shared across all layers of the application.
/// All magic numbers that represent game rules belong here.
/// </summary>
public static class GameConstants
{
    // ── Dungeon layout ────────────────────────────────────────────────────────

    /// <summary>The final floor of the dungeon. Reaching this floor and defeating the boss wins the run.</summary>
    public const int FinalFloor = 8;

    /// <summary>Default number of columns in a generated dungeon grid.</summary>
    public const int DungeonWidth = 5;

    /// <summary>Default number of rows in a generated dungeon grid.</summary>
    public const int DungeonHeight = 4;

    // ── Player progression ────────────────────────────────────────────────────

    /// <summary>The maximum player level achievable in a run.</summary>
    public const int MaxLevel = 20;

    /// <summary>
    /// Divisor used in the XP level-up formula: a player reaches level N when
    /// <c>XP / XpBase + 1 &gt; Level</c>.
    /// </summary>
    public const int XpBase = 100;

    // ── Environmental hazard damage ───────────────────────────────────────────

    /// <summary>HP lost when walking into a room containing a spike trap.</summary>
    public const int HazardDamageSpike = 5;

    /// <summary>HP lost when walking into a room containing a poison trap.</summary>
    public const int HazardDamagePoison = 3;

    /// <summary>HP lost when walking into a room containing a fire trap.</summary>
    public const int HazardDamageFire = 7;

    // ── Thresholds &amp; probabilities ────────────────────────────────────────────

    /// <summary>
    /// HP fraction below which the player's health is considered critically low
    /// (used for warning log messages). E.g. <c>0.2</c> = 20% max HP.
    /// </summary>
    public const double CriticalHpThreshold = 0.2;

    /// <summary>
    /// Probability that an atmospheric narration line is shown when the player
    /// enters a new room. E.g. <c>0.15</c> = 15% chance.
    /// </summary>
    public const double AtmosphericNarrationChance = 0.15;

    /// <summary>
    /// Base probability that a flee attempt succeeds before class/item modifiers
    /// are applied. E.g. <c>0.4</c> = 40% base chance.
    /// </summary>
    public const double BaseFleeChance = 0.4;
}
