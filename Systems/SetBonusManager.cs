namespace Dungnz.Systems;

using System;
using System.Collections.Generic;
using System.Linq;
using Dungnz.Models;

/// <summary>Describes the bonuses active for a specific equipment set at a given piece count.</summary>
public record SetBonus
{
    /// <summary>Identifier of the equipment set this bonus belongs to (e.g. "shadowstalker").</summary>
    public string SetId { get; init; } = string.Empty;
    /// <summary>Minimum number of set pieces that must be equipped to activate this bonus.</summary>
    public int PiecesRequired { get; init; }
    /// <summary>Human-readable description shown to the player when the bonus is active.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Flat bonus added to the player's attack while this set bonus is active.</summary>
    public int AttackBonus { get; init; }
    /// <summary>Flat bonus added to the player's defense while this set bonus is active.</summary>
    public int DefenseBonus { get; init; }
    /// <summary>Max HP bonus granted while this set bonus is active.</summary>
    public int MaxHPBonus { get; init; }
    /// <summary>Max mana bonus granted while this set bonus is active.</summary>
    public int MaxManaBonus { get; init; }
    /// <summary>Critical hit chance bonus (fraction) granted while this set bonus is active.</summary>
    public float CritChanceBonus { get; init; }
    /// <summary>Dodge chance bonus (fraction) granted while this set bonus is active.</summary>
    public float DodgeChanceBonus { get; init; }
    /// <summary>When true, the Shadow Dance mechanic is active — every 3rd attack auto-crits.</summary>
    public bool GrantsShadowDance { get; init; }
    /// <summary>When true, the Unyielding mechanic is active — +50% DEF + CC immunity when HP &lt; 25%.</summary>
    public bool GrantsUnyielding { get; init; }
    /// <summary>When true, the Arcane Surge mechanic is active — +40% spell damage when mana &gt; 80%.</summary>
    public bool GrantsArcaneSurge { get; init; }
    /// <summary>Ironclad 4-piece: fraction of incoming damage reflected back to the attacker (e.g. 0.1 = 10%).</summary>
    public float DamageReflectPercent { get; init; }
    /// <summary>Shadowstep 4-piece: guaranteed Bleed application on every hit.</summary>
    public bool SetBonusAppliesBleed { get; init; }
    /// <summary>Arcane Ascendant 4-piece: flat mana discount applied to all ability costs.</summary>
    public int ManaDiscount { get; init; }
    /// <summary>Sentinel 4-piece: player cannot be stunned.</summary>
    public bool GrantsStunImmunity { get; init; }
}

/// <summary>
/// Manages equipment set bonuses — counts equipped set pieces and applies or
/// removes the corresponding stat bonuses.
/// </summary>
public static class SetBonusManager
{
    // Hardcoded set bonus table — one entry per (setId, pieceCount) combination.
    private static readonly IReadOnlyList<SetBonus> SetBonuses = new List<SetBonus>
    {
        // ── Shadowstalker (Rogue/Ranger) ────────────────────────────────────
        new SetBonus
        {
            SetId = "shadowstalker", PiecesRequired = 2,
            Description = "Shadowstalker 2-piece: +10% crit, +5% dodge",
            CritChanceBonus = 0.10f,
            DodgeChanceBonus = 0.05f
        },
        new SetBonus
        {
            SetId = "shadowstalker", PiecesRequired = 3,
            Description = "Shadowstalker 3-piece: Shadow Dance — every 3rd attack auto-crits",
            GrantsShadowDance = true
        },

        // ── Ironclad Vanguard (Warrior/Paladin) ─────────────────────────────
        new SetBonus
        {
            SetId = "ironclad", PiecesRequired = 2,
            Description = "Ironclad 2-piece: +10 max HP, +3 DEF",
            MaxHPBonus = 10,
            DefenseBonus = 3
        },
        new SetBonus
        {
            SetId = "ironclad", PiecesRequired = 3,
            Description = "Ironclad 3-piece: Unyielding — when HP < 25%: +50% DEF, CC immunity for 3T",
            GrantsUnyielding = true
        },

        // ── Arcanist's Regalia (Mage/Necromancer) ───────────────────────────
        new SetBonus
        {
            SetId = "arcanist", PiecesRequired = 2,
            Description = "Arcanist 2-piece: +20 max mana, -10% ability mana cost",
            MaxManaBonus = 20
        },
        new SetBonus
        {
            SetId = "arcanist", PiecesRequired = 3,
            Description = "Arcanist 3-piece: Arcane Surge — when mana > 80% max: +40% spell damage",
            GrantsArcaneSurge = true
        },

        // ── 4-piece set bonuses ───────────────────────────────────────────────
        new SetBonus
        {
            SetId = "ironclad", PiecesRequired = 4,
            Description = "Ironclad 4-piece: Reflect — 10% of incoming damage reflected back",
            DamageReflectPercent = 0.10f
        },
        new SetBonus
        {
            SetId = "shadowstalker", PiecesRequired = 4,
            Description = "Shadowstep 4-piece: every hit guarantees Bleed on the target",
            SetBonusAppliesBleed = true
        },
        new SetBonus
        {
            SetId = "arcanist", PiecesRequired = 4,
            Description = "Arcane Ascendant 4-piece: -1 mana cost on all abilities",
            ManaDiscount = 1
        },
        new SetBonus
        {
            SetId = "sentinel", PiecesRequired = 4,
            Description = "Sentinel 4-piece: Stun immunity",
            GrantsStunImmunity = true
        },
    };

