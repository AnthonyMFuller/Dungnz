using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class StatsPanelViewModelTests
{
    [Fact]
    public void DefaultStatsText_ShowsPlaceholder()
    {
        // Arrange & Act
        var vm = new StatsPanelViewModel();

        // Assert
        vm.StatsText.Should().Be("Stats will appear here");
    }

    [Fact]
    public void Update_WithPlayer_ContainsNameLevelAndHP()
    {
        // Arrange
        var vm = new StatsPanelViewModel();
        var player = new PlayerBuilder()
            .Named("Hero")
            .WithHP(80).WithMaxHP(100)
            .WithLevel(3)
            .WithAttack(15).WithDefense(5)
            .WithClass(PlayerClass.Warrior)
            .Build();

        // Act
        vm.Update(player, Array.Empty<(string, int)>());

        // Assert
        vm.StatsText.Should().Contain("Hero");
        vm.StatsText.Should().Contain("Lv 3");
        vm.StatsText.Should().Contain("80/100");
        vm.StatsText.Should().Contain("ATK 15");
        vm.StatsText.Should().Contain("DEF 5");
    }

    [Fact]
    public void Update_WithCooldowns_ShowsCooldownInfo()
    {
        // Arrange
        var vm = new StatsPanelViewModel();
        var player = new PlayerBuilder().Named("TestHero").Build();
        var cooldowns = new List<(string name, int turnsRemaining)>
        {
            ("Fireball", 3),
            ("Shield", 0)
        };

        // Act
        vm.Update(player, cooldowns);

        // Assert
        vm.StatsText.Should().Contain("CD:");
        vm.StatsText.Should().Contain("Fireball:3t");
        vm.StatsText.Should().Contain("Shield:✅");
    }

    [Fact]
    public void UpdateCombat_ProducesSameFormatAsUpdate()
    {
        // Arrange
        var vm = new StatsPanelViewModel();
        var player = new PlayerBuilder()
            .Named("CombatHero")
            .WithHP(50).WithMaxHP(100)
            .WithAttack(20).WithDefense(8)
            .Build();
        var cooldowns = Array.Empty<(string, int)>();

        // Act
        vm.UpdateCombat(player, cooldowns);

        // Assert — same format as exploration
        vm.StatsText.Should().Contain("CombatHero");
        vm.StatsText.Should().Contain("50/100");
        vm.StatsText.Should().Contain("ATK 20");
    }

    [Fact]
    public void Update_PropertyChanged_FiresNotification()
    {
        // Arrange
        var vm = new StatsPanelViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);
        var player = new PlayerBuilder().Build();

        // Act
        vm.Update(player, Array.Empty<(string, int)>());

        // Assert
        changedProperties.Should().Contain("StatsText");
    }

    [Fact]
    public void Update_WithMana_ShowsManaBar()
    {
        // Arrange
        var vm = new StatsPanelViewModel();
        var player = new PlayerBuilder()
            .WithMana(20).WithMaxMana(30)
            .WithClass(PlayerClass.Mage)
            .Build();

        // Act
        vm.Update(player, Array.Empty<(string, int)>());

        // Assert
        vm.StatsText.Should().Contain("MP");
        vm.StatsText.Should().Contain("20/30");
    }
}
