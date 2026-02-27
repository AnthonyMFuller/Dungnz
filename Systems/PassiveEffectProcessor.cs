namespace Dungnz.Systems;

using System;
using System.Linq;
using Dungnz.Display;
using Dungnz.Models;

/// <summary>Identifies when a passive item effect should be triggered.</summary>
public enum PassiveEffectTrigger
{
    /// <summary>Player successfully hit the enemy and dealt damage.</summary>
    OnPlayerHit,

    /// <summary>Enemy has just been killed (HP &lt;= 0).</summary>
    OnEnemyKilled,

    /// <summary>Player just took damage from the enemy.</summary>
    OnPlayerTakeDamage,

    /// <summary>Start of each combat turn (before the player acts).</summary>
    OnTurnStart,

    /// <summary>Combat has just started — used to reset per-combat state.</summary>
    OnCombatStart,

    /// <summary>
    /// Player HP just reached 0 or below — gives survive-at-one or phoenix-revive
    /// a chance to intercept before death is declared.
    /// </summary>
    OnPlayerWouldDie,
}

/// <summary>
/// Processes passive item effects that trigger at specific moments during combat or on equip.
/// Checks all three equipment slots for relevant <see cref="Item.PassiveEffectId"/> values and
/// applies the corresponding effect.
/// </summary>
public class PassiveEffectProcessor
{
    private readonly IDisplayService _display;
    private readonly Random _rng;
    private readonly StatusEffectManager _statusEffects;

