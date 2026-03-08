namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Stub implementation of <see cref="IStatusEffectApplicator"/>. Logic will be migrated from
/// <see cref="CombatEngine"/> in a follow-up decomposition task.
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
    public void ApplyOnDeathEffects(Player player, Enemy enemy) { }

    /// <inheritdoc/>
    public bool CheckOnDeathEffects(Player player, Enemy enemy, Random rng) => false;

    /// <inheritdoc/>
    public void ResetCombatEffects(Player player, Enemy enemy) { }
}
