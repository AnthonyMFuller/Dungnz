using Dungnz.Display.Avalonia.ViewModels;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class ContentPanelViewModelTests
{
    [Fact]
    public void AppendMessage_SingleMessage_AddsToContentLines()
    {
        // Arrange
        var vm = new ContentPanelViewModel();

        // Act
        vm.AppendMessage("Hello, adventurer!");

        // Assert
        vm.ContentLines.Should().ContainSingle()
            .Which.Should().Be("Hello, adventurer!");
    }

    [Fact]
    public void AppendMessage_ExceedsMaxLines_TrimsOldest()
    {
        // Arrange
        var vm = new ContentPanelViewModel();

        // Act — MaxContentLines is 50, so add 52 messages
        for (int i = 0; i < 52; i++)
            vm.AppendMessage($"Line {i}");

        // Assert — oldest two trimmed, 50 remain
        vm.ContentLines.Should().HaveCount(50);
        vm.ContentLines[0].Should().Be("Line 2");
        vm.ContentLines[^1].Should().Be("Line 51");
    }

    [Fact]
    public void Clear_RemovesAllLines()
    {
        // Arrange
        var vm = new ContentPanelViewModel();
        vm.AppendMessage("First");
        vm.AppendMessage("Second");

        // Act
        vm.Clear();

        // Assert
        vm.ContentLines.Should().BeEmpty();
    }

    [Fact]
    public void SetContent_ReplacesLinesAndHeader()
    {
        // Arrange
        var vm = new ContentPanelViewModel();
        vm.AppendMessage("Old line");

        // Act
        vm.SetContent("Line A\nLine B\nLine C", "Combat");

        // Assert
        vm.HeaderText.Should().Be("Combat");
        vm.ContentLines.Should().HaveCount(3);
        vm.ContentLines[0].Should().Be("Line A");
        vm.ContentLines[2].Should().Be("Line C");
    }

    [Fact]
    public void SetContent_EmptyContent_ClearsLinesButSetsHeader()
    {
        // Arrange
        var vm = new ContentPanelViewModel();
        vm.AppendMessage("Old line");

        // Act
        vm.SetContent("", "Empty Room");

        // Assert
        vm.HeaderText.Should().Be("Empty Room");
        vm.ContentLines.Should().BeEmpty();
    }

    [Fact]
    public void HeaderText_DefaultValue_IsAdventure()
    {
        // Arrange & Act
        var vm = new ContentPanelViewModel();

        // Assert
        vm.HeaderText.Should().Be("Adventure");
    }

    [Fact]
    public void HeaderText_PropertyChanged_FiresNotification()
    {
        // Arrange
        var vm = new ContentPanelViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.SetContent("test", "New Header");

        // Assert
        changedProperties.Should().Contain("HeaderText");
    }
}
