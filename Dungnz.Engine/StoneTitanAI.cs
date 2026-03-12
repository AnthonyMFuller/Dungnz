namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Stone Titan: ArmorPiercingAttack (Boulder Smash) on round%5==0; Cower (Stone Wall) on even rounds; else Attack.</summary>
public class StoneTitanAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.RoundNumber % 5 == 0)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber % 2 == 0)
            return new EnemyAction(EnemyActionType.Cower);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
