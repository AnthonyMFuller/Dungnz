using Dungnz.Engine;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class CommandParserTests
{
    [Theory]
    [InlineData("n", "north")]
    [InlineData("s", "south")]
    [InlineData("e", "east")]
    [InlineData("w", "west")]
    [InlineData("north", "north")]
    [InlineData("south", "south")]
    [InlineData("east", "east")]
    [InlineData("west", "west")]
    public void SingleDirectionInput_ReturnsGoCommand(string input, string expectedArg)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Go);
        cmd.Argument.Should().Be(expectedArg);
    }

    [Fact]
    public void GoNorth_ReturnsGoNorth()
    {
        var cmd = CommandParser.Parse("go north");
        cmd.Type.Should().Be(CommandType.Go);
        cmd.Argument.Should().Be("north");
    }

    [Theory]
    [InlineData("look")]
    [InlineData("l")]
    public void LookCommands_ReturnLook(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Look);
    }

    [Theory]
    [InlineData("examine sword", "sword")]
    [InlineData("ex sword", "sword")]
    public void ExamineCommands_ReturnExamineWithArgument(string input, string expectedArg)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Examine);
        cmd.Argument.Should().Be(expectedArg);
    }

    [Theory]
    [InlineData("take potion", "potion")]
    [InlineData("get potion", "potion")]
    public void TakeCommands_ReturnTakeWithArgument(string input, string expectedArg)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Take);
        cmd.Argument.Should().Be(expectedArg);
    }

    [Fact]
    public void UsePotion_ReturnsUse()
    {
        var cmd = CommandParser.Parse("use potion");
        cmd.Type.Should().Be(CommandType.Use);
        cmd.Argument.Should().Be("potion");
    }

    [Theory]
    [InlineData("inventory")]
    [InlineData("inv")]
    [InlineData("i")]
    public void InventoryCommands_ReturnInventory(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Inventory);
    }

    [Theory]
    [InlineData("stats")]
    [InlineData("status")]
    public void StatsCommands_ReturnStats(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Stats);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("?")]
    [InlineData("h")]
    public void HelpCommands_ReturnHelp(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Help);
    }

    [Theory]
    [InlineData("quit")]
    [InlineData("exit")]
    [InlineData("q")]
    public void QuitCommands_ReturnQuit(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Quit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foobar")]
    [InlineData("xyzzy")]
    public void UnknownOrEmpty_ReturnUnknown(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Unknown);
    }

    [Theory]
    [InlineData("NORTH", "north")]
    [InlineData("SOUTH", "south")]
    [InlineData("EAST", "east")]
    [InlineData("WEST", "west")]
    public void CaseInsensitiveDirections(string input, string expectedArg)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Go);
        cmd.Argument.Should().Be(expectedArg);
    }

    [Theory]
    [InlineData("HELP")]
    [InlineData("Help")]
    public void CaseInsensitiveHelp(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Help);
    }

    [Theory]
    [InlineData("INV")]
    [InlineData("Inv")]
    public void CaseInsensitiveInventory(string input)
    {
        var cmd = CommandParser.Parse(input);
        cmd.Type.Should().Be(CommandType.Inventory);
    }
}
