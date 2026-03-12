namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Plague Hound Alpha (Floor 2): ArmorPiercingAttack when player HP below 40% or round%4==0; else Attack (with AppliesPoisonOnHit).</summary>
public class PlagueHoundAlphaAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.PlayerHPPercent < 0.40)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        if (context.RoundNumber % 4 == 0)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
