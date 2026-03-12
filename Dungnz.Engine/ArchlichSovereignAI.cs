namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Archlich Sovereign (Floor 6): ArmorPiercingAttack every turn when self HP below 50%; BoneRattle (Death Curse) on round%5==0; ArmorPiercingAttack (Void Blast) on even rounds; else Attack.</summary>
public class ArchlichSovereignAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.50)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber % 5 == 0)
            return new EnemyAction(EnemyActionType.BoneRattle);
        if (context.RoundNumber % 2 == 0)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
