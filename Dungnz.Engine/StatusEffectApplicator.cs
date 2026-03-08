namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;

/// <summary>
/// Handles status effect application, on-death mechanics, and end-of-combat state cleanup.
/// Migrated from <see cref="CombatEngine"/> as part of the decomposition task (#1205).
/// </summary>
public class StatusEffectApplicator : IStatusEffectApplicator
{
    private readonly IDisplayService _display;
    private readonly Random _rng;
    private readonly StatusEffectManager _statusEffects;

    /// <summary>Initialises a new <see cref="StatusEffectApplicator"/> with the required dependencies.</summary>
    public StatusEffectApplicator(
        IDisplayService display,
        Random rng,
        StatusEffectManager statusEffects)
    {
        _display = display;
        _rng = rng;
        _statusEffects = statusEffects;
    }

    /// <inheritdoc/>
    public void ApplyOnDeathEffects(Player player, Enemy enemy)
    {
        if (enemy.OnDeathEffect.HasValue)
        {
            _statusEffects.Apply(player, enemy.OnDeathEffect.Value, 3);
            _display.ShowCombatMessage($"The dying {enemy.Name} curses you — {enemy.OnDeathEffect.Value} applied!");
        }
        // PlagueBear: 40% chance to reapply Poison on death
        if (enemy.PoisonOnDeathChance > 0 && _rng.NextDouble() < enemy.PoisonOnDeathChance)
        {
            _statusEffects.Apply(player, StatusEffect.Poison, 3);
            _display.ShowCombatMessage($"The {enemy.Name}'s corpse releases a final cloud of plague!");
        }
    }

    /// <inheritdoc/>
    public bool CheckOnDeathEffects(Player player, Enemy enemy, Random rng)
    {
        // ArchlichSovereign revive: once per combat at 0 HP after adds were summoned
        if (enemy is ArchlichSovereign lich && !lich.HasRevived && lich.Phase2Triggered && lich.TurnCount >= 0)
        {
            lich.HasRevived = true;
            lich.HP = (int)(lich.MaxHP * 0.20);
            lich.DamageImmune = false;
            lich.AddsAlive = 0;
            _display.ShowCombatMessage("⚠ The Archlich reforms from sheer necromantic will — it rises at 20% HP!");
            return true; // enemy revived
        }
        return false;
    }

    /// <inheritdoc/>
    public void ResetCombatEffects(Player player, Enemy enemy)
    {
        _statusEffects.Clear(player);
        _statusEffects.Clear(enemy);
        player.ActiveEffects.Clear();
        player.ResetComboPoints();
        player.LastStandTurns = 0;
        player.EvadeNextAttack = false;
        player.WardingVeilActive = false;
        player.ActiveMinions.Clear();
        player.ActiveTraps.Clear();
        player.TrapTriggeredThisCombat = false;
        player.DivineHealUsedThisCombat = false;
        player.HunterMarkUsedThisCombat = false;
        player.DivineShieldTurnsRemaining = 0;
        player.LichsBargainActive = false;
        player.IsManaShieldActive = false;
        PassiveEffectProcessor.ResetCombatState(player);
        player.ResetCombatPassives(); // Fix #544: revert BattleHardened ATK stacks; Fix #546: clear DivineBulwarkFired
    }
}
