namespace Dungnz.Engine;

using Dungnz.Models;

// Temporary stub — replaced when Barton delivers CombatEngine
internal class StubCombatEngine : ICombatEngine
{
    public CombatResult RunCombat(Player player, Enemy enemy)
    {
        // Instant win for testing until real combat is wired
        enemy.HP = 0;
        return CombatResult.Won;
    }
}
