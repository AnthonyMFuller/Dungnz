namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Shadow Wraith: Cower (phase shift) when self HP below 30% or round%4==0; else ArmorPiercingAttack (Shadow Strike).</summary>
public class ShadowWraithAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.30)
            return new EnemyAction(EnemyActionType.Cower);
        if (context.RoundNumber % 4 == 0)
            return new EnemyAction(EnemyActionType.Cower);
        return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
    }
}
