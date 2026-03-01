using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class PlayerTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var player = new Player();
        player.HP.Should().Be(100);
        player.MaxHP.Should().Be(100);
        player.Attack.Should().Be(10);
        player.Defense.Should().Be(5);
        player.Level.Should().Be(1);
        player.Gold.Should().Be(0);
        player.XP.Should().Be(0);
    }

    [Fact]
    public void Inventory_StartsEmpty()
    {
        var player = new Player();
        player.Inventory.Should().BeEmpty();
    }

    [Fact]
    public void Stats_CanBeModified()
    {
        var player = new PlayerBuilder().WithHP(50).WithAttack(20).WithDefense(15).Build();
        player.HP.Should().Be(50);
        player.Attack.Should().Be(20);
        player.Defense.Should().Be(15);
    }

    [Fact]
    public void XP_Accumulates()
    {
        var player = new Player();
        player.XP += 50;
        player.XP += 30;
        player.XP.Should().Be(80);
    }

    [Fact]
    public void Name_CanBeSet()
    {
        var player = new PlayerBuilder().Named("Romanoff").Build();
        player.Name.Should().Be("Romanoff");
    }

    [Fact]
    public void Inventory_CanAddItems()
    {
        var sword = new ItemBuilder().Named("Sword").WithDamage(5).Build();
        var player = new PlayerBuilder().WithItem(sword).Build();
        player.Inventory.Should().ContainSingle().Which.Name.Should().Be("Sword");
    }

    [Fact]
    public void Gold_Accumulates()
    {
        var player = new Player();
        player.Gold += 25;
        player.Gold += 10;
        player.Gold.Should().Be(35);
    }
}
