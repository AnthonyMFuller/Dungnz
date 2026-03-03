using Dungnz.Models;
using Dungnz.Systems.Enemies;

namespace Dungnz.Engine;

/// <summary>
/// AI for the Goblin Shaman: heals for 20% MaxHP when below 50% HP
/// (cooldown of 3 turns), otherwise performs a normal attack.
/// </summary>
public class GoblinShamanAI : IEnemyAI
{
    private int _healCooldown;

    /// <summary>Gets the current heal cooldown remaining.</summary>
    public int HealCooldown => _healCooldown;

    /// <summary>
    /// Executes Goblin Shaman turn: if HP below 50% and heal is off cooldown,
    /// self-heals for 20% MaxHP. Otherwise, defers to default attack.
    /// </summary>
    public void TakeTurn(Enemy self, Player player, CombatContext context)
    {
        if (_healCooldown > 0) _healCooldown--;

        if (self is GoblinShaman && self.HP < self.MaxHP / 2 && _healCooldown == 0)
        {
            _healCooldown = 3;
            int heal = (int)(self.MaxHP * 0.20);
            self.HP = Math.Min(self.MaxHP, self.HP + heal);
            LastAction = EnemyAction.Heal;
            LastHealAmount = heal;
            return;
        }

        LastAction = EnemyAction.Attack;
        LastHealAmount = 0;
    }

    /// <summary>The action taken on the last TakeTurn call.</summary>
    public EnemyAction LastAction { get; private set; }

    /// <summary>Amount healed on the last heal action (0 if attacked).</summary>
    public int LastHealAmount { get; private set; }
}

/// <summary>Describes the action an enemy AI chose to take on its turn.</summary>
public enum EnemyAction
{
    /// <summary>The enemy performed a standard attack.</summary>
    Attack,
    /// <summary>The enemy healed itself.</summary>
    Heal,
    /// <summary>The enemy used a breath weapon.</summary>
    Breath,
    /// <summary>The enemy resurrected from death.</summary>
    Resurrect
}
