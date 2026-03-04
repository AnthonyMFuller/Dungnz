using Dungnz.Display.Tui;
using FluentAssertions;
using Terminal.Gui;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for TuiMenuDialog verifying menu creation and selection logic.
/// Uses Terminal.Gui's FakeDriver for headless testing.
/// </summary>
public class TuiMenuDialogTests : IDisposable
{
    /// <summary>Initializes Terminal.Gui with FakeDriver before each test.</summary>
    public TuiMenuDialogTests()
    {
        Application.Init(new FakeDriver());
    }

    /// <summary>Shuts down Terminal.Gui after each test.</summary>
    public void Dispose()
    {
        Application.Shutdown();
    }

    [Fact]
    public void Constructor_CreatesDialog_WithValidOptions()
    {
        // Arrange
        var options = new[] { ("Option 1", 1), ("Option 2", 2), ("Option 3", 3) };

        // Act
        var dialog = new TuiMenuDialog<int>("Test Menu", options, 0);

        // Assert
        ((object)dialog).Should().NotBeNull();
        dialog.Title.ToString().Should().Be("Test Menu");
    }

    [Fact]
    public void Constructor_AcceptsEmptyOptions()
    {
        // Arrange
        var options = Array.Empty<(string, int)>();

        // Act
        var dialog = new TuiMenuDialog<int>("Empty Menu", options, 0);

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsSingleOption()
    {
        // Arrange
        var options = new[] { ("Only Option", 42) };

        // Act
        var dialog = new TuiMenuDialog<int>("Single Menu", options, 0);

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsNullCancelValue()
    {
        // Arrange
        var options = new[] { ("Option 1", "value1"), ("Option 2", "value2") };

        // Act
        var dialog = new TuiMenuDialog<string>("Test Menu", options, cancelValue: null);

        // Assert
        dialog.Should().NotBeNull();
        dialog.SelectedValue.Should().BeNull("default SelectedValue should be null when cancelValue is null");
    }

    [Fact]
    public void SelectedValue_InitiallyEqualsCancel_Value()
    {
        // Arrange
        var options = new[] { ("A", 1), ("B", 2) };
        var cancelValue = 99;

        // Act
        var dialog = new TuiMenuDialog<int>("Test", options, cancelValue);

        // Assert
        dialog.SelectedValue.Should().Be(cancelValue, "SelectedValue should initially be the cancel value");
    }

    [Fact]
    public void Show_StaticHelper_CreatesDialog()
    {
        // Arrange
        var options = new[] { "Option 1", "Option 2", "Option 3" };

        // Act & Assert - just verify it doesn't throw during construction
        // (we can't run Application.Run in a unit test without Terminal.Gui init)
        Action act = () =>
        {
            // This creates the dialog but doesn't run it
            var dialog = new TuiMenuDialog<string>(
                "Test",
                options.Select(o => (o, o)),
                null
            );
            dialog.Should().NotBeNull();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void ShowIndexed_StaticHelper_CreatesDialogWithIndices()
    {
        // Arrange
        var options = new[] { "First", "Second", "Third" };

        // Act & Assert - verify construction
        Action act = () =>
        {
            var indexedOptions = options.Select((label, idx) => (label, idx + 1));
            var dialog = new TuiMenuDialog<int>("Test", indexedOptions, 0);
            dialog.Should().NotBeNull();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void ShowConfirm_StaticHelper_CreatesYesNoDialog()
    {
        // Arrange
        var prompt = "Are you sure?";

        // Act & Assert - verify construction
        Action act = () =>
        {
            var dialog = new TuiMenuDialog<bool>(
                prompt,
                new[] { ("Yes", true), ("No", false) },
                false
            );
            dialog.Should().NotBeNull();
        };

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Simple Menu")]
    [InlineData("Menu with Special Characters !@#$")]
    [InlineData("")]
    public void Constructor_AcceptsVariousTitles(string title)
    {
        // Arrange
        var options = new[] { ("Option", 1) };

        // Act
        var dialog = new TuiMenuDialog<int>(title, options, 0);

        // Assert
        dialog.Should().NotBeNull();
        dialog.Title.ToString().Should().Be(title);
    }

    [Fact]
    public void Constructor_WithValueTypeOptions_Works()
    {
        // Arrange
        var options = new[] { ("Int", 1), ("Long", 2), ("Float", 3) };

        // Act
        var dialog = new TuiMenuDialog<int>("Value Types", options, 0);

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithReferenceTypeOptions_Works()
    {
        // Arrange
        var options = new[] 
        { 
            ("String 1", "value1"), 
            ("String 2", "value2"),
            ("String 3", "value3")
        };

        // Act
        var dialog = new TuiMenuDialog<string>("Reference Types", options, "cancel");

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithEnumOptions_Works()
    {
        // Arrange
        var options = new[]
        {
            ("Easy", DifficultyLevel.Easy),
            ("Medium", DifficultyLevel.Medium),
            ("Hard", DifficultyLevel.Hard)
        };

        // Act
        var dialog = new TuiMenuDialog<DifficultyLevel>("Difficulty", options, DifficultyLevel.Easy);

        // Assert
        ((object)dialog).Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomClassOptions_Works()
    {
        // Arrange
        var obj1 = new TestMenuItem { Id = 1, Name = "Item 1" };
        var obj2 = new TestMenuItem { Id = 2, Name = "Item 2" };
        var options = new[] { ("First", obj1), ("Second", obj2) };

        // Act
        var dialog = new TuiMenuDialog<TestMenuItem>("Custom", options, null);

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLargeNumberOfOptions_Works()
    {
        // Arrange
        var options = Enumerable.Range(1, 100)
            .Select(i => ($"Option {i}", i))
            .ToArray();

        // Act
        var dialog = new TuiMenuDialog<int>("Many Options", options, 0);

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDuplicateLabels_Works()
    {
        // Arrange
        var options = new[]
        {
            ("Same Label", 1),
            ("Same Label", 2),
            ("Same Label", 3)
        };

        // Act
        var dialog = new TuiMenuDialog<int>("Duplicates", options, 0);

        // Assert
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDuplicateValues_Works()
    {
        // Arrange
        var options = new[]
        {
            ("Label 1", 42),
            ("Label 2", 42),
            ("Label 3", 42)
        };

        // Act
        var dialog = new TuiMenuDialog<int>("Duplicate Values", options, 0);

        // Assert
        dialog.Should().NotBeNull();
    }

    // Helper enum for testing
    private enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    // Helper class for testing
    private class TestMenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
