using System.Text.Json.Serialization;

namespace Dungnz.Systems;

/// <summary>
/// Data-driven definition for a status effect, loaded from JSON.
/// Provides tick damage, stat modifiers, duration, and display metadata.
/// </summary>
public record StatusEffectDefinition
{
    /// <summary>Unique identifier matching the StatusEffect enum name (e.g. "Poison").</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Display name shown in combat messages.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Flavour text describing the effect.</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>Default duration in combat rounds when applied.</summary>
    [JsonPropertyName("durationRounds")]
    public int DurationRounds { get; init; }

    /// <summary>Damage dealt per tick (start of turn). Null if no tick damage.</summary>
    [JsonPropertyName("tickDamage")]
    public int? TickDamage { get; init; }

    /// <summary>Stat modifiers applied while the effect is active. Keys: "Attack", "Defense".</summary>
    [JsonPropertyName("statModifiers")]
    public Dictionary<string, double>? StatModifiers { get; init; }
}
