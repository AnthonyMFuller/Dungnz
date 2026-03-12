namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Stone Golem: Shield Stance (Cower) on even rounds; Smash (ArmorPiercingAttack) on odd rounds.</summary>
public class StoneGolemAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.RoundNumber % 2 == 0)
            return new EnemyAction(EnemyActionType.Cower);
        return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
    }
}
