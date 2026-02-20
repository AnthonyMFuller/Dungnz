namespace Dungnz.Systems;

using System.Text.Json;

public class RunStats
{
    public int TurnsTaken { get; set; }
    public int EnemiesDefeated { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int GoldCollected { get; set; }
    public int ItemsFound { get; set; }
    public int FinalLevel { get; set; }
    public TimeSpan TimeElapsed { get; set; }

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
