using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for ColorCodes methods not yet covered.</summary>
public class ColorCodesAdditionalTests
{
    // ── HealthColor ───────────────────────────────────────────────────────────

    [Fact]
    public void HealthColor_ZeroMax_ReturnsGray()
    {
        ColorCodes.HealthColor(50, 0).Should().Be(ColorCodes.Gray);
    }

    [Fact]
    public void HealthColor_Above70Percent_ReturnsGreen()
    {
        ColorCodes.HealthColor(80, 100).Should().Be(ColorCodes.Green);
    }

    [Fact]
    public void HealthColor_Between40And70_ReturnsYellow()
    {
        ColorCodes.HealthColor(55, 100).Should().Be(ColorCodes.Yellow);
    }

    [Fact]
    public void HealthColor_Between20And40_ReturnsRed()
    {
        ColorCodes.HealthColor(30, 100).Should().Be(ColorCodes.Red);
    }

    [Fact]
    public void HealthColor_Below20Percent_ReturnsBrightRed()
    {
        ColorCodes.HealthColor(10, 100).Should().Be(ColorCodes.BrightRed);
    }

    [Fact]
    public void HealthColor_ExactlyZero_ReturnsBrightRed()
    {
        ColorCodes.HealthColor(0, 100).Should().Be(ColorCodes.BrightRed);
    }

    // ── ManaColor ────────────────────────────────────────────────────────────

    [Fact]
    public void ManaColor_ZeroMax_ReturnsGray()
    {
        ColorCodes.ManaColor(10, 0).Should().Be(ColorCodes.Gray);
    }

    [Fact]
    public void ManaColor_Above50Percent_ReturnsBlue()
    {
        ColorCodes.ManaColor(60, 100).Should().Be(ColorCodes.Blue);
    }

    [Fact]
    public void ManaColor_Between20And50_ReturnsCyan()
    {
        ColorCodes.ManaColor(35, 100).Should().Be(ColorCodes.Cyan);
    }

    [Fact]
    public void ManaColor_Below20Percent_ReturnsGray()
    {
        ColorCodes.ManaColor(10, 100).Should().Be(ColorCodes.Gray);
    }

    // ── WeightColor ───────────────────────────────────────────────────────────

    [Fact]
    public void WeightColor_ZeroMax_ReturnsGray()
    {
        ColorCodes.WeightColor(10, 0).Should().Be(ColorCodes.Gray);
    }

    [Fact]
    public void WeightColor_Below80Percent_ReturnsGreen()
    {
        ColorCodes.WeightColor(70, 100).Should().Be(ColorCodes.Green);
    }

    [Fact]
    public void WeightColor_Between80And95_ReturnsYellow()
    {
        ColorCodes.WeightColor(85, 100).Should().Be(ColorCodes.Yellow);
    }

    [Fact]
    public void WeightColor_Above95Percent_ReturnsRed()
    {
        ColorCodes.WeightColor(96, 100).Should().Be(ColorCodes.Red);
    }

    // ── Colorize ──────────────────────────────────────────────────────────────

    [Fact]
    public void Colorize_WrapsTextInColorAndReset()
    {
        var result = ColorCodes.Colorize("Hello", ColorCodes.Red);
        result.Should().StartWith(ColorCodes.Red);
        result.Should().Contain("Hello");
        result.Should().EndWith(ColorCodes.Reset);
    }

    // ── ColorizeItemName ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ItemTier.Common, "\u001b[97m")]      // BrightWhite
    [InlineData(ItemTier.Uncommon, "\u001b[32m")]    // Green
    [InlineData(ItemTier.Rare, "\u001b[96m")]        // BrightCyan
    [InlineData(ItemTier.Epic, "\u001b[35m")]        // Magenta
    [InlineData(ItemTier.Legendary, "\u001b[33m")]   // Yellow
    public void ColorizeItemName_ReturnsCorrectColorForTier(ItemTier tier, string expectedColor)
    {
        var result = ColorCodes.ColorizeItemName("Test Item", tier);
        result.Should().Contain(expectedColor);
        result.Should().Contain("Test Item");
    }

    // ── GetRoomTypeColor ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(RoomType.Standard)]
    [InlineData(RoomType.Dark)]
    [InlineData(RoomType.Mossy)]
    [InlineData(RoomType.Flooded)]
    [InlineData(RoomType.Scorched)]
    [InlineData(RoomType.Ancient)]
    [InlineData(RoomType.ForgottenShrine)]
    [InlineData(RoomType.PetrifiedLibrary)]
    [InlineData(RoomType.ContestedArmory)]
    public void GetRoomTypeColor_ReturnsNonNullColorCode(RoomType type)
    {
        var color = ColorCodes.GetRoomTypeColor(type);
        color.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetRoomTypeColor_Standard_ReturnsWhite()
    {
        ColorCodes.GetRoomTypeColor(RoomType.Standard).Should().Be(ColorCodes.White);
    }

    [Fact]
    public void GetRoomTypeColor_Dark_ReturnsGray()
    {
        ColorCodes.GetRoomTypeColor(RoomType.Dark).Should().Be(ColorCodes.Gray);
    }

    [Fact]
    public void GetRoomTypeColor_Mossy_ReturnsGreen()
    {
        ColorCodes.GetRoomTypeColor(RoomType.Mossy).Should().Be(ColorCodes.Green);
    }

    [Fact]
    public void GetRoomTypeColor_Flooded_ReturnsBlue()
    {
        ColorCodes.GetRoomTypeColor(RoomType.Flooded).Should().Be(ColorCodes.Blue);
    }

    [Fact]
    public void GetRoomTypeColor_Scorched_ReturnsYellow()
    {
        ColorCodes.GetRoomTypeColor(RoomType.Scorched).Should().Be(ColorCodes.Yellow);
    }

    [Fact]
    public void GetRoomTypeColor_Ancient_ReturnsMagenta()
    {
        ColorCodes.GetRoomTypeColor(RoomType.Ancient).Should().Be(ColorCodes.Magenta);
    }

    [Fact]
    public void GetRoomTypeColor_ForgottenShrine_ReturnsCyan()
    {
        ColorCodes.GetRoomTypeColor(RoomType.ForgottenShrine).Should().Be(ColorCodes.Cyan);
    }

    [Fact]
    public void GetRoomTypeColor_PetrifiedLibrary_ReturnsCyan()
    {
        ColorCodes.GetRoomTypeColor(RoomType.PetrifiedLibrary).Should().Be(ColorCodes.Cyan);
    }

    [Fact]
    public void GetRoomTypeColor_ContestedArmory_ReturnsYellow()
    {
        ColorCodes.GetRoomTypeColor(RoomType.ContestedArmory).Should().Be(ColorCodes.Yellow);
    }

    // ── StripAnsiCodes ────────────────────────────────────────────────────────

    [Fact]
    public void StripAnsiCodes_RemovesAllAnsiSequences()
    {
        var input = $"{ColorCodes.Red}Hello{ColorCodes.Reset} {ColorCodes.Green}World{ColorCodes.Reset}";
        var result = ColorCodes.StripAnsiCodes(input);
        result.Should().Be("Hello World");
    }

    [Fact]
    public void StripAnsiCodes_PlainText_Unchanged()
    {
        var text = "No ANSI codes here";
        ColorCodes.StripAnsiCodes(text).Should().Be(text);
    }
}
