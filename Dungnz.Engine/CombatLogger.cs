namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using System.Collections.Generic;

/// <summary>
/// Stub implementation of <see cref="ICombatLogger"/>. Logic will be migrated from
/// <see cref="CombatEngine"/> in a follow-up decomposition task.
/// </summary>
public class CombatLogger : ICombatLogger
{
    private readonly IDisplayService _display;
    private readonly NarrationService _narration;

    /// <summary>Initialises a new <see cref="CombatLogger"/> with the required dependencies.</summary>
    public CombatLogger(IDisplayService display, NarrationService narration)
    {
        _display = display;
        _narration = narration;
    }

    /// <inheritdoc/>
    public string ColorizeDamage(string message, int damage, bool isCrit = false, bool isHealing = false) => message;

    /// <inheritdoc/>
    public void ShowDeathNarration(Enemy enemy) { }

    /// <inheritdoc/>
    public void LogTurn(IList<CombatTurn> turnLog, CombatTurn turn) { }

    /// <inheritdoc/>
    public void ShowRecentTurns(IReadOnlyList<CombatTurn> turnLog) { }
}
