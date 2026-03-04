using Dungnz.Models;
using Dungnz.Systems;
using Terminal.Gui;

namespace Dungnz.Display.Tui;

/// <summary>
/// Maps ANSI color codes and game colors to Terminal.Gui Attribute colors.
/// </summary>
public static class TuiColorMapper
{
    /// <summary>
    /// Maps an ANSI color code string to a Terminal.Gui color.
    /// </summary>
    public static Color MapAnsiToTuiColor(string ansiColor)
    {
        return ansiColor switch
        {
            ColorCodes.Red => Color.Red,
            ColorCodes.Green => Color.Green,
            ColorCodes.Yellow => Color.Brown,
            ColorCodes.Blue => Color.Blue,
            ColorCodes.Cyan => Color.Cyan,
            ColorCodes.Magenta => Color.Magenta,
            ColorCodes.BrightRed => Color.BrightRed,
            ColorCodes.BrightCyan => Color.BrightCyan,
            ColorCodes.BrightWhite => Color.BrightYellow,
            ColorCodes.Gray => Color.Gray,
            ColorCodes.White => Color.White,
            _ => Color.White
        };
    }

    /// <summary>
    /// Gets the appropriate color for a health percentage.
    /// </summary>
    public static Color GetHealthColor(int current, int max)
    {
        if (max <= 0) return Color.Gray;
        
        var percent = (double)current / max;
        
        return percent switch
        {
            > 0.70 => Color.Green,
            > 0.40 => Color.Brown,
            > 0.20 => Color.Red,
            _ => Color.BrightRed
        };
    }

    /// <summary>
    /// Gets the appropriate color for a mana percentage.
    /// </summary>
    public static Color GetManaColor(int current, int max)
    {
        if (max <= 0) return Color.Gray;
        
        var percent = (double)current / max;
        
        return percent switch
        {
            > 0.50 => Color.Blue,
            > 0.20 => Color.Cyan,
            _ => Color.Gray
        };
    }

    /// <summary>
    /// Gets the appropriate color for a room type on the map.
    /// </summary>
    public static Color GetRoomTypeColor(RoomType type)
    {
        return type switch
        {
            RoomType.Dark => Color.Gray,
            RoomType.Mossy => Color.Green,
            RoomType.Flooded => Color.Blue,
            RoomType.Scorched => Color.Brown,
            RoomType.Ancient => Color.Magenta,
            RoomType.ForgottenShrine => Color.Cyan,
            RoomType.PetrifiedLibrary => Color.Cyan,
            RoomType.ContestedArmory => Color.Brown,
            _ => Color.White
        };
    }

    /// <summary>
    /// Gets the color for an item tier.
    /// </summary>
    public static Color GetItemTierColor(ItemTier tier)
    {
        return tier switch
        {
            ItemTier.Uncommon => Color.Green,
            ItemTier.Rare => Color.BrightCyan,
            ItemTier.Epic => Color.Magenta,
            ItemTier.Legendary => Color.Brown,
            _ => Color.White
        };
    }
}
