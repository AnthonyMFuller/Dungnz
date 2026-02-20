namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

public class Achievement
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Func<RunStats, Player, bool> Condition { get; init; } = (_, _) => false;
    public bool Unlocked { get; set; }
}

public class AchievementSystem
{
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dungnz", "achievements.json");

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
