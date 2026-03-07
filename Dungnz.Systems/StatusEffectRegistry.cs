using System.Text.Json;

namespace Dungnz.Systems;

/// <summary>
/// Registry of data-driven status effect definitions loaded from JSON.
/// Provides lookup by effect ID for tick damage, duration, and stat modifiers.
/// </summary>
public static class StatusEffectRegistry
{
    private static readonly Dictionary<string, StatusEffectDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets all loaded definitions.</summary>
    public static IReadOnlyDictionary<string, StatusEffectDefinition> All => _definitions;

    /// <summary>
    /// Loads status effect definitions from the specified JSON file.
    /// Can be called multiple times; replaces existing definitions.
    /// </summary>
    /// <param name="path">Path to the status-effects.json file.</param>
    public static void Load(string path)
    {
        _definitions.Clear();
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var defs = JsonSerializer.Deserialize<List<StatusEffectDefinition>>(json, DataJsonOptions.Default);
        if (defs == null) return;
        foreach (var d in defs)
            _definitions[d.Id] = d;
    }

    /// <summary>
    /// Gets the definition for the given effect ID, or null if not found.
    /// </summary>
    /// <param name="id">The effect ID (case-insensitive).</param>
    /// <returns>The definition, or null.</returns>
    public static StatusEffectDefinition? Get(string id) =>
        _definitions.TryGetValue(id, out var def) ? def : null;

    /// <summary>
    /// Gets the tick damage for the given effect ID from data, falling back
    /// to the provided default if no definition exists.
    /// </summary>
    /// <param name="id">The effect ID.</param>
    /// <param name="fallback">Default tick damage if not found in data.</param>
    /// <returns>The tick damage value.</returns>
    public static int GetTickDamage(string id, int fallback)
    {
        var def = Get(id);
        return def?.TickDamage ?? fallback;
    }

    /// <summary>
    /// Gets the default duration for the given effect ID from data, falling back
    /// to the provided default if no definition exists.
    /// </summary>
    /// <param name="id">The effect ID.</param>
    /// <param name="fallback">Default duration if not found in data.</param>
    /// <returns>The duration in rounds.</returns>
    public static int GetDuration(string id, int fallback)
    {
        var def = Get(id);
        return def?.DurationRounds > 0 ? def.DurationRounds : fallback;
    }

    /// <summary>Clears all loaded definitions. Used in testing.</summary>
    internal static void Clear() => _definitions.Clear();
}
