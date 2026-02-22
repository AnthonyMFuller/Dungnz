using Dungnz.Models;

namespace Dungnz.Systems;

/// <summary>
/// Provides ANSI escape codes for terminal color formatting and utility methods
/// for applying context-aware color schemes to game output (health thresholds,
/// mana levels, status effects, etc.).
/// </summary>
public static class ColorCodes
{
    // Basic ANSI color codes
    /// <summary>ANSI code for red text (health, damage, negative effects).</summary>
    public const string Red = "\u001b[31m";
    
    /// <summary>ANSI code for green text (success, healing, positive effects, XP).</summary>
    public const string Green = "\u001b[32m";
    
    /// <summary>ANSI code for yellow text (warnings, gold, medium priority).</summary>
    public const string Yellow = "\u001b[33m";
    
    /// <summary>ANSI code for blue text (mana, abilities, information).</summary>
    public const string Blue = "\u001b[34m";
    
    /// <summary>ANSI code for cyan text (defense, shields, secondary stats).</summary>
    public const string Cyan = "\u001b[36m";
    
    /// <summary>ANSI code for bright/bold red text (critical health, attack stat, high damage).</summary>
    public const string BrightRed = "\u001b[91m";
    
    /// <summary>ANSI code for magenta text (ancient rooms, special areas).</summary>
    public const string Magenta = "\u001b[35m";

    /// <summary>ANSI code for white text (standard rooms, default text).</summary>
    public const string White = "\u001b[37m";

    /// <summary>ANSI code for gray text (disabled options, cooldowns, dimmed info).</summary>
    public const string Gray = "\u001b[90m";
    
    /// <summary>ANSI code for bright white text (highlights, important values).</summary>
    public const string BrightWhite = "\u001b[97m";

    /// <summary>ANSI code for bright cyan text (Rare item tier, high-value items).</summary>
    public const string BrightCyan = "\u001b[96m";
    
    /// <summary>ANSI code for bold text formatting.</summary>
    public const string Bold = "\u001b[1m";
    
    /// <summary>ANSI reset code to return to default terminal formatting.</summary>
    public const string Reset = "\u001b[0m";

    /// <summary>
    /// Wraps the given text in the specified color code and automatically resets
    /// formatting at the end.
    /// </summary>
    /// <param name="text">The text to colorize.</param>
    /// <param name="color">The ANSI color code (e.g. <see cref="Red"/>, <see cref="Green"/>).</param>
    /// <returns>A string with embedded ANSI codes: [color][text][reset].</returns>
    public static string Colorize(string text, string color)
    {
        return $"{color}{text}{Reset}";
    }

    /// <summary>
    /// Returns an appropriate color code for displaying health values based on
    /// percentage thresholds: green (healthy), yellow (injured), red (critical),
    /// or bright red (near death).
    /// </summary>
    /// <param name="current">The current HP value.</param>
    /// <param name="max">The maximum HP value.</param>
    /// <returns>
    /// An ANSI color code representing the health state:
    /// <list type="bullet">
    /// <item>&gt;70% → Green</item>
    /// <item>40-70% → Yellow</item>
    /// <item>20-40% → Red</item>
    /// <item>&lt;20% → Bright Red</item>
    /// </list>
    /// </returns>
    public static string HealthColor(int current, int max)
    {
        if (max <= 0) return Gray;
        
        var percent = (double)current / max;
        
        return percent switch
        {
            > 0.70 => Green,
            > 0.40 => Yellow,
            > 0.20 => Red,
            _ => BrightRed
        };
    }

    /// <summary>
    /// Returns an appropriate color code for displaying mana values based on
    /// percentage thresholds: blue (full/high), cyan (medium), or gray (depleted).
    /// </summary>
    /// <param name="current">The current mana value.</param>
    /// <param name="max">The maximum mana value.</param>
    /// <returns>
    /// An ANSI color code representing the mana state:
    /// <list type="bullet">
    /// <item>&gt;50% → Blue</item>
    /// <item>20-50% → Cyan</item>
    /// <item>&lt;20% → Gray</item>
    /// </list>
    /// </returns>
    public static string ManaColor(int current, int max)
    {
        if (max <= 0) return Gray;
        
        var percent = (double)current / max;
        
        return percent switch
        {
            > 0.50 => Blue,
            > 0.20 => Cyan,
            _ => Gray
        };
    }

    /// <summary>
    /// Returns an appropriate color code for displaying weight ratios based on
    /// capacity thresholds: green (plenty of room), yellow (getting full), or
    /// red (near/at capacity).
    /// </summary>
    /// <param name="current">The current weight carried.</param>
    /// <param name="max">The maximum weight capacity.</param>
    /// <returns>
    /// An ANSI color code representing the weight state:
    /// <list type="bullet">
    /// <item>&lt;80% → Green</item>
    /// <item>80-95% → Yellow</item>
    /// <item>&gt;95% → Red</item>
    /// </list>
    /// </returns>
    public static string WeightColor(double current, double max)
    {
        if (max <= 0) return Gray;
        
        var percent = current / max;
        
        return percent switch
        {
            < 0.80 => Green,
            < 0.95 => Yellow,
            _ => Red
        };
    }

    /// <summary>
    /// Returns the item's name wrapped in the ANSI color appropriate for its tier:
    /// BrightWhite (Common), Green (Uncommon), BrightCyan (Rare).
    /// </summary>
    public static string ColorizeItemName(string name, ItemTier tier)
    {
        var color = tier switch
        {
            ItemTier.Uncommon => Green,
            ItemTier.Rare     => BrightCyan,
            _                 => BrightWhite
        };
        return $"{color}{name}{Reset}";
    }

    /// <summary>
    /// Returns the ANSI color code associated with the given room type, used to
    /// render map symbols in context-appropriate colors.
    /// </summary>
    /// <param name="type">The <see cref="RoomType"/> to look up.</param>
    /// <returns>
    /// An ANSI color code for the room type:
    /// <list type="bullet">
    /// <item>Standard → White</item>
    /// <item>Dark → Gray</item>
    /// <item>Mossy → Green</item>
    /// <item>Flooded → Blue</item>
    /// <item>Scorched → Yellow</item>
    /// <item>Ancient → Magenta</item>
    /// </list>
    /// </returns>
    public static string GetRoomTypeColor(RoomType type)
    {
        return type switch
        {
            RoomType.Dark     => Gray,
            RoomType.Mossy    => Green,
            RoomType.Flooded  => Blue,
            RoomType.Scorched => Yellow,
            RoomType.Ancient  => Magenta,
            _                 => White
        };
    }

    /// <summary>
    /// Strips all ANSI escape sequences from the given text, returning plain text
    /// suitable for testing or terminals that don't support color codes.
    /// </summary>
    /// <param name="text">The text potentially containing ANSI codes.</param>
    /// <returns>The same text with all ANSI sequences removed.</returns>
    public static string StripAnsiCodes(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"\u001b\[[0-9;]*m", string.Empty);
    }
}
