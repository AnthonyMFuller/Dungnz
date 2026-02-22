namespace Dungnz.Systems;
using System.Text.Json;

/// <summary>
/// Stores the player's cross-run prestige progress, including total wins/runs,
/// the current prestige level, and the cumulative stat bonuses applied at the start of each run.
/// </summary>
public class PrestigeData
{
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

/// <summary>
/// Manages loading and saving cross-run prestige data, recording run outcomes, and
/// returning display strings for the prestige HUD element.
/// </summary>
public static class PrestigeSystem
{
    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dungnz", "prestige.json");

    private static string? _testSavePath;
    private static string ActualSavePath => _testSavePath ?? SavePath;

    internal static void SetSavePathForTesting(string? path) => _testSavePath = path;

    /// <summary>
    /// Loads the persisted <see cref="PrestigeData"/> from disk. Returns a default instance
    /// if the save file does not exist or cannot be read.
    /// </summary>
    /// <returns>The loaded <see cref="PrestigeData"/>, or a fresh default instance on failure.</returns>
    public static PrestigeData Load()
    {
        try
        {
            if (!File.Exists(ActualSavePath)) return new PrestigeData();
            var json = File.ReadAllText(ActualSavePath);
            return JsonSerializer.Deserialize<PrestigeData>(json) ?? new PrestigeData();
        }
        catch { return new PrestigeData(); }
    }

    /// <summary>
    /// Persists the given <see cref="PrestigeData"/> to disk as JSON.
    /// Silently swallows any I/O errors to avoid crashing the game on save failure.
    /// </summary>
    /// <param name="data">The prestige data to save.</param>
    public static void Save(PrestigeData data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ActualSavePath)!);
            File.WriteAllText(ActualSavePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* silently fail */ }
    }

    /// <summary>
    /// Records the outcome of a completed dungeon run, incrementing total run/win counts
    /// and granting a prestige level (with stat bonuses) every three wins.
    /// </summary>
    /// <param name="won"><see langword="true"/> if the player defeated the final boss; <see langword="false"/> otherwise.</param>
    public static void RecordRun(bool won)
    {
        var data = Load();
        data.TotalRuns++;
        if (won)
        {
            data.TotalWins++;
            // Every 3 wins grant a prestige level with a stat bonus
            if (data.TotalWins % 3 == 0)
            {
                data.PrestigeLevel++;
                data.BonusStartAttack += 1;
                data.BonusStartDefense += 1;
                data.BonusStartHP += 5;
            }
        }
        Save(data);
    }

    /// <summary>
    /// Returns a formatted one-line summary of the player's prestige bonuses for display in the HUD,
    /// or an empty string if the player has not yet earned any prestige level.
    /// </summary>
    /// <param name="data">The prestige data to render.</param>
    /// <returns>A formatted prestige display string, or <see cref="string.Empty"/> if prestige level is 0.</returns>
    public static string GetPrestigeDisplay(PrestigeData data)
    {
        if (data.PrestigeLevel == 0) return "";
        return $"⭐ Prestige {data.PrestigeLevel} | +{data.BonusStartAttack} Atk, +{data.BonusStartDefense} Def, +{data.BonusStartHP} HP";
    }
}
