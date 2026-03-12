namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Infernal Dragon (Floor 8 final boss): ArmorPiercingAttack (Infernal Rage) when self HP below 50%; Attack on round 1; Cower (Gathering Flame) on even rounds; ArmorPiercingAttack (Flame Strike) on odd rounds. FlameBreath fires via boss-phase pipeline.</summary>
public class InfernalDragonAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.50)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber == 1)
            return new EnemyAction(EnemyActionType.Attack);
        if (context.RoundNumber % 2 == 0)
            return new EnemyAction(EnemyActionType.Cower);
        return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
    }
}
