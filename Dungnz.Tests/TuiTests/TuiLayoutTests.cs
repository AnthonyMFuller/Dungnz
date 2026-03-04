using Dungnz.Display.Tui;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for TuiLayout verifying the layout structure and panel operations
/// without requiring Terminal.Gui Application.Init().
/// </summary>
public class TuiLayoutTests
{
    [Fact]
    public void Constructor_InitializesAllPanels()
    {
        // Act
        var layout = new TuiLayout();

        // Assert
        layout.Should().NotBeNull();
        layout.MainWindow.Should().NotBeNull();
        layout.MapPanel.Should().NotBeNull();
        layout.StatsPanel.Should().NotBeNull();
        layout.ContentPanel.Should().NotBeNull();
        layout.MessageLogPanel.Should().NotBeNull();
        layout.CommandInput.Should().NotBeNull();
    }

    [Fact]
    public void MapPanel_HasCorrectTitle()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        layout.MapPanel.Title.ToString().Should().Contain("Map");
    }

    [Fact]
    public void StatsPanel_HasCorrectTitle()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        layout.StatsPanel.Title.ToString().Should().Contain("Stats");
    }

    [Fact]
    public void ContentPanel_IsReadOnly()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        layout.ContentPanel.ReadOnly.Should().BeTrue("content panel should be read-only");
    }

    [Fact]
    public void MessageLogPanel_IsReadOnly()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        layout.MessageLogPanel.ReadOnly.Should().BeTrue("message log panel should be read-only");
    }

    [Fact]
    public void ContentPanel_HasWordWrapEnabled()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        layout.ContentPanel.WordWrap.Should().BeTrue("content panel should have word wrap");
    }

    [Fact]
    public void MessageLogPanel_HasWordWrapEnabled()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        layout.MessageLogPanel.WordWrap.Should().BeTrue("message log should have word wrap");
    }

    [Fact]
    public void AppendContent_AppendsTextToContentPanel()
    {
        // Arrange
        var layout = new TuiLayout();
        layout.SetContent("Initial");

        // Act
        layout.AppendContent(" Added");

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("Initial Added");
    }

    [Fact]
    public void SetContent_ReplacesContentPanelText()
    {
        // Arrange
        var layout = new TuiLayout();
        layout.SetContent("Old content");

        // Act
        layout.SetContent("New content");

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("New content");
    }

    [Fact]
    public void AppendLog_AppendsLineToMessageLog()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        layout.AppendLog("First message");
        layout.AppendLog("Second message");

        // Assert
        var logText = layout.MessageLogPanel.Text.ToString();
        logText.Should().Contain("First message");
        logText.Should().Contain("Second message");
    }

    [Fact]
    public void AppendLog_AddsTimestampToMessages()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        layout.AppendLog("Message 1");
        layout.AppendLog("Message 2");

        // Assert
        var logText = layout.MessageLogPanel.Text.ToString();
        logText.Should().Contain("Message 1");
        logText.Should().Contain("Message 2");
        // AppendLog adds timestamps, so messages won't be plain text
        logText.Length.Should().BeGreaterThan("Message 1\nMessage 2\n".Length);
    }

    [Fact]
    public void SetContent_CanHandleEmptyString()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        layout.SetContent("");

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("");
    }

    [Fact]
    public void AppendContent_CanHandleEmptyString()
    {
        // Arrange
        var layout = new TuiLayout();
        layout.SetContent("Content");

        // Act
        layout.AppendContent("");

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("Content");
    }

    [Fact]
    public void AppendLog_CanHandleEmptyString()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        layout.AppendLog("");

        // Assert
        // AppendLog adds timestamp and prefix, so it won't be just "\n"
        layout.MessageLogPanel.Text.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void SetContent_CanHandleMultilineText()
    {
        // Arrange
        var layout = new TuiLayout();
        var multiline = "Line 1\nLine 2\nLine 3";

        // Act
        layout.SetContent(multiline);

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be(multiline);
    }

    [Fact]
    public void AppendContent_CanHandleMultilineText()
    {
        // Arrange
        var layout = new TuiLayout();
        layout.SetContent("Start\n");
        var multiline = "Line 1\nLine 2";

        // Act
        layout.AppendContent(multiline);

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("Start\nLine 1\nLine 2");
    }

    [Fact]
    public void SetContent_ClearsExistingContent()
    {
        // Arrange
        var layout = new TuiLayout();
        layout.SetContent("Old content");
        layout.AppendContent(" more content");

        // Act
        layout.SetContent("Fresh start");

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("Fresh start");
    }

    [Fact]
    public void SetMap_UpdatesMapPanel()
    {
        // Arrange
        var layout = new TuiLayout();
        var mapText = "[@] - You are here\n[E] - Enemy\n[X] - Exit";

        // Act
        Action act = () => layout.SetMap(mapText);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetStats_UpdatesStatsPanel()
    {
        // Arrange
        var layout = new TuiLayout();
        var statsText = "HP: 100/100\nMP: 50/50\nLevel: 5";

        // Act
        Action act = () => layout.SetStats(statsText);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetMap_CanHandleEmptyString()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        Action act = () => layout.SetMap("");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetStats_CanHandleEmptyString()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        Action act = () => layout.SetStats("");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AppendContent_WithMultipleCalls_AppendsInOrder()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        layout.SetContent("");
        layout.AppendContent("First");
        layout.AppendContent(" Second");
        layout.AppendContent(" Third");

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be("First Second Third");
    }

    [Fact]
    public void AppendLog_WithMultipleCalls_AppendsInOrder()
    {
        // Arrange
        var layout = new TuiLayout();

        // Act
        layout.AppendLog("A");
        layout.AppendLog("B");
        layout.AppendLog("C");

        // Assert
        var logText = layout.MessageLogPanel.Text.ToString();
        logText.Should().Contain("A");
        logText.Should().Contain("B");
        logText.Should().Contain("C");
        // Messages should appear in order
        logText.IndexOf("A").Should().BeLessThan(logText.IndexOf("B"));
        logText.IndexOf("B").Should().BeLessThan(logText.IndexOf("C"));
    }

    [Fact]
    public void Constructor_PanelsAreAddedToMainWindow()
    {
        // Arrange & Act
        var layout = new TuiLayout();

        // Assert
        // Check that panels are part of the main window's subviews
        var subviews = layout.MainWindow.Subviews;
        subviews.Should().Contain(layout.MapPanel);
        subviews.Should().Contain(layout.StatsPanel);
    }

    [Fact]
    public void SetContent_CanHandleSpecialCharacters()
    {
        // Arrange
        var layout = new TuiLayout();
        var specialText = "⚔ 🗡 💀 ✨ Special characters!";

        // Act
        layout.SetContent(specialText);

        // Assert
        layout.ContentPanel.Text.ToString().Should().Be(specialText);
    }

    [Fact]
    public void AppendLog_CanHandleSpecialCharacters()
    {
        // Arrange
        var layout = new TuiLayout();
        var specialText = "⚔ Attack! 💀 Enemy defeated!";

        // Act
        layout.AppendLog(specialText);

        // Assert
        layout.MessageLogPanel.Text.ToString().Should().Contain(specialText);
    }
}
