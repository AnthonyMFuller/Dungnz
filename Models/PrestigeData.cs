namespace Dungnz.Models;

/// <summary>
/// Stores the player's cross-run prestige progress, including total wins/runs,
/// the current prestige level, and the cumulative stat bonuses applied at the start of each run.
/// </summary>
public class PrestigeData
{
    /// <summary>Data format version. Used to detect corrupt or stale prestige files.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the player's current prestige level, incremented every three wins.</summary>
    public int PrestigeLevel { get; set; } = 0;

    /// <summary>Gets or sets the total number of dungeon runs the player has won.</summary>
    public int TotalWins { get; set; } = 0;

    /// <summary>Gets or sets the total number of dungeon runs the player has attempted.</summary>
    public int TotalRuns { get; set; } = 0;

    /// <summary>Gets or sets the cumulative flat attack bonus applied to the player at the start of each run.</summary>
    public int BonusStartAttack { get; set; } = 0;

    /// <summary>Gets or sets the cumulative flat defense bonus applied to the player at the start of each run.</summary>
    public int BonusStartDefense { get; set; } = 0;

    /// <summary>Gets or sets the cumulative flat HP bonus applied to the player's maximum HP at the start of each run.</summary>
    public int BonusStartHP { get; set; } = 0;
}
