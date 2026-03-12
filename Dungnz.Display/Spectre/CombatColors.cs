namespace Dungnz.Display.Spectre;

/// <summary>
/// Spectre markup color token constants for danger-coded combat log entries.
/// </summary>
/// <remarks>
/// Use these constants instead of magic strings in <see cref="SpectreLayoutDisplayService"/>
/// whenever log messages need semantic colour coding.
/// </remarks>
public static class CombatColors
{
    /// <summary>Spectre markup colour for critical hit log entries.</summary>
    public const string CritHit = "bold yellow";

    /// <summary>Spectre markup colour for "HP below 30%" warning log entries.</summary>
    public const string LowHp = "bold red";

    /// <summary>Spectre markup colour for healing event log entries.</summary>
    public const string Heal = "green";

    /// <summary>Spectre markup colour for poison status log entries.</summary>
    public const string Poison = "chartreuse1";

    /// <summary>Spectre markup colour for burning/flame status log entries.</summary>
    public const string Burn = "darkorange";

    /// <summary>Fallback Spectre markup colour for general combat log entries.</summary>
    public const string Default = "white";

    /// <summary>Spectre markup colour for set-bonus-break warnings in loot comparison.</summary>
    public const string SetBonusBreak = "orange3";
}
