namespace Dungnz.Display.Spectre;

/// <summary>
/// Panel height constants for the Spectre TUI layout.
/// Baseline: 40-row terminal. Ratios defined in <see cref="SpectreLayout"/>.
/// </summary>
/// <remarks>
/// Layout ratios (rows split 2:5:3 = 20%:50%:30% of terminal height):
/// <list type="bullet">
///   <item>TopRow = Ratio(2) → 20% of terminal height (8 rows @ baseline)</item>
///   <item>MiddleRow = Ratio(5) → 50% of terminal height (20 rows @ baseline)</item>
///   <item>BottomRow = Ratio(3) → 30% of terminal height (12 rows @ baseline)</item>
///   <item>Within TopRow: Map Ratio(6) = 60% width, Stats Ratio(4) = 40% width</item>
///   <item>Within MiddleRow: Content Ratio(7) = 70% width, Gear Ratio(3) = 30% width</item>
///   <item>Within BottomRow: Log Ratio(7) = 70% height, Input Ratio(3) = 30% height</item>
/// </list>
/// All renderers and tests must reference these constants rather than magic numbers.
/// </remarks>
public static class LayoutConstants
{
    /// <summary>Baseline terminal height in rows used to derive panel height constants.</summary>
    public const int BaselineTerminalHeight = 40;

    /// <summary>Stats panel total height (TopRow = 20% of 40 = 8 rows).</summary>
    public const int StatsPanelHeight = 8;

    /// <summary>Gear panel total height (MiddleRow = 50% of 40 = 20 rows).</summary>
    public const int GearPanelHeight = 20;

    /// <summary>Content panel total height (MiddleRow = 50% of 40 = 20 rows).</summary>
    public const int ContentPanelHeight = 20;

    /// <summary>Log panel total height (BottomRow 30% × Log 70% of 40 ≈ 8 rows).</summary>
    public const int LogPanelHeight = 8;

    /// <summary>Map panel usable content height (TopRow 20% × Map 60% of 40 ≈ 5 rows).</summary>
    public const int MapPanelHeight = 5;
}
