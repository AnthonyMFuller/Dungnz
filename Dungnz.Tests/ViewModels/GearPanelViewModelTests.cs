using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class GearPanelViewModelTests
{
    [Fact]
    public void DefaultGearText_ShowsPlaceholder()
    {
        // Arrange & Act
        var vm = new GearPanelViewModel();

        // Assert
        vm.GearText.Should().Be("Gear will appear here");
    }

    [Fact]
    public void Update_NoEquipment_ShowsEmptySlots()
    {
        // Arrange
        var vm = new GearPanelViewModel();
        var player = new PlayerBuilder().Build();

        // Act
        vm.Update(player);

        // Assert
        vm.GearText.Should().Contain("(empty)");
        vm.GearText.Should().Contain("Weapon");
    }

    [Fact]
    public void Update_WithWeapon_ShowsWeaponName()
    {
        // Arrange
        var vm = new GearPanelViewModel();
        var sword = new ItemBuilder()
            .Named("Iron Sword")
            .OfType(ItemType.Weapon)
            .WithDamage(8)
            .Build();
        var player = new PlayerBuilder().WithWeapon(sword).Build();

        // Act
        vm.Update(player);

        // Assert
        vm.GearText.Should().Contain("Iron Sword");
        vm.GearText.Should().Contain("+8 ATK");
    }

    [Fact]
    public void ShowEnemyStats_DisplaysEnemyInfo()
    {
        // Arrange
        var vm = new GearPanelViewModel();
        var enemy = new EnemyBuilder()
            .Named("Dark Goblin")
            .WithHP(30)
            .WithAttack(12)
            .WithDefense(4)
            .Build();
        var effects = new List<ActiveEffect>();

        // Act
        vm.ShowEnemyStats(enemy, effects);

        // Assert
        vm.GearText.Should().Contain("Dark Goblin");
        vm.GearText.Should().Contain("30/30");
        vm.GearText.Should().Contain("ATK 12");
        vm.GearText.Should().Contain("DEF 4");
    }

    [Fact]
    public void ShowEnemyStats_WithEffects_ShowsStatusEffects()
    {
        // Arrange
        var vm = new GearPanelViewModel();
        var enemy = new EnemyBuilder().Named("Zombie").WithHP(40).Build();
        var effects = new List<ActiveEffect>
        {
            new(StatusEffect.Poison, 3),
            new(StatusEffect.Burn, 2)
        };

        // Act
        vm.ShowEnemyStats(enemy, effects);

        // Assert
        vm.GearText.Should().Contain("Poison");
        vm.GearText.Should().Contain("3t");
        vm.GearText.Should().Contain("Burn");
        vm.GearText.Should().Contain("2t");
    }

    [Fact]
    public void Update_PropertyChanged_FiresNotification()
    {
        // Arrange
        var vm = new GearPanelViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);
        var player = new PlayerBuilder().Build();

        // Act
        vm.Update(player);

        // Assert
        changedProperties.Should().Contain("GearText");
    }
}
