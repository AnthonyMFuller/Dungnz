namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Goblin Shaman: heals itself (SelfHeal 20% MaxHP) when below 30% HP every 3 rounds. Otherwise attacks.</summary>
public class GoblinShamanAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        double selfHPPercent = (double)self.HP / self.MaxHP;
        if (selfHPPercent < 0.30 && context.RoundNumber % 3 == 1)
            return new EnemyAction(EnemyActionType.SelfHeal, 0.20);
        return new EnemyAction(EnemyActionType.Attack);
    }
}
