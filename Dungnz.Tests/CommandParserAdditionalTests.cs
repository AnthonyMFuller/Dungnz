using Dungnz.Engine;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for CommandParser commands not covered by the existing CommandParserTests.</summary>
public class CommandParserAdditionalTests
{
    // ── Equip/Unequip/Equipment ───────────────────────────────────────────────

    [Fact]
    public void Parse_Equip_ReturnsEquipCommand()
    {
        var cmd = CommandParser.Parse("equip iron sword");
        cmd.Type.Should().Be(CommandType.Equip);
        cmd.Argument.Should().Be("iron sword");
    }

    [Fact]
    public void Parse_Unequip_ReturnsUnequipCommand()
    {
        var cmd = CommandParser.Parse("unequip weapon");
        cmd.Type.Should().Be(CommandType.Unequip);
        cmd.Argument.Should().Be("weapon");
    }

    [Theory]
    [InlineData("equipment")]
    [InlineData("gear")]
    public void Parse_Equipment_ReturnsEquipmentCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Equipment);
    }

    // ── Save/Load/ListSaves ───────────────────────────────────────────────────

    [Fact]
    public void Parse_Save_ReturnsSaveCommand()
    {
        var cmd = CommandParser.Parse("save slot1");
        cmd.Type.Should().Be(CommandType.Save);
        cmd.Argument.Should().Be("slot1");
    }

    [Fact]
    public void Parse_Load_ReturnsLoadCommand()
    {
        var cmd = CommandParser.Parse("load slot1");
        cmd.Type.Should().Be(CommandType.Load);
        cmd.Argument.Should().Be("slot1");
    }

    [Theory]
    [InlineData("list")]
    [InlineData("saves")]
    [InlineData("listsaves")]
    public void Parse_ListSaves_ReturnsListSavesCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.ListSaves);
    }

    // ── Navigation aliases ────────────────────────────────────────────────────

    [Theory]
    [InlineData("descend")]
    [InlineData("down")]
    public void Parse_Descend_ReturnsDescendCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Descend);
    }

    [Theory]
    [InlineData("map")]
    [InlineData("m")]
    public void Parse_Map_ReturnsMapCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Map);
    }

    // ── Shop/Sell ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("shop")]
    [InlineData("buy")]
    public void Parse_Shop_ReturnsShopCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Shop);
    }

    [Fact]
    public void Parse_Sell_ReturnsSellCommand()
    {
        var cmd = CommandParser.Parse("sell potion");
        cmd.Type.Should().Be(CommandType.Sell);
    }

    // ── Prestige ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("prestige")]
    [InlineData("p")]
    public void Parse_Prestige_ReturnsPrestigeCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Prestige);
    }

    // ── Skills/Learn ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("skills")]
    [InlineData("skill")]
    public void Parse_Skills_ReturnsSkillsCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Skills);
    }

    [Fact]
    public void Parse_Learn_ReturnsLearnCommandWithArgument()
    {
        var cmd = CommandParser.Parse("learn ironskin");
        cmd.Type.Should().Be(CommandType.Learn);
        cmd.Argument.Should().Be("ironskin");
    }

    // ── Craft ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Craft_ReturnsCraftCommandWithArgument()
    {
        var cmd = CommandParser.Parse("craft health potion");
        cmd.Type.Should().Be(CommandType.Craft);
        cmd.Argument.Should().Be("health potion");
    }

    // ── Leaderboard ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("leaderboard")]
    [InlineData("lb")]
    [InlineData("scores")]
    public void Parse_Leaderboard_ReturnsLeaderboardCommand(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Leaderboard);
    }

    // ── Case insensitive ──────────────────────────────────────────────────────

    [Fact]
    public void Parse_UppercaseInput_IsNormalized()
    {
        var cmd = CommandParser.Parse("EQUIP SWORD");
        cmd.Type.Should().Be(CommandType.Equip);
    }

    [Fact]
    public void Parse_MixedCaseInput_IsNormalized()
    {
        var cmd = CommandParser.Parse("Descend");
        cmd.Type.Should().Be(CommandType.Descend);
    }
}
