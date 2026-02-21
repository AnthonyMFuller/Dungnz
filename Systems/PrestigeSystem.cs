namespace Dungnz.Systems;
using System.Text.Json;

public class PrestigeData
{
    public int PrestigeLevel { get; set; } = 0;
    public int TotalWins { get; set; } = 0;
    public int TotalRuns { get; set; } = 0;
    public int BonusStartAttack { get; set; } = 0;
    public int BonusStartDefense { get; set; } = 0;
    public int BonusStartHP { get; set; } = 0;
}

public static class PrestigeSystem
{
    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Dungnz", "prestige.json");

    public static PrestigeData Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return new PrestigeData();
            var json = File.ReadAllText(SavePath);
            return JsonSerializer.Deserialize<PrestigeData>(json) ?? new PrestigeData();
        }
        catch { return new PrestigeData(); }
    }

    public static void Save(PrestigeData data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            File.WriteAllText(SavePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* silently fail */ }
    }

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

    public static string GetPrestigeDisplay(PrestigeData data)
    {
        if (data.PrestigeLevel == 0) return "";
        return $"⭐ Prestige {data.PrestigeLevel} | +{data.BonusStartAttack} Atk, +{data.BonusStartDefense} Def, +{data.BonusStartHP} HP";
    }
}
