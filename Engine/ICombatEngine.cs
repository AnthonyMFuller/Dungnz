namespace TextGame.Engine;
using TextGame.Models;

public interface ICombatEngine
{
    CombatResult RunCombat(Player player, Enemy enemy);
}
