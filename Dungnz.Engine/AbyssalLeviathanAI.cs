namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Abyssal Leviathan (Floor 7): ArmorPiercingAttack when self HP below 40%, round%3==0, or player HP below 60%; else Attack. TidalSlam fires via boss-phase pipeline.</summary>
public class AbyssalLeviathanAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.40)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber % 3 == 0)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.PlayerHPPercent < 0.60)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
