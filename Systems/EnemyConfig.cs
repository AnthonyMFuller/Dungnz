namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

/// <summary>
/// Represents the raw combat statistics for a single enemy type as read from the JSON
/// configuration file. Immutable once deserialised and used to initialise enemy instances.
/// </summary>
public record EnemyStats
{
    /// <summary>The display name of the enemy type shown during combat and in room descriptions.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The enemy's maximum (and starting) hit points.</summary>
    public int MaxHP { get; init; }

    /// <summary>The enemy's base attack value used to calculate damage dealt per hit.</summary>
    public int Attack { get; init; }

    /// <summary>The enemy's base defense value used to reduce incoming damage.</summary>
    public int Defense { get; init; }

    /// <summary>The amount of experience points awarded to the player upon defeating this enemy.</summary>
    public int XPValue { get; init; }

    /// <summary>The minimum amount of gold that drops from this enemy's loot table.</summary>
    public int MinGold { get; init; }

    /// <summary>The maximum amount of gold that drops from this enemy's loot table.</summary>
    public int MaxGold { get; init; }

    /// <summary>Optional ASCII art lines displayed before combat. Empty array means no art.</summary>
    public string[] AsciiArt { get; init; } = Array.Empty<string>();

    /// <summary>When <c>true</c>, this enemy is of an undead creature type (e.g. Skeleton, Zombie, Lich).</summary>
    public bool IsUndead { get; init; }
}

/// <summary>
/// Provides a static helper for loading and validating enemy stat definitions from a JSON
/// configuration file, returning a dictionary keyed by enemy type identifier.
/// </summary>
public static class EnemyConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Reads the specified JSON file and deserialises it as a dictionary of enemy stat entries,
    /// validating that each entry has a non-empty name, positive max HP, and non-negative
    /// attack, defense, XP, and gold values.
    /// </summary>
    /// <param name="path">Absolute or relative path to the enemy JSON configuration file.</param>
    /// <returns>A dictionary mapping enemy type keys to their validated <see cref="EnemyStats"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist at <paramref name="path"/>.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file is empty, invalid, or contains malformed enemy data.</exception>
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
