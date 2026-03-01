using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Regression tests for 6 alignment bugs fixed in display methods.
/// Each test captures console output, strips ANSI codes, and verifies that all
/// content lines (║) have the same visual width as the border (╔...╗) line.
/// </summary>
[Collection("console-output")]
public class AlignmentRegressionTests : IDisposable
{
    private readonly StringWriter _consoleCapture;
    private readonly TextWriter _originalOut;

    public AlignmentRegressionTests()
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
        border.Should().NotBeNull("the box must have a top border line");
        return border!.Length;
    }

    /// <summary>
    /// Visual terminal column width of a string.
    /// BMP chars in <see cref="_wideChars"/> render as 2 columns; all others are 1.
    /// Surrogate-pair emoji (U+10000+) already contribute 2 to <see cref="string.Length"/>.
    /// </summary>
    private static readonly HashSet<char> _wideChars =
        new HashSet<char> { '⭐', '⚔', '⚡', '✅', '❌', '✨', '☠' };

    private static int VisualWidth(string s)
        => s.Length + s.Count(c => _wideChars.Contains(c));

    // -------------------------------------------------------------------------
    // Test 1: ShowItemDetail weapon box has consistent border width
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowItemDetail_WeaponWithWideIcon_AllBoxLinesHaveConsistentVisualWidth()
    {
        // Arrange — create a Weapon item (AttackBonus > 0) which triggers ⚔ icon
        var item = new Item
        {
            Name = "Steel Sword",
            Type = ItemType.Weapon,
            AttackBonus = 10,
            Weight = 5,
            IsEquippable = true
        };
        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowItemDetail(item);

        // Assert — every content line (║) must have same visual width as border after stripping ANSI
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var contentLines = lines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the box must contain content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            VisualWidth(stripped).Should().Be(expectedWidth,
                $"content line '{stripped}' should have the same visual width as the border");
        }
    }

    // -------------------------------------------------------------------------
    // Test 2: ShowShop weapon row has consistent border width
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowShop_WeaponWithWideIcon_AllBoxLinesHaveConsistentVisualWidth()
    {
        // Arrange — create a Weapon item with a price
        var item = new Item
        {
            Name = "Battle Axe",
            Type = ItemType.Weapon,
            AttackBonus = 8,
            Weight = 6,
            IsEquippable = true
        };
        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowShop(new[] { (item, 100) }, 200);

        // Assert — all content lines must have same visual length as border after stripping ANSI
        var lines = CapturedLines.ToList();
        
        // Find all box borders (there's one box per item in the shop)
        var boxStarts = lines
            .Select((line, idx) => (line, idx))
            .Where(x => x.line.StartsWith("╔") && x.line.EndsWith("╗"))
            .ToList();

        boxStarts.Should().NotBeEmpty("shop should have at least one item box");

        // Check the first item box
        var boxStartIdx = boxStarts[0].idx;
        var boxEndIdx = lines.Skip(boxStartIdx + 1).TakeWhile(l => !l.StartsWith("╚")).Count() + boxStartIdx + 1;
        var boxLines = lines.Skip(boxStartIdx).Take(boxEndIdx - boxStartIdx + 1).ToList();
        
        int expectedWidth = BoxWidth(boxLines);
        var contentLines = boxLines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the shop item box must have content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            VisualWidth(stripped).Should().Be(expectedWidth,
                $"shop item line '{stripped}' should have the same visual width as the border");
        }
    }

    // -------------------------------------------------------------------------
    // Test 3: ShowEnemyDetail elite enemy box has consistent border width
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowEnemyDetail_EliteEnemyWithWideIcon_AllBoxLinesHaveConsistentVisualWidth()
    {
        // Arrange — create an enemy with IsElite = true (triggers ⭐ icon)
        var enemy = new TestEnemy
        {
            Name = "Elite Goblin",
            HP = 100,
            MaxHP = 100,
            Attack = 15,
            Defense = 5,
            XPValue = 50,
            IsElite = true
        };
        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowEnemyDetail(enemy);

        // Assert — all content lines (║) must have same visual length as border (╔)
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var contentLines = lines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the enemy detail box must contain content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            VisualWidth(stripped).Should().Be(expectedWidth,
                $"enemy detail line '{stripped}' should have the same visual width as the border");
        }
    }

    // -------------------------------------------------------------------------
    // Test 4: ShowVictory box has consistent border width
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowVictory_AllBoxLinesHaveConsistentVisualWidth()
    {
        // Arrange — create a Player and RunStats
        var player = new Player
        {
            Name = "Hero",
            Level = 5
        };
        var stats = new RunStats
        {
            EnemiesDefeated = 25,
            GoldCollected = 500,
            ItemsFound = 10,
            TurnsTaken = 100
        };
        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowVictory(player, 3, stats);

        // Assert — all content lines (║) must have same visual length as border (╔)
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var contentLines = lines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the victory box must contain content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            VisualWidth(stripped).Should().Be(expectedWidth,
                $"victory line '{stripped}' should have the same visual width as the border");
        }
    }

    // -------------------------------------------------------------------------
    // Test 5: ShowGameOver box has consistent border width
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowGameOver_AllBoxLinesHaveConsistentVisualWidth()
    {
        // Arrange — create a Player and RunStats
        var player = new Player
        {
            Name = "Fallen Hero",
            Level = 3
        };
        var stats = new RunStats
        {
            EnemiesDefeated = 15,
            FloorsVisited = 2,
            TurnsTaken = 75
        };
        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowGameOver(player, "killed by a Goblin", stats);

        // Assert — all content lines (║) must have same visual length as border (╔)
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var contentLines = lines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the game over box must contain content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            VisualWidth(stripped).Should().Be(expectedWidth,
                $"game over line '{stripped}' should have the same visual width as the border");
        }
    }

    // -------------------------------------------------------------------------
    // Test 6: ShowItemDetail non-weapon (armor, accessory) box also aligns
    // -------------------------------------------------------------------------

    [Fact]
    public void ShowItemDetail_ArmorWithSurrogatePairIcon_AllBoxLinesHaveConsistentVisualWidth()
    {
        // Arrange — test with an Armor item (🛡 = surrogate pair, already 2 chars)
        var item = new Item
        {
            Name = "Iron Shield",
            Type = ItemType.Armor,
            DefenseBonus = 8,
            Weight = 4,
            IsEquippable = true
        };
        var svc = new ConsoleDisplayService();

        // Act
        svc.ShowItemDetail(item);

        // Assert — verify alignment still correct after Fix 1
        var lines = CapturedLines.ToList();
        int expectedWidth = BoxWidth(lines);

        var contentLines = lines.Where(l => l.StartsWith("║")).ToList();
        contentLines.Should().NotBeEmpty("the box must contain content lines");

        foreach (var line in contentLines)
        {
            var stripped = ColorCodes.StripAnsiCodes(line);
            VisualWidth(stripped).Should().Be(expectedWidth,
                $"armor item line '{stripped}' should have the same visual width as the border");
        }
    }
}

/// <summary>Simple test enemy class for alignment testing.</summary>
internal class TestEnemy : Enemy
{
    // No constructor needed — properties can be set via initializer
}
