namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>
/// AI implementation for Goblins: cowardly opportunists who flee at low HP
/// and attack aggressively when the player is weakened.
/// </summary>
public class GoblinAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        double selfHPPercent = (double)self.HP / self.MaxHP;
        
        // Cowardly: flee when HP drops to or below 25%
        if (selfHPPercent <= 0.25)
        {
            return new EnemyAction(EnemyActionType.Flee);
        }
        
        // Opportunistic: aggressive attack when player is weakened (< 50% HP)
        if (context.PlayerHPPercent < 0.5)
        {
            return new EnemyAction(EnemyActionType.AggressiveAttack, 1.5);
        }
        
        // Default: standard attack
        return new EnemyAction(EnemyActionType.Attack);
    }
}
