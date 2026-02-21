namespace Dungnz.Systems;

using System.Text.Json;

/// <summary>
/// Accumulates performance metrics for a single dungeon run, used for end-of-run
/// summaries, history logging, and achievement evaluation.
/// </summary>
public class RunStats
{
    /// <summary>The total number of turns the player took during the run.</summary>
    public int TurnsTaken { get; set; }

    /// <summary>The total number of enemies defeated during the run.</summary>
    public int EnemiesDefeated { get; set; }

    /// <summary>The cumulative damage dealt by the player to all enemies during the run.</summary>
    public int DamageDealt { get; set; }

    /// <summary>The cumulative damage received by the player from all sources during the run.</summary>
    public int DamageTaken { get; set; }

    /// <summary>The total amount of gold collected from enemy loot and room rewards during the run.</summary>
    public int GoldCollected { get; set; }

    /// <summary>The total number of items discovered or picked up during the run.</summary>
    public int ItemsFound { get; set; }

    /// <summary>The player's character level at the end of the run.</summary>
    public int FinalLevel { get; set; }

    /// <summary>The wall-clock duration from the start to the end of the run.</summary>
    public TimeSpan TimeElapsed { get; set; }

    /// <summary>
    /// Outputs a formatted statistics summary to the provided delegate, including
    /// turns, kills, damage, gold, items, level, and time.
    /// </summary>
    /// <param name="output">A delegate that receives each formatted line of output, e.g. the console or UI renderer.</param>
    public void Display(Action<string> output)
    {
        output("=== RUN STATISTICS ===");
        output($"Turns Taken:      {TurnsTaken}");
        output($"Enemies Defeated: {EnemiesDefeated}");
        output($"Damage Dealt:     {DamageDealt}");
        output($"Damage Taken:     {DamageTaken}");
        output($"Gold Collected:   {GoldCollected}");
        output($"Items Found:      {ItemsFound}");
        output($"Final Level:      {FinalLevel}");
        output($"Time Elapsed:     {(int)TimeElapsed.TotalMinutes}m {TimeElapsed.Seconds}s");
    }

    /// <summary>
    /// Appends a run record to the persistent JSON history file stored in the user's AppData folder.
    /// Failures are silently swallowed so a history write error never disrupts the game.
    /// </summary>
    /// <param name="stats">The statistics collected during the completed run.</param>
    /// <param name="won"><see langword="true"/> if the player won the run; <see langword="false"/> if they died.</param>
    public static void AppendToHistory(RunStats stats, bool won)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Dungnz");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "stats-history.json");

            var entries = new List<object>();
            if (File.Exists(path))
            {
                var existing = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(path));
                if (existing != null) entries.AddRange(existing.Cast<object>());
            }

            entries.Add(new
            {
                Date = DateTime.UtcNow,
                Won = won,
                stats.TurnsTaken,
                stats.EnemiesDefeated,
                stats.DamageDealt,
                stats.DamageTaken,
                stats.GoldCollected,
                stats.ItemsFound,
                stats.FinalLevel,
                TimeElapsedSeconds = (int)stats.TimeElapsed.TotalSeconds
            });

            File.WriteAllText(path, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Non-critical — don't crash the game if history can't be written
        }
    }
}
