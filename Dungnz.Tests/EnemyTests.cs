using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class EnemyTests
{
    [Fact]
    public void Goblin_HasCorrectStats()
    {
        var g = new Goblin();
        g.Name.Should().Be("Goblin");
        g.HP.Should().Be(20);
        g.MaxHP.Should().Be(20);
        g.Attack.Should().Be(8);
        g.Defense.Should().Be(2);
        g.XPValue.Should().Be(15);
        g.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void Skeleton_HasCorrectStats()
    {
        var s = new Skeleton();
        s.Name.Should().Be("Skeleton");
        s.HP.Should().Be(30);
        s.MaxHP.Should().Be(30);
        s.Attack.Should().Be(12);
        s.Defense.Should().Be(5);
        s.XPValue.Should().Be(25);
        s.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void Troll_HasCorrectStats()
    {
        var t = new Troll();
        t.Name.Should().Be("Troll");
        t.HP.Should().Be(60);
        t.MaxHP.Should().Be(60);
        t.Attack.Should().Be(10);
        t.Defense.Should().Be(8);
        t.XPValue.Should().Be(40);
        t.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void DarkKnight_HasCorrectStats()
    {
        var dk = new DarkKnight();
        dk.Name.Should().Be("Dark Knight");
        dk.HP.Should().Be(45);
        dk.MaxHP.Should().Be(45);
        dk.Attack.Should().Be(18);
        dk.Defense.Should().Be(12);
        dk.XPValue.Should().Be(55);
        dk.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void DungeonBoss_HasCorrectStats()
    {
        var boss = new DungeonBoss();
        boss.Name.Should().Be("Dungeon Boss");
        boss.HP.Should().Be(100);
        boss.MaxHP.Should().Be(100);
        boss.Attack.Should().Be(22);
        boss.Defense.Should().Be(15);
        boss.XPValue.Should().Be(100);
        boss.LootTable.Should().NotBeNull();
    }
}
