namespace Dungnz.Engine;
using Dungnz.Models;

public interface ICombatEngine
{
    CombatResult RunCombat(Player player, Enemy enemy);
}
