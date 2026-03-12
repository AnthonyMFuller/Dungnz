namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>AI for the Wraith: phases into shadow (Cower) when below 25% HP; otherwise DrainAttack (siphons mana + armor-piercing).</summary>
public class WraithAI : IEnemyAI
{
    public EnemyAction ChooseAction(Enemy self, Player player, CombatContext context)
    {
        if ((double)self.HP / self.MaxHP < 0.25)
            return new EnemyAction(EnemyActionType.Cower);
        return new EnemyAction(EnemyActionType.DrainAttack);
    }
}
