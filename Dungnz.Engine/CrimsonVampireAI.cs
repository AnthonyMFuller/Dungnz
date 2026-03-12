namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Crimson Vampire (Floor 5): ArmorPiercingAttack (Blood Fang) when player HP below 50%; ArmorPiercingAttack (Desperate Feed) when self HP below 30%; else Attack (30% lifesteal on all).</summary>
public class CrimsonVampireAI : IEnemyAI
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
