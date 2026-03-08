namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>
/// AI implementation for Skeletons: relentless undead warriors with armor-piercing attacks
/// and bone rattle ability that reduces player accuracy.
/// </summary>
public class SkeletonAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        // Bone Rattle: every 3rd turn, reduce player accuracy
        if (context.RoundNumber % 3 == 0)
        {
            return new EnemyAction(EnemyActionType.BoneRattle, 0.10); // 10% accuracy reduction
        }
        
        // Relentless: armor-piercing attack (ignores defense)
        // Skeletons never flee and always use armor-piercing attacks
        return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
    }
}
