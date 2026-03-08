namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>
/// Default AI implementation for enemies without specialized behaviors.
/// Uses a simple attack strategy with no special tactics.
/// </summary>
public class DefaultEnemyAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        // Simple aggressive behavior: standard attack every turn
        return new EnemyAction(EnemyActionType.Attack);
    }
}
