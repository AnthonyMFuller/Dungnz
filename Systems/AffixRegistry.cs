namespace Dungnz.Systems;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dungnz.Models;

/// <summary>DTO for a single prefix or suffix affix loaded from <c>item-affixes.json</c>.</summary>
public record AffixDefinition
{
    /// <summary>Unique slug identifier for this affix (e.g. "keen").</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Display name prepended/appended to the item name (e.g. "Keen").</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Flat attack bonus granted by this affix.</summary>
    public int AttackBonus { get; init; }
    /// <summary>Flat defense bonus granted by this affix.</summary>
    public int DefenseBonus { get; init; }
    /// <summary>Max HP bonus granted by this affix (applied to StatModifier).</summary>
    public int MaxHPBonus { get; init; }
    /// <summary>Max mana bonus granted by this affix (applied to MaxManaBonus).</summary>
    public int ManaBonus { get; init; }
    /// <summary>Flat dodge chance bonus (fraction, e.g. 0.08).</summary>
    public float DodgeChanceBonus { get; init; }
    /// <summary>Critical hit chance bonus (fraction, e.g. 0.10).</summary>
    public float CritChanceBonus { get; init; }
    /// <summary>Reduces enemy defense by this flat amount while item is equipped.</summary>
    public int EnemyDefReduction { get; init; }
    /// <summary>Multiplier for extra damage against undead enemies (e.g. 0.20 = +20%).</summary>
    public float HolyDamageVsUndead { get; init; }
    /// <summary>HP restored on each successful player hit.</summary>
    public int HPOnHit { get; init; }
    /// <summary>Flat chance to block incoming attacks (fraction, e.g. 0.10).</summary>
    public float BlockChanceBonus { get; init; }
    /// <summary>When true, the phoenix revive passive gains an additional charge.</summary>
    public bool ReviveCooldownBonus { get; init; }
    /// <summary>Bonus periodic damage dealt each turn.</summary>
    public int PeriodicDmgBonus { get; init; }
    /// <summary>Status effect to apply on hit (e.g. "Bleed"). Null when no on-hit status.</summary>
    public string? OnHitStatus { get; init; }
    /// <summary>Probability (0–1) to apply <see cref="OnHitStatus"/> on each successful hit.</summary>
    public float OnHitChance { get; init; }
}

internal record AffixConfigData
{
    public List<AffixDefinition> Prefixes { get; init; } = new();
    public List<AffixDefinition> Suffixes { get; init; } = new();
}

/// <summary>
/// Loads affix definitions from <c>Data/item-affixes.json</c> and provides a method to apply
/// a random prefix and/or suffix to Uncommon+ items.
/// </summary>
public static class AffixRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static IReadOnlyList<AffixDefinition> _prefixes = Array.Empty<AffixDefinition>();
    private static IReadOnlyList<AffixDefinition> _suffixes = Array.Empty<AffixDefinition>();

    /// <summary>
    /// Loads affix definitions from the specified JSON path.
    /// Called once at game startup; silently no-ops if the file is absent.
    /// </summary>
    public static void Load(string path = "Data/item-affixes.json")
    {
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<AffixConfigData>(json, JsonOptions);
        if (data == null) return;

        _prefixes = data.Prefixes;
        _suffixes = data.Suffixes;
    }

    /// <summary>
    /// Potentially applies a random prefix and/or suffix to <paramref name="item"/> based on its tier.
    /// Common items never receive affixes. Uncommon+ items have a 10% chance per affix slot.
    /// Stat bonuses from the applied affixes are added directly to the item's stat fields.
    /// </summary>
    public static void ApplyRandomAffix(Item item, Random rng)
    {
        if (item.Tier == ItemTier.Common) return;
        if (_prefixes.Count == 0 && _suffixes.Count == 0) return;

        if (_prefixes.Count > 0 && rng.NextDouble() < 0.10)
        {
            var prefix = _prefixes[rng.Next(_prefixes.Count)];
            item.Prefix = prefix.Name;
            ApplyAffixStats(item, prefix);
        }

        if (_suffixes.Count > 0 && rng.NextDouble() < 0.10)
        {
            var suffix = _suffixes[rng.Next(_suffixes.Count)];
            item.Suffix = suffix.Name;
            ApplyAffixStats(item, suffix);
        }
    }

    private static void ApplyAffixStats(Item item, AffixDefinition affix)
    {
        if (affix.AttackBonus > 0)  item.AttackBonus  += affix.AttackBonus;
        if (affix.DefenseBonus > 0) item.DefenseBonus += affix.DefenseBonus;
        if (affix.ManaBonus > 0)    item.MaxManaBonus += affix.ManaBonus;
        if (affix.MaxHPBonus > 0)   item.StatModifier += affix.MaxHPBonus;
        if (affix.DodgeChanceBonus > 0) item.DodgeBonus += affix.DodgeChanceBonus;
    }
}
