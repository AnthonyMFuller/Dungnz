namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

public record EnemyStats
{
    public string Name { get; init; } = string.Empty;
    public int MaxHP { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int XPValue { get; init; }
    public int MinGold { get; init; }
    public int MaxGold { get; init; }
}

public static class EnemyConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static Dictionary<string, EnemyStats> Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Enemy config file not found: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<Dictionary<string, EnemyStats>>(json, JsonOptions);

            if (config == null || config.Count == 0)
            {
                throw new InvalidDataException($"Enemy config file is empty or invalid: {path}");
            }

            // Validate each enemy has required stats
            foreach (var (key, stats) in config)
            {
                if (string.IsNullOrWhiteSpace(stats.Name))
                {
                    throw new InvalidDataException($"Enemy '{key}' missing required field: Name");
                }
                if (stats.MaxHP <= 0)
                {
                    throw new InvalidDataException($"Enemy '{key}' has invalid MaxHP: {stats.MaxHP}");
                }
                if (stats.Attack < 0)
                {
                    throw new InvalidDataException($"Enemy '{key}' has invalid Attack: {stats.Attack}");
                }
                if (stats.Defense < 0)
                {
                    throw new InvalidDataException($"Enemy '{key}' has invalid Defense: {stats.Defense}");
                }
                if (stats.XPValue < 0)
                {
                    throw new InvalidDataException($"Enemy '{key}' has invalid XPValue: {stats.XPValue}");
                }
                if (stats.MinGold < 0 || stats.MaxGold < stats.MinGold)
                {
                    throw new InvalidDataException($"Enemy '{key}' has invalid gold range: {stats.MinGold}-{stats.MaxGold}");
                }
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to parse enemy config file '{path}': {ex.Message}", ex);
        }
    }
}
