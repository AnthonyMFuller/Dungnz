using Dungnz.Display.Avalonia.ViewModels;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class InputPanelViewModelTests
{
    [Fact]
    public void DefaultState_CommandTextEmpty_InputDisabled()
    {
        // Arrange & Act
        var vm = new InputPanelViewModel();

        // Assert
        vm.CommandText.Should().BeEmpty();
        vm.PromptText.Should().Be(">");
        vm.IsInputEnabled.Should().BeFalse();
    }

    [Fact]
    public void Submit_FiresInputSubmittedEvent()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        vm.CommandText = "attack";
        string? received = null;
        vm.InputSubmitted += text => received = text;

        // Act
        vm.Submit();

        // Assert
        received.Should().Be("attack");
    }

    [Fact]
    public void Submit_ClearsCommandText()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        vm.CommandText = "look";

        // Act
        vm.Submit();

        // Assert
        vm.CommandText.Should().BeEmpty();
    }

    [Fact]
    public void Submit_DisablesInput()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        vm.IsInputEnabled = true;
        vm.CommandText = "go north";

        // Act
        vm.Submit();

        // Assert
        vm.IsInputEnabled.Should().BeFalse();
    }

    [Fact]
    public void Submit_TrimsWhitespace()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        vm.CommandText = "  attack  ";
        string? received = null;
        vm.InputSubmitted += text => received = text;

        // Act
        vm.Submit();

        // Assert
        received.Should().Be("attack");
    }

    [Fact]
    public void Submit_EmptyText_FiresWithEmptyString()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        vm.CommandText = "";
        string? received = null;
        vm.InputSubmitted += text => received = text;

        // Act
        vm.Submit();

        // Assert
        received.Should().BeEmpty();
    }

    [Fact]
    public void Submit_NoSubscriber_DoesNotThrow()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        vm.CommandText = "test";

        // Act & Assert — no subscriber means no crash
        var act = () => vm.Submit();
        act.Should().NotThrow();
    }

    [Fact]
    public void PropertyChanged_FiredOnCommandTextChange()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.CommandText = "hello";

        // Assert
        changedProperties.Should().Contain("CommandText");
    }

    [Fact]
    public void PropertyChanged_FiredOnIsInputEnabledChange()
    {
        // Arrange
        var vm = new InputPanelViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.IsInputEnabled = true;

        // Assert
        changedProperties.Should().Contain("IsInputEnabled");
    }
}
