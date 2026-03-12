namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Mimic: AggressiveAttack x2.0 (Ambush) on round 1; Attack thereafter.</summary>
public class MimicAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.RoundNumber == 1)
            return new EnemyAction(EnemyActionType.AggressiveAttack, 2.0);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
