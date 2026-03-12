namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Goblin Warchief (Floor 1): ArmorPiercingAttack when self HP below 40% or round%4==0; else Attack.</summary>
public class GoblinWarchiefAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.40)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber % 4 == 0)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
