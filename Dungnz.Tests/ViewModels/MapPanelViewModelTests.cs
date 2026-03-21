using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class MapPanelViewModelTests
{
    [Fact]
    public void DefaultMapText_ShowsPlaceholder()
    {
        // Arrange & Act
        var vm = new MapPanelViewModel();

        // Assert
        vm.MapText.Should().Be("Map will appear here");
        vm.CurrentFloor.Should().Be(1);
    }

    [Fact]
    public void Update_SetsFloorNumber()
    {
        // Arrange
        var vm = new MapPanelViewModel();
        var room = new RoomBuilder().Named("Entrance Hall").Build();

        // Act
        vm.Update(room, 3);

        // Assert
        vm.CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void Update_SetsMapText_NonEmpty()
    {
        // Arrange
        var vm = new MapPanelViewModel();
        var room = new RoomBuilder().Named("Dark Cave").Build();

        // Act
        vm.Update(room, 1);

        // Assert — MapRenderer.BuildPlainTextMap should produce non-empty output
        vm.MapText.Should().NotBeNullOrEmpty();
        vm.MapText.Should().NotBe("Map will appear here");
    }

    [Fact]
    public void Update_PropertyChanged_FiresForBothProperties()
    {
        // Arrange
        var vm = new MapPanelViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);
        var room = new RoomBuilder().Build();

        // Act
        vm.Update(room, 2);

        // Assert
        changedProperties.Should().Contain("CurrentFloor");
        changedProperties.Should().Contain("MapText");
    }
}
