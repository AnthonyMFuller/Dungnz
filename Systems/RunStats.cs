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

    /// <summary>
    /// Loads all previously recorded run history from the persistent JSON file.
    /// Returns an empty list if the file does not exist or cannot be read.
    /// </summary>
    public static List<RunStats> LoadHistory()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Dungnz", "stats-history.json");
            if (!File.Exists(path)) return new List<RunStats>();
            var elements = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(path));
            if (elements == null) return new List<RunStats>();
            var result = new List<RunStats>();
            foreach (var el in elements)
            {
                var rs = new RunStats
                {
                    TurnsTaken      = el.TryGetProperty("TurnsTaken",      out var p1) ? p1.GetInt32() : 0,
                    EnemiesDefeated = el.TryGetProperty("EnemiesDefeated", out var p2) ? p2.GetInt32() : 0,
                    DamageDealt     = el.TryGetProperty("DamageDealt",     out var p3) ? p3.GetInt32() : 0,
                    DamageTaken     = el.TryGetProperty("DamageTaken",     out var p4) ? p4.GetInt32() : 0,
                    GoldCollected   = el.TryGetProperty("GoldCollected",   out var p5) ? p5.GetInt32() : 0,
                    ItemsFound      = el.TryGetProperty("ItemsFound",      out var p6) ? p6.GetInt32() : 0,
                    FinalLevel      = el.TryGetProperty("FinalLevel",      out var p7) ? p7.GetInt32() : 0,
                    TimeElapsed     = TimeSpan.FromSeconds(
                        el.TryGetProperty("TimeElapsedSeconds", out var p8) ? p8.GetInt32() : 0)
                };
                result.Add(rs);
            }
            return result;
        }
        catch
        {
            return new List<RunStats>();
        }
    }

    /// <summary>
    /// Returns the top <paramref name="count"/> runs from history, ranked by
    /// score (FinalLevel × 100 + EnemiesDefeated).
    /// </summary>
    public static List<RunStats> GetTopRuns(int count = 5)
    {
        return LoadHistory()
            .OrderByDescending(r => r.FinalLevel * 100 + r.EnemiesDefeated)
            .Take(count)
            .ToList();
    }
}
