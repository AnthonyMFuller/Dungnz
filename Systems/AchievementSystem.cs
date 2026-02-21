namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

/// <summary>
/// Represents a single achievement that can be unlocked when specific run conditions are met.
/// </summary>
public class Achievement
{
    /// <summary>The display name of the achievement shown to the player.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>A short description explaining how the achievement is earned.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// A predicate evaluated against end-of-run statistics and the player's state to determine
    /// whether the achievement should be unlocked. Only evaluated on won runs.
    /// </summary>
    public Func<RunStats, Player, bool> Condition { get; init; } = (_, _) => false;

    /// <summary>
    /// Indicates whether this achievement has already been unlocked in a previous run.
    /// Set to <see langword="true"/> after being persisted to disk.
    /// </summary>
    public bool Unlocked { get; set; }
}

/// <summary>
/// Evaluates run achievements after a victory, tracks which achievements have been unlocked
/// across all runs, and persists unlock state to disk.
/// </summary>
public class AchievementSystem
{
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dungnz", "achievements.json");

    /// <summary>
    /// The full catalogue of achievements available in the game, each with its unlock condition
    /// evaluated at run completion.
    /// </summary>
    public List<Achievement> Achievements { get; } = new()
    {
        new Achievement
        {
            Name = "Glass Cannon",
            Description = "Win a run with HP below 10.",
            Condition = (_, player) => player.HP < 10
        },
        new Achievement
        {
            Name = "Untouchable",
            Description = "Win a run without taking any damage.",
            Condition = (stats, _) => stats.DamageTaken == 0
        },
        new Achievement
        {
            Name = "Hoarder",
            Description = "Collect 500+ gold in a single run.",
            Condition = (stats, _) => stats.GoldCollected >= 500
        },
        new Achievement
        {
            Name = "Elite Hunter",
            Description = "Defeat 10+ enemies in a single run.",
            Condition = (stats, _) => stats.EnemiesDefeated >= 10
        },
        new Achievement
        {
            Name = "Speed Runner",
            Description = "Win a run in under 100 turns.",
            Condition = (stats, _) => stats.TurnsTaken < 100
        }
    };

    /// <summary>
    /// Evaluates all achievement conditions against the completed run. Only runs where the player
    /// won are eligible. Newly satisfied achievements are persisted to disk and returned.
    /// </summary>
    /// <param name="stats">Statistics collected over the course of the run.</param>
    /// <param name="player">The player's final state at run completion.</param>
    /// <param name="won"><see langword="true"/> if the player won; achievements are only evaluated on wins.</param>
    /// <returns>A list of achievements unlocked for the first time during this run.</returns>
    public List<Achievement> Evaluate(RunStats stats, Player player, bool won)
    {
        if (!won) return new List<Achievement>();

        var newlyUnlocked = new List<Achievement>();
        var savedNames = LoadUnlocked();

        foreach (var a in Achievements)
        {
            a.Unlocked = savedNames.Contains(a.Name);
            if (!a.Unlocked && a.Condition(stats, player))
            {
                a.Unlocked = true;
                savedNames.Add(a.Name);
                newlyUnlocked.Add(a);
            }
        }

        SaveUnlocked(savedNames);
        return newlyUnlocked;
    }

    private static HashSet<string> LoadUnlocked()
    {
        try
        {
            if (File.Exists(HistoryPath))
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(HistoryPath)) ?? new();
        }
        catch { }
        return new HashSet<string>();
    }

    private static void SaveUnlocked(HashSet<string> names)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
            File.WriteAllText(HistoryPath, JsonSerializer.Serialize(names, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