    /// <summary>
    /// Initialises a new <see cref="PassiveEffectProcessor"/>.
    /// </summary>
    public PassiveEffectProcessor(IDisplayService display, Random rng, StatusEffectManager statusEffects)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _statusEffects = statusEffects ?? throw new ArgumentNullException(nameof(statusEffects));
    }

    /// <summary>
    /// Evaluates all passive effects active on the player's equipped items and fires the ones
    /// whose trigger matches <paramref name="trigger"/>.
    /// </summary>
    /// <param name="player">The player whose equipment is checked.</param>
    /// <param name="trigger">The event that just occurred.</param>
    /// <param name="enemy">The enemy involved in the current combat (may be <see langword="null"/> outside combat).</param>
    /// <param name="damageDealt">
    /// For <see cref="PassiveEffectTrigger.OnPlayerHit"/> and <see cref="PassiveEffectTrigger.OnEnemyKilled"/>:
    /// the damage dealt this turn/kill. For <see cref="PassiveEffectTrigger.OnPlayerTakeDamage"/>: the damage received.
    /// </param>
    /// <returns>
    /// A bonus damage value to apply back to the enemy (used for <c>damage_reflect</c> and
    /// <c>thunderstrike_on_kill</c>). Zero if no bonus damage applies.
    /// </returns>
    public int ProcessPassiveEffects(Player player, PassiveEffectTrigger trigger, Enemy? enemy, int damageDealt)
    {
        int bonusDamage = 0;

        var equipped = new[] { player.EquippedWeapon, player.EquippedAccessory }
            .Concat(player.AllEquippedArmor)
            .Where(i => i != null);

        foreach (var item in equipped)
        {
            if (item?.PassiveEffectId == null) continue;
            bonusDamage += ProcessEffect(player, item.PassiveEffectId, trigger, enemy, damageDealt);
        }

        return bonusDamage;
    }

    /// <summary>Resets all per-combat passive state on the player.</summary>
    public static void ResetCombatState(Player player)
    {
        player.AegisUsedThisCombat = false;
        player.ShadowmeldUsedThisCombat = false;
        player.BonusFleeUsed = false;
        player.ExtraFleeCount = 0;
        player.ShadowDanceCounter = 0;
    }

    /// <summary>
    /// Applies the <c>cooldown_reduction</c> passive immediately when the Ring of Haste is equipped.
    /// Reduces all active ability cooldowns by 1.
    /// </summary>
    public static void ApplyCooldownReduction(AbilityManager abilities)
    {
        abilities?.ReduceAllCooldowns(1);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private int ProcessEffect(Player player, string effectId, PassiveEffectTrigger trigger, Enemy? enemy, int damageDealt)
    {
        switch (effectId)
        {
            case "vampiric_strike":
                return ApplyVampiricStrike(player, trigger, damageDealt);

            case "frostbite_on_hit":
                return ApplyFrostbiteOnHit(player, trigger, enemy);

            case "thunderstrike_on_kill":
                return ApplyThunderstrikeOnKill(player, trigger, damageDealt);

            case "survive_at_one":
                return ApplySurviveAtOne(player, trigger);

            case "first_attack_dodge":
                return ApplyFirstAttackDodge(player, trigger);

            case "damage_reflect":
                return ApplyDamageReflect(player, trigger, enemy, damageDealt);

            case "cooldown_reduction":
                // Applied at equip-time via ApplyCooldownReduction — no in-combat trigger
                return 0;

            case "phoenix_revive":
                return ApplyPhoenixRevive(player, trigger);

            case "carry_weight":
                // Passive weight bonus — no combat trigger needed
                return 0;

            case "extra_flee":
                return ApplyExtraFlee(player, trigger);

            case "warding_ring":
                return ApplyWardingRing(player, trigger, enemy);

            case "belt_regen":
                return ApplyBeltRegen(player, trigger);

            default:
                return 0;
        }
    }

    private int ApplyVampiricStrike(Player player, PassiveEffectTrigger trigger, int damageDealt)
    {
        if (trigger != PassiveEffectTrigger.OnPlayerHit || damageDealt <= 0) return 0;

        var heal = Math.Max(1, (int)(damageDealt * 0.20));
        player.Heal(heal);
        _display.ShowColoredCombatMessage($"🩸 Lifedrinker — vampiric strike heals you for {heal} HP!", ColorCodes.Green);
        return 0;
    }

    private int ApplyFrostbiteOnHit(Player player, PassiveEffectTrigger trigger, Enemy? enemy)
    {
        if (trigger != PassiveEffectTrigger.OnPlayerHit || enemy == null) return 0;
        if (enemy.HP <= 0) return 0;

        if (_rng.NextDouble() < 0.30)
        {
            _statusEffects.Apply(enemy, StatusEffect.Slow, 2);
            _display.ShowColoredCombatMessage($"❄ Frostbite Edge — {enemy.Name} is slowed by the cold!", ColorCodes.Blue);
        }
        return 0;
    }

    private int ApplyThunderstrikeOnKill(Player player, PassiveEffectTrigger trigger, int damageDealt)
    {
        if (trigger != PassiveEffectTrigger.OnEnemyKilled) return 0;

        var bonusDmg = Math.Max(1, (int)(damageDealt * 0.50));
        _display.ShowColoredCombatMessage($"⚡ Thunderstrike Maul — a thunderclap deals {bonusDmg} bonus damage!", ColorCodes.Yellow);
        return bonusDmg;
    }

    private int ApplySurviveAtOne(Player player, PassiveEffectTrigger trigger)
    {
        if (trigger != PassiveEffectTrigger.OnPlayerWouldDie) return 0;
        if (player.AegisUsedThisCombat) return 0;

        player.AegisUsedThisCombat = true;
        player.HP = 1;
        _display.ShowColoredCombatMessage("🛡 Aegis of the Immortal — the shield refuses to let you fall!", ColorCodes.Yellow);
        return 0;
    }

    private int ApplyFirstAttackDodge(Player player, PassiveEffectTrigger trigger)
    {
        // Wired by CombatEngine before the enemy attack roll via OnCombatStart setup
        // The flag ShadowmeldUsedThisCombat is checked in CombatEngine.PerformEnemyTurn
        if (trigger != PassiveEffectTrigger.OnCombatStart) return 0;

        player.ShadowmeldUsedThisCombat = false; // reset at combat start (already false from ResetCombatState)
        return 0;
    }

    private int ApplyDamageReflect(Player player, PassiveEffectTrigger trigger, Enemy? enemy, int damageDealt)
    {
        if (trigger != PassiveEffectTrigger.OnPlayerTakeDamage || enemy == null || damageDealt <= 0) return 0;

        var reflect = Math.Max(1, (int)(damageDealt * 0.25));
        _display.ShowColoredCombatMessage($"🔥 Ironheart Plate — {reflect} damage reflected back to {enemy.Name}!", ColorCodes.BrightRed);
        return reflect;
    }

    private int ApplyPhoenixRevive(Player player, PassiveEffectTrigger trigger)
    {
        if (trigger != PassiveEffectTrigger.OnPlayerWouldDie) return 0;
        if (player.PhoenixUsedThisRun) return 0;

        player.PhoenixUsedThisRun = true;
        var reviveHp = Math.Max(1, (int)(player.MaxHP * 0.30));
        player.HP = reviveHp;
        _display.ShowColoredCombatMessage($"🔥 Amulet of the Phoenix — you are reborn from the ashes at {reviveHp} HP!", ColorCodes.Yellow);
        return 0;
    }

    private int ApplyExtraFlee(Player player, PassiveEffectTrigger trigger)
    {
        if (trigger != PassiveEffectTrigger.OnCombatStart) return 0;
        player.ExtraFleeCount = 1;
        return 0;
    }

    private int ApplyWardingRing(Player player, PassiveEffectTrigger trigger, Enemy? enemy)
    {
        if (trigger != PassiveEffectTrigger.OnTurnStart) return 0;
        if (player.HP > player.MaxHP * 0.25) return 0;

        // Apply a temporary defense boost via status modifier (show message; stat system handles it separately)
        _display.ShowColoredCombatMessage($"🔮 Ring of Warding — protective runes flare, granting +10 DEF while you're near death!", ColorCodes.Cyan);
        return 0;
    }

    private int ApplyBeltRegen(Player player, PassiveEffectTrigger trigger)
    {
        if (trigger != PassiveEffectTrigger.OnTurnStart) return 0;
        if (player.HP >= player.MaxHP) return 0;

        player.Heal(3);
        _display.ShowColoredCombatMessage($"🌿 Belt of Regeneration — regenerates 3 HP.", ColorCodes.Green);
        return 0;
    }
}
