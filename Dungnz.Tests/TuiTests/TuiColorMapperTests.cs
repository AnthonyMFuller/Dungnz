using Dungnz.Display.Tui;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Terminal.Gui;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for TuiColorMapper static mapping methods.
/// Pure static logic — no Terminal.Gui initialization required.
/// </summary>
public class TuiColorMapperTests
{
    // ─── MapAnsiToTuiColor ───────────────────────────────────────────────────

    [Fact]
    public void MapAnsiToTuiColor_Red_ReturnsRed()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Red).Should().Be(Color.Red);
    }

    [Fact]
    public void MapAnsiToTuiColor_Green_ReturnsGreen()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Green).Should().Be(Color.Green);
    }

    [Fact]
    public void MapAnsiToTuiColor_Yellow_ReturnsBrown()
    {
        // Terminal.Gui maps Yellow ANSI to Brown (closest equivalent)
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Yellow).Should().Be(Color.Brown);
    }

    [Fact]
    public void MapAnsiToTuiColor_Blue_ReturnsBlue()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Blue).Should().Be(Color.Blue);
    }

    [Fact]
    public void MapAnsiToTuiColor_Cyan_ReturnsCyan()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Cyan).Should().Be(Color.Cyan);
    }

    [Fact]
    public void MapAnsiToTuiColor_Magenta_ReturnsMagenta()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Magenta).Should().Be(Color.Magenta);
    }

    [Fact]
    public void MapAnsiToTuiColor_BrightRed_ReturnsBrightRed()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.BrightRed).Should().Be(Color.BrightRed);
    }

    [Fact]
    public void MapAnsiToTuiColor_BrightCyan_ReturnsBrightCyan()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.BrightCyan).Should().Be(Color.BrightCyan);
    }

    [Fact]
    public void MapAnsiToTuiColor_BrightWhite_ReturnsBrightYellow()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.BrightWhite).Should().Be(Color.BrightYellow);
    }

    [Fact]
    public void MapAnsiToTuiColor_Gray_ReturnsGray()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.Gray).Should().Be(Color.Gray);
    }

    [Fact]
    public void MapAnsiToTuiColor_White_ReturnsWhite()
    {
        TuiColorMapper.MapAnsiToTuiColor(ColorCodes.White).Should().Be(Color.White);
    }

    [Fact]
    public void MapAnsiToTuiColor_Unknown_ReturnsWhite()
    {
        TuiColorMapper.MapAnsiToTuiColor("unknown-code").Should().Be(Color.White);
    }

    [Fact]
    public void MapAnsiToTuiColor_EmptyString_ReturnsWhite()
    {
        TuiColorMapper.MapAnsiToTuiColor("").Should().Be(Color.White);
    }

    // ─── GetHealthColor ──────────────────────────────────────────────────────

    [Fact]
    public void GetHealthColor_MaxIsZero_ReturnsGray()
    {
        TuiColorMapper.GetHealthColor(0, 0).Should().Be(Color.Gray);
    }

    [Fact]
    public void GetHealthColor_MaxIsNegative_ReturnsGray()
    {
        TuiColorMapper.GetHealthColor(0, -1).Should().Be(Color.Gray);
    }

    [Fact]
    public void GetHealthColor_FullHealth_ReturnsGreen()
    {
        // 100/100 = 100% > 70%
        TuiColorMapper.GetHealthColor(100, 100).Should().Be(Color.Green);
    }

    [Fact]
    public void GetHealthColor_SeventyOnePercent_ReturnsGreen()
    {
        // 71/100 = 71% > 70%
        TuiColorMapper.GetHealthColor(71, 100).Should().Be(Color.Green);
    }

    [Fact]
    public void GetHealthColor_SeventyPercent_ReturnsBrown()
    {
        // 70/100 = 70%, which is NOT > 70%, so Brown
        TuiColorMapper.GetHealthColor(70, 100).Should().Be(Color.Brown);
    }

    [Fact]
    public void GetHealthColor_FiftyPercent_ReturnsBrown()
    {
        // 50% > 40%
        TuiColorMapper.GetHealthColor(50, 100).Should().Be(Color.Brown);
    }

    [Fact]
    public void GetHealthColor_FortyOnePercent_ReturnsBrown()
    {
        TuiColorMapper.GetHealthColor(41, 100).Should().Be(Color.Brown);
    }

    [Fact]
    public void GetHealthColor_FortyPercent_ReturnsRed()
    {
        // 40% is NOT > 40%, is > 20% → Red
        TuiColorMapper.GetHealthColor(40, 100).Should().Be(Color.Red);
    }

    [Fact]
    public void GetHealthColor_TwentyOnePercent_ReturnsRed()
    {
        TuiColorMapper.GetHealthColor(21, 100).Should().Be(Color.Red);
    }

    [Fact]
    public void GetHealthColor_TwentyPercent_ReturnsBrightRed()
    {
        // 20% is NOT > 20% → BrightRed
        TuiColorMapper.GetHealthColor(20, 100).Should().Be(Color.BrightRed);
    }

    [Fact]
    public void GetHealthColor_ZeroHP_ReturnsBrightRed()
    {
        TuiColorMapper.GetHealthColor(0, 100).Should().Be(Color.BrightRed);
    }

    // ─── GetManaColor ────────────────────────────────────────────────────────

    [Fact]
    public void GetManaColor_MaxIsZero_ReturnsGray()
    {
        TuiColorMapper.GetManaColor(0, 0).Should().Be(Color.Gray);
    }

    [Fact]
    public void GetManaColor_FullMana_ReturnsBlue()
    {
        // 100% > 50%
        TuiColorMapper.GetManaColor(100, 100).Should().Be(Color.Blue);
    }

    [Fact]
    public void GetManaColor_FiftyOnePercent_ReturnsBlue()
    {
        TuiColorMapper.GetManaColor(51, 100).Should().Be(Color.Blue);
    }

    [Fact]
    public void GetManaColor_FiftyPercent_ReturnsCyan()
    {
        // 50% NOT > 50%, > 20% → Cyan
        TuiColorMapper.GetManaColor(50, 100).Should().Be(Color.Cyan);
    }

    [Fact]
    public void GetManaColor_TwentyOnePercent_ReturnsCyan()
    {
        TuiColorMapper.GetManaColor(21, 100).Should().Be(Color.Cyan);
    }

    [Fact]
    public void GetManaColor_TwentyPercent_ReturnsGray()
    {
        // 20% NOT > 20% → Gray
        TuiColorMapper.GetManaColor(20, 100).Should().Be(Color.Gray);
    }

    [Fact]
    public void GetManaColor_ZeroMana_ReturnsGray()
    {
        TuiColorMapper.GetManaColor(0, 100).Should().Be(Color.Gray);
    }

    // ─── GetRoomTypeColor ────────────────────────────────────────────────────

    [Theory]
    [InlineData(RoomType.Dark, Color.Gray)]
    [InlineData(RoomType.Mossy, Color.Green)]
    [InlineData(RoomType.Flooded, Color.Blue)]
    [InlineData(RoomType.Scorched, Color.Brown)]
    [InlineData(RoomType.Ancient, Color.Magenta)]
    [InlineData(RoomType.ForgottenShrine, Color.Cyan)]
    [InlineData(RoomType.PetrifiedLibrary, Color.Cyan)]
    [InlineData(RoomType.ContestedArmory, Color.Brown)]
    [InlineData(RoomType.Standard, Color.White)]
    [InlineData(RoomType.TrapRoom, Color.White)]
    public void GetRoomTypeColor_ReturnsExpectedColor(RoomType type, Color expected)
    {
        TuiColorMapper.GetRoomTypeColor(type).Should().Be(expected);
    }

    // ─── GetItemTierColor ────────────────────────────────────────────────────

    [Theory]
    [InlineData(ItemTier.Common, Color.White)]
    [InlineData(ItemTier.Uncommon, Color.Green)]
    [InlineData(ItemTier.Rare, Color.BrightCyan)]
    [InlineData(ItemTier.Epic, Color.Magenta)]
    [InlineData(ItemTier.Legendary, Color.Brown)]
    public void GetItemTierColor_ReturnsExpectedColor(ItemTier tier, Color expected)
    {
        TuiColorMapper.GetItemTierColor(tier).Should().Be(expected);
    }
}
