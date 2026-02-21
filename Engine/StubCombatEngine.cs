namespace Dungnz.Engine;

using Dungnz.Models;
using Dungnz.Systems;

// Temporary stub — replaced when Barton delivers CombatEngine
internal class StubCombatEngine : ICombatEngine
{
    public CombatResult RunCombat(Player player, Enemy enemy, RunStats? stats = null)
    {
        // Instant win for testing until real combat is wired
        enemy.HP = 0;
        return CombatResult.Won;
    }
}
