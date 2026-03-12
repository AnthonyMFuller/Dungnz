namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Bone Archon (Floor 4): BoneRattle (Necrotic Aura) every 3rd round; ArmorPiercingAttack (Void Strike) when self HP below 40%; else Attack.</summary>
public class BoneArchonAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.RoundNumber % 3 == 0)
            return new EnemyAction(EnemyActionType.BoneRattle);
        if ((double)self.HP / self.MaxHP < 0.40)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
