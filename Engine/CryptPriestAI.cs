using Dungnz.Models;

namespace Dungnz.Engine;

/// <summary>
/// AI for the Crypt Priest: regenerates HP every turn, self-heals every N turns.
/// </summary>
public class CryptPriestAI : IEnemyAI
{
    /// <summary>
    /// Executes Crypt Priest turn: applies per-turn regen, and on every
    /// <see cref="Enemy.SelfHealEveryTurns"/> turns, performs a large self-heal.
    /// </summary>
    public void TakeTurn(Enemy self, Player player, CombatContext context)
    {
        RegenApplied = 0;
        HealApplied = 0;
        DidSelfHeal = false;

        // Per-turn regen
        if (self.RegenPerTurn > 0)
        {
            int regen = self.RegenPerTurn;
            self.HP = Math.Min(self.MaxHP, self.HP + regen);
            RegenApplied = regen;
        }

        // Periodic self-heal
        if (self.SelfHealEveryTurns > 0)
        {
            if (self.SelfHealCooldown > 0)
            {
                self.SelfHealCooldown--;
            }
            else
            {
                self.SelfHealCooldown = self.SelfHealEveryTurns - 1;
                int heal = self.SelfHealAmount;
                self.HP = Math.Min(self.MaxHP, self.HP + heal);
                HealApplied = heal;
                DidSelfHeal = true;
            }
        }
    }

    /// <summary>Amount of HP regenerated this turn via RegenPerTurn.</summary>
    public int RegenApplied { get; private set; }

    /// <summary>Amount of HP healed this turn via periodic self-heal.</summary>
    public int HealApplied { get; private set; }

    /// <summary>True if the periodic self-heal fired this turn.</summary>
    public bool DidSelfHeal { get; private set; }
}
