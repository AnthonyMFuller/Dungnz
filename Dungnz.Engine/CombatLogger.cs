namespace Dungnz.Engine;
using Dungnz.Data;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using System.Collections.Generic;

/// <summary>
/// Handles all combat message formatting and logging, including damage colorisation,
/// turn history, and enemy death narration.
/// Migrated from <see cref="CombatEngine"/> as part of the decomposition task (#1205).
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

    /// <summary>
    /// Replaces only the last occurrence of <paramref name="find"/> in <paramref name="source"/>.
    /// Safe because damage values always appear at the end of narration strings.
    /// </summary>
    private static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int lastIndex = source.LastIndexOf(find);
        if (lastIndex < 0) return source;
        return source.Substring(0, lastIndex) + replace + source.Substring(lastIndex + find.Length);
    }

    /// <inheritdoc/>
    public string ColorizeDamage(string message, int damage, bool isCrit = false, bool isHealing = false)
    {
        var damageStr = damage.ToString();
        var coloredDamage = isHealing
            ? ColorCodes.Colorize(damageStr, ColorCodes.Green)
            : ColorCodes.Colorize(damageStr, ColorCodes.BrightRed);

        if (isCrit)
        {
            // Crits get bold yellow wrapper
            return ColorCodes.Colorize(ReplaceLastOccurrence(message, damageStr, coloredDamage), ColorCodes.Yellow + ColorCodes.Bold);
        }

        return ReplaceLastOccurrence(message, damageStr, coloredDamage);
    }

    /// <inheritdoc/>
    public void ShowDeathNarration(Enemy enemy)
    {
        if (enemy is DungeonBoss)
            _display.ShowCombat(BossNarration.GetDeath(enemy.Name));
        else
            _display.ShowCombat(_narration.Pick(EnemyNarration.GetDeaths(enemy.Name), enemy.Name));
    }

    /// <inheritdoc/>
    public void LogTurn(IList<CombatTurn> turnLog, CombatTurn turn) => turnLog.Add(turn);

    /// <inheritdoc/>
    public void ShowRecentTurns(IReadOnlyList<CombatTurn> turnLog)
    {
        var recent = turnLog.Count > 3 ? turnLog.Skip(turnLog.Count - 3).ToList() : turnLog;
        if (recent.Count == 0) return;

        _display.ShowMessage("─── Recent turns ───");
        foreach (var turn in recent)
        {
            string line;
            if (turn.IsDodge)
                line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.Gray}dodged{ColorCodes.Reset}";
            else if (turn.IsCrit)
                line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.Bold}{ColorCodes.Yellow}CRIT{ColorCodes.Reset} {ColorCodes.BrightRed}{turn.Damage}{ColorCodes.Reset} dmg";
            else
                line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.BrightRed}{turn.Damage}{ColorCodes.Reset} dmg";

            if (turn.StatusApplied != null)
                line += $" [{ColorCodes.Green}{turn.StatusApplied}{ColorCodes.Reset}]";

            _display.ShowMessage(line);
        }
    }
}
