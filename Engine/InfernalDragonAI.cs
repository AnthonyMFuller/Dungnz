using Dungnz.Models;

namespace Dungnz.Engine;

/// <summary>
/// AI for the Infernal Dragon: multi-phase AI with breath weapon.
/// Phase 1 (>50% HP): regular attacks with occasional Breath (high damage).
/// Phase 2 (≤50% HP): enraged — uses Breath more often, increased damage.
/// </summary>
public class InfernalDragonAI : IEnemyAI
{
    private int _breathCooldown;
    private bool _phase2Active;

    /// <summary>Gets the current breath weapon cooldown remaining.</summary>
    public int BreathCooldown => _breathCooldown;

    /// <summary>Gets whether the dragon has entered Phase 2 (enraged).</summary>
    public bool Phase2Active => _phase2Active;

    /// <summary>
    /// Executes Infernal Dragon turn: tracks HP-based phase transitions and
    /// breath weapon cooldowns. In Phase 1 (>50% HP), breath fires every 3 turns.
    /// In Phase 2 (≤50% HP), breath fires every 2 turns with 1.5× damage multiplier.
    /// </summary>
    public void TakeTurn(Enemy self, Player player, CombatContext context)
    {
        double hpPercent = (double)self.HP / self.MaxHP;

        // Phase transition check
        if (hpPercent <= 0.50 && !_phase2Active)
        {
            _phase2Active = true;
            // Reset cooldown on phase transition to make breath available sooner
            _breathCooldown = 1;
        }

        // Determine breath cooldown based on phase
        int breathInterval = _phase2Active ? 2 : 3;

        if (_breathCooldown > 0)
        {
            _breathCooldown--;
        }

        // Check if breath should fire
        if (_breathCooldown == 0)
        {
            _breathCooldown = breathInterval;
            LastAction = EnemyAction.Breath;
            BreathDamageMultiplier = _phase2Active ? 1.5f : 1.0f;
            return;
        }

        LastAction = EnemyAction.Attack;
        BreathDamageMultiplier = 1.0f;
    }

    /// <summary>The action taken on the last TakeTurn call.</summary>
    public EnemyAction LastAction { get; private set; }

    /// <summary>Damage multiplier for breath weapon (1.0 in Phase 1, 1.5 in Phase 2).</summary>
    public float BreathDamageMultiplier { get; private set; }
}
