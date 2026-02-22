using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for ShowEquipmentComparison box alignment (Issue #221).
///
/// The box must render at a consistent visual width on every row regardless of
/// whether coloured delta strings (e.g. green "(+8)") are present.  ANSI escape
/// codes are invisible to the terminal, so the visual width is the length of the
/// line AFTER stripping them.  A line with an ANSI-coloured delta must have the
/// same stripped length as a line without one.
/// </summary>
[Collection("console-output")]
public class ShowEquipmentComparisonAlignmentTests : IDisposable
{
    private readonly StringWriter _consoleCapture;
    private readonly TextWriter _originalOut;

    public ShowEquipmentComparisonAlignmentTests()
    {
        _originalOut = Console.Out;
        _consoleCapture = new StringWriter();
        Console.SetOut(_consoleCapture);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleCapture.Dispose();
    }

    private IEnumerable<string> CapturedLines =>
        _consoleCapture.ToString()
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Expected box width taken from the header/footer border line.</summary>
    private static int BoxWidth(IEnumerable<string> lines)
    {
        var border = lines.FirstOrDefault(l => l.StartsWith("╔") && l.EndsWith("╗"));
        border.Should().NotBeNull("the comparison box must have a top border line");
        return border!.Length;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowEquipmentComparison_AllBoxLines_HaveConsistentVisualWidth_WhenDeltasAreColoured()
    {
        // Arrange — equip an item that produces non-zero attack AND defense deltas
        // so both stat rows contain ANSI-coloured delta strings.
        var player = new Player { Name = "Tester", Attack = 10, Defense = 5 };
        var oldItem = new Item
        {
            Name = "Iron Sword",
            Type = ItemType.Weapon,
            AttackBonus = 2,
            DefenseBonus = 0,
            IsEquippable = true
        };
        var newItem = new Item
        {
            Name = "Steel Sword",
            Type = ItemType.Weapon,
            AttackBonus = 10,   // delta = +8
            DefenseBonus = 3,   // delta = +3
            IsEquippable = true
        };
        player.Inventory.Add(oldItem);
        player.EquipItem(oldItem);

        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowEquipmentComparison(player, oldItem, newItem);

        // Assert — every line that is a box content row (starts with ║) must have
        // the same visual (ANSI-stripped) length as the header border row.
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var contentLines = lines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the box must contain content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            stripped.Length.Should().Be(expectedWidth,
                $"content line '{stripped}' should have the same visual width as the border");
        }
    }

    [Fact]
    public void ShowEquipmentComparison_RightBorderChar_AppearsAtConsistentColumn_WhenOnlyAttackChanges()
    {
        // Arrange — only attack delta is non-zero; defense row has no delta.
        // This exercises the mixed case: one stat row with coloured delta, one without.
        var player = new Player { Name = "Tester", Attack = 10, Defense = 5 };
        var oldItem = new Item
        {
            Name = "Dagger",
            Type = ItemType.Weapon,
            AttackBonus = 2,
            DefenseBonus = 0,
            IsEquippable = true
        };
        var newItem = new Item
        {
            Name = "Longsword",
            Type = ItemType.Weapon,
            AttackBonus = 10,   // delta = +8  (coloured)
            DefenseBonus = 0,   // delta = 0   (no colour)
            IsEquippable = true
        };
        player.Inventory.Add(oldItem);
        player.EquipItem(oldItem);

        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowEquipmentComparison(player, oldItem, newItem);

        // Assert — both Attack and Defense content lines must end with ║ at the
        // same column position after stripping ANSI.
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var columnPositions = lines
            .Where(l => l.StartsWith("║"))
            .Select(l => ColorCodes.StripAnsiCodes(l).Length)
            .Distinct()
            .ToList();

        columnPositions.Should().HaveCount(1,
            "the right ║ border must appear at the same column regardless of ANSI delta colouring (fix for #221)");
        columnPositions.Single().Should().Be(expectedWidth);
    }
}
