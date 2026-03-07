using System.Text.Json;

namespace Dungnz.Systems;

/// <summary>
/// Shared JSON serializer options for all data file loading (enemy-stats, item-stats,
/// crafting recipes, affixes, merchant inventories). Centralizes common configuration
/// to avoid duplication and simplify future changes.
/// </summary>
internal static class DataJsonOptions
{
    /// <summary>
    /// Default options for deserializing game data files. Includes case-insensitive
    /// property matching and comment support for JSON files.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}
