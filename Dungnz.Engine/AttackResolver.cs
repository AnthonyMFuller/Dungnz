namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Stub implementation of <see cref="IAttackResolver"/>. Logic will be migrated from
/// <see cref="CombatEngine"/> in a follow-up decomposition task.
/// </summary>
public class AttackResolver : IAttackResolver
{
    private readonly IDisplayService _display;
    private readonly Random _rng;
    private readonly StatusEffectManager _statusEffects;
    private readonly NarrationService _narration;

    /// <summary>Initialises a new <see cref="AttackResolver"/> with the required dependencies.</summary>
    public AttackResolver(
        IDisplayService display,
        Random rng,
        StatusEffectManager statusEffects,
        NarrationService narration)
    {
        _display = display;
        _rng = rng;
        _statusEffects = statusEffects;
        _narration = narration;
    }

    /// <inheritdoc/>
    public void PerformPlayerAttack(Player player, Enemy enemy) { }

    /// <inheritdoc/>
    public bool RollDodge(int defense) => false;

    /// <inheritdoc/>
    public bool RollPlayerDodge(Player player) => false;

    /// <inheritdoc/>
    public bool RollCrit(Player? player = null) => false;
}
