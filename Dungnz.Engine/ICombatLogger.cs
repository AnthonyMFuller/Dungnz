namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using System.Collections.Generic;

/// <summary>
/// Handles all combat message formatting and logging, including damage colorisation,
/// turn history, and enemy death narration.
/// </summary>
public interface ICombatLogger
{
    /// <summary>
    /// Colorizes damage values in a combat message. Damage is rendered in red, healing
    /// in green, and critical hits in bold yellow.
    /// </summary>
    string ColorizeDamage(string message, int damage, bool isCrit = false, bool isHealing = false);

    /// <summary>Displays the enemy death narration via the display service.</summary>
    void ShowDeathNarration(Enemy enemy);

    /// <summary>Appends a turn record to the running turn log.</summary>
    void LogTurn(IList<CombatTurn> turnLog, CombatTurn turn);

    /// <summary>Displays the most recent turns from the combat log.</summary>
    void ShowRecentTurns(IReadOnlyList<CombatTurn> turnLog);
}
