namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Lich King: BoneRattle (Deathmark Curse) every 3rd round; ArmorPiercingAttack (Void Bolt) when self HP below 50%; else Attack.</summary>
public class LichKingAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if (context.RoundNumber % 3 == 0)
            return new EnemyAction(EnemyActionType.BoneRattle);
        if ((double)self.HP / self.MaxHP < 0.50)
            return new EnemyAction(EnemyActionType.ArmorPiercingAttack);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