    /// <summary>
    /// Returns the number of distinct equipped items belonging to <paramref name="setId"/>.
    /// Checks all three equipment slots (weapon, armor, accessory).
    /// </summary>
    public static int GetEquippedSetPieces(Player player, string setId)
    {
        int count = 0;
        if (player.EquippedWeapon?.SetId    == setId) count++;
        if (player.EquippedHead?.SetId      == setId) count++;
        if (player.EquippedShoulders?.SetId == setId) count++;
        if (player.EquippedChest?.SetId     == setId) count++;
        if (player.EquippedHands?.SetId     == setId) count++;
        if (player.EquippedLegs?.SetId      == setId) count++;
        if (player.EquippedFeet?.SetId      == setId) count++;
        if (player.EquippedBack?.SetId      == setId) count++;
        if (player.EquippedOffHand?.SetId   == setId) count++;
        if (player.EquippedAccessory?.SetId == setId) count++;
        return count;
    }

    /// <summary>
    /// Returns all <see cref="SetBonus"/> entries whose piece threshold is met by the
    /// player's currently equipped items.
    /// </summary>
    public static IReadOnlyList<SetBonus> GetActiveBonuses(Player player)
    {
        var result = new List<SetBonus>();
        var setIds = GetEquippedSetIds(player);

        foreach (var setId in setIds)
        {
            int pieces = GetEquippedSetPieces(player, setId);
            foreach (var bonus in SetBonuses)
            {
                if (bonus.SetId == setId && bonus.PiecesRequired <= pieces)
                    result.Add(bonus);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns a human-readable summary of all active set bonuses for display in the UI.
    /// Returns an empty string when no set bonuses are active.
    /// </summary>
    public static string GetActiveBonusDescription(Player player)
    {
        var bonuses = GetActiveBonuses(player);
        if (bonuses.Count == 0) return string.Empty;
        return string.Join("\n", bonuses.Select(b => b.Description));
    }

    /// <summary>
    /// Applies the cumulative stat bonuses of all active set bonuses to the player,
    /// then removes the effects of bonuses that are no longer active.
    /// Should be called after every equip/unequip operation.
    /// </summary>
    public static void ApplySetBonuses(Player player)
    {
        // We recompute from scratch each call — first remove any previously applied set bonuses,
        // then re-apply based on current equipment.
        // To keep things simple without a separate "previously applied" tracker,
        // we store the total delta on the player via dedicated set-bonus fields.
        // Instead, we layer on top of base stats and recompute the delta.

        var active = GetActiveBonuses(player);

        int totalDef  = active.Sum(b => b.DefenseBonus);
        int totalHP   = active.Sum(b => b.MaxHPBonus);
        int totalMana = active.Sum(b => b.MaxManaBonus);
        float totalDodge = active.Sum(b => b.DodgeChanceBonus);

        // Store the resulting bonuses so callers can query them.
        // Actual stat application is handled in CombatEngine / EquipmentManager
        // as the set bonuses are combat-time modifiers rather than permanent stat changes.
        _ = totalDef;
        _ = totalHP;
        _ = totalMana;
        _ = totalDodge;

        // Wire 4-piece set bonus flags onto the player so combat systems can read them.
        player.DamageReflectPercent = active.Sum(b => b.DamageReflectPercent);
        player.SetBonusAppliesBleed = active.Any(b => b.SetBonusAppliesBleed);
        player.ManaDiscount         = active.Sum(b => b.ManaDiscount);
        player.IsStunImmune         = active.Any(b => b.GrantsStunImmunity);
    }

    /// <summary>Returns true when the Arcane Surge set bonus is active and the player's mana is above 80%.</summary>
    public static bool IsArcaneSurgeActive(Player player)
    {
        var bonuses = GetActiveBonuses(player);
        return bonuses.Any(b => b.GrantsArcaneSurge) && player.Mana > player.MaxMana * 0.80;
    }

    /// <summary>Returns true when the Shadow Dance set bonus is active.</summary>
    public static bool IsShadowDanceActive(Player player)
        => GetActiveBonuses(player).Any(b => b.GrantsShadowDance);

    /// <summary>Returns true when the Unyielding set bonus is active and HP is below 25%.</summary>
    public static bool IsUnyieldingActive(Player player)
        => GetActiveBonuses(player).Any(b => b.GrantsUnyielding) && player.HP < player.MaxHP * 0.25;

    // ── Private helpers ─────────────────────────────────────────────────────

    private static IEnumerable<string> GetEquippedSetIds(Player player)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (player.EquippedWeapon?.SetId != null)    ids.Add(player.EquippedWeapon.SetId);
        if (player.EquippedChest?.SetId != null)     ids.Add(player.EquippedChest.SetId);
        if (player.EquippedAccessory?.SetId != null) ids.Add(player.EquippedAccessory.SetId);
        return ids;
    }
}
