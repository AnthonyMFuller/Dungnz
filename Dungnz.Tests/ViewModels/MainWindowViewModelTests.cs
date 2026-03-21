using Dungnz.Display.Avalonia.ViewModels;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesAllPanels()
    {
        // Arrange & Act
        var vm = new MainWindowViewModel();

        // Assert
        vm.Map.Should().NotBeNull();
        vm.Stats.Should().NotBeNull();
        vm.Content.Should().NotBeNull();
        vm.Gear.Should().NotBeNull();
        vm.Log.Should().NotBeNull();
        vm.Input.Should().NotBeNull();
    }

    [Fact]
    public void Panels_AreDistinctInstances()
    {
        // Arrange & Act
        var vm1 = new MainWindowViewModel();
        var vm2 = new MainWindowViewModel();

        // Assert — each MainWindowViewModel creates its own panel instances
        vm1.Content.Should().NotBeSameAs(vm2.Content);
        vm1.Stats.Should().NotBeSameAs(vm2.Stats);
        vm1.Input.Should().NotBeSameAs(vm2.Input);
    }

    [Fact]
    public void Panels_CanBeUsedIndependently()
    {
        // Arrange
        var vm = new MainWindowViewModel();

        // Act — exercise each panel through the parent
        vm.Content.AppendMessage("Hello");
        vm.Log.AppendLog("Test event");
        vm.Input.CommandText = "attack";

        // Assert
        vm.Content.ContentLines.Should().ContainSingle();
        vm.Log.LogLines.Should().ContainSingle();
        vm.Input.CommandText.Should().Be("attack");
    }
}
