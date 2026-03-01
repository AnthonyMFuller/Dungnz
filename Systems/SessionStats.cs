namespace Dungnz.Systems;

/// <summary>
/// Tracks per-session balance metrics for post-run analysis: enemies killed,
/// gold earned, floors cleared, boss kills, and total damage dealt.
/// </summary>
public class SessionStats
{
    /// <summary>Total enemies killed during this session.</summary>
    public int EnemiesKilled { get; set; }

    /// <summary>Total gold earned (loot + room rewards) during this session.</summary>
    public int GoldEarned { get; set; }

    /// <summary>Number of dungeon floors cleared (reached the exit of).</summary>
    public int FloorsCleared { get; set; }

    /// <summary>Number of boss enemies killed during this session.</summary>
    public int BossKills { get; set; }

    /// <summary>Total damage dealt by the player to all enemies during this session.</summary>
    public int DamageDealt { get; set; }
}
