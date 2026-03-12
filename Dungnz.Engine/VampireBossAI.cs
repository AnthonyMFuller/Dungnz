namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Vampire Boss: ArmorPiercingAttack when player HP below 50% or self HP below 30%; else Attack (30% lifesteal on all).</summary>
public class VampireBossAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.PlayerHPPercent < 0.50)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if ((double)self.HP / self.MaxHP < 0.30)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
