namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Iron Sentinel (Floor 3): ArmorPiercingAttack every turn when self HP below 60% (Emergency Protocols); else every even round (Precision Strike); else Attack.</summary>
public class IronSentinelAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.60)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber % 2 == 0)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
