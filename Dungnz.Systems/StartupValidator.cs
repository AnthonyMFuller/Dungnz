using System.Text.Json;
using NJsonSchema;

namespace Dungnz.Systems;

/// <summary>
/// Validates all game data files at startup, throwing descriptive exceptions on any
/// missing files, malformed JSON, or schema violations before the game loop initialises.
/// </summary>
public static class StartupValidator
{
    private static readonly (string DataFile, string SchemaFile)[] ValidationPairs =
    [
        ("Data/item-stats.json", "Data/schemas/item-stats.schema.json"),
        ("Data/enemy-stats.json", "Data/schemas/enemy-stats.schema.json"),
        ("Data/crafting-recipes.json", "Data/schemas/crafting-recipes.schema.json"),
        ("Data/item-affixes.json", "Data/schemas/item-affixes.schema.json"),
    ];

    /// <summary>
    /// Verifies every required data file exists, is valid JSON, and conforms to its schema.
    /// Throws <see cref="FileNotFoundException"/> or <see cref="InvalidDataException"/> on failure.
    /// </summary>
    public static void ValidateOrThrow()
    {
        foreach (var (dataFile, schemaFile) in ValidationPairs)
        {
            // Check data file exists
            if (!File.Exists(dataFile))
                throw new FileNotFoundException($"Required data file not found: {dataFile}");

            // Read data file
            string dataContent;
            try { dataContent = File.ReadAllText(dataFile); }
            catch (Exception ex) { throw new InvalidDataException($"Cannot read {dataFile}: {ex.Message}", ex); }

            if (string.IsNullOrWhiteSpace(dataContent))
                throw new InvalidDataException($"Data file is empty: {dataFile}");

            // Parse JSON
            try { JsonDocument.Parse(dataContent); }
            catch (JsonException ex) { throw new InvalidDataException($"Invalid JSON in {dataFile}: {ex.Message}", ex); }

            // Validate against schema if schema file exists
            if (File.Exists(schemaFile))
            {
                try
                {
                    string schemaContent = File.ReadAllText(schemaFile);
                    var schema = JsonSchema.FromJsonAsync(schemaContent).Result;
                    var errors = schema.Validate(dataContent);
                    
                    if (errors.Count > 0)
                    {
                        var errorMessages = string.Join(", ", errors.Select(e => $"{e.Path}: {e.Kind}"));
                        throw new InvalidDataException($"Schema validation failed for {dataFile}: {errorMessages}");
                    }
                }
                catch (InvalidDataException)
                {
                    throw; // Re-throw our own validation errors
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error validating {dataFile} against schema: {ex.Message}", ex);
                }
            }
        }
    }
}
