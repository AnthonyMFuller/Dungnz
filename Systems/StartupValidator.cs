using System.Text.Json;

namespace Dungnz.Systems;

/// <summary>
/// Validates all game data files at startup, throwing descriptive exceptions on any
/// missing files or malformed JSON before the game loop initialises.
/// </summary>
public static class StartupValidator
{
    private static readonly string[] RequiredDataFiles =
    [
        "Data/item-stats.json",
        "Data/enemy-stats.json",
        "Data/crafting-recipes.json",
        "Data/item-affixes.json",
    ];

    /// <summary>
    /// Verifies every required data file exists and is valid JSON.
    /// Throws <see cref="FileNotFoundException"/> or <see cref="InvalidDataException"/> on failure.
    /// </summary>
    public static void ValidateOrThrow()
    {
        foreach (var path in RequiredDataFiles)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required data file not found: {path}");

            string content;
            try { content = File.ReadAllText(path); }
            catch (Exception ex) { throw new InvalidDataException($"Cannot read {path}: {ex.Message}", ex); }

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidDataException($"Data file is empty: {path}");

            try { JsonDocument.Parse(content); }
            catch (JsonException ex) { throw new InvalidDataException($"Invalid JSON in {path}: {ex.Message}", ex); }
        }
    }
}
