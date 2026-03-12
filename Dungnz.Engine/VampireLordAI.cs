namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Vampire Lord: ArmorPiercingAttack (Blood Nova) when player HP above 75%; ArmorPiercingAttack (Predator Rush) when player HP below 50%; else Attack. All attacks carry 50% lifesteal.</summary>
public class VampireLordAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.PlayerHPPercent > 0.75)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.PlayerHPPercent < 0.50)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
