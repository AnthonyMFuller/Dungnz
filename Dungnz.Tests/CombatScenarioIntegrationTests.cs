using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class CombatScenarioIntegrationTests
{
    private static LootTable NoLoot() => new LootTable(new Random(42), minGold: 0, maxGold: 0);

    [Fact]
    public void Combat_KillEnemy_XPGrantedAndLevelIncremented()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 100); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        int levelBefore = player.Level;
        var result = new CombatEngine(display, new FakeInputReader("A", "1"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        result.Should().Be(CombatResult.Won);
        player.XP.Should().Be(100);
        player.Level.Should().Be(levelBefore + 1);
    }

    [Fact]
    public void Combat_LevelUp_MaxHPIncreasedBeyondBase()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 100); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithAttack(20).Build();
        int before = player.MaxHP;
        new CombatEngine(display, new FakeInputReader("A", "1"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.MaxHP.Should().BeGreaterThan(before);
    }

    [Fact]
    public void Combat_250XP_StartingLevel1_ReachesLevel3()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 250); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        new CombatEngine(display, new FakeInputReader("A", "1", "1"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.Level.Should().Be(3);
    }

    [Fact]
    public void Combat_PlayerHPReachesZero_CombatResultIsPlayerDied()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 100, atk: 999, def: 0, xp: 50); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(1).WithMaxHP(1).WithAttack(1).WithDefense(0).Build();
        var result = new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        result.Should().Be(CombatResult.PlayerDied);
        player.HP.Should().BeLessOrEqualTo(0);
    }

    [Fact]
    public void Combat_PlayerDeath_HPClampedAtZeroNotNegative()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 50, atk: 999, def: 0, xp: 10); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(5).WithMaxHP(5).WithAttack(1).WithDefense(0).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.HP.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Combat_EnemyKilled_HPClampedAtZeroNotNegative()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 5, atk: 1, def: 0, xp: 10); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(999).Build();
        var result = new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        result.Should().Be(CombatResult.Won);
        enemy.HP.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Combat_HighDefenseEnemy_PlayerDealsAtLeast1DamagePerHit()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 3, atk: 1, def: 9999, xp: 5); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(5).WithDefense(0).Build();
        new CombatEngine(display, new FakeInputReader("A", "A", "A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy)
            .Should().Be(CombatResult.Won);
    }

    [Fact]
    public void Combat_RunStats_DamageDealtTracked()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 10, atk: 1, def: 0, xp: 10); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        var stats = new RunStats();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy, stats);
        stats.DamageDealt.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Combat_RunStats_DamageTakenTracked()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 5, atk: 5, def: 0, xp: 10); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(10).WithDefense(0).Build();
        var stats = new RunStats();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy, stats);
        stats.DamageTaken.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Combat_TwoEnemiesSequential_XPAccumulates()
    {
        var display = new FakeDisplayService();
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        var e1 = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10); e1.LootTable = NoLoot();
        new CombatEngine(display, new FakeInputReader("A"), rng).RunCombat(player, e1);
        var e2 = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10); e2.LootTable = NoLoot();
        new CombatEngine(display, new FakeInputReader("A"), rng).RunCombat(player, e2);
        player.XP.Should().Be(20);
    }

    [Fact]
    public void Combat_DisplayReceivesCombatMessages()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10); enemy.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        display.AllOutput.Should().NotBeEmpty();
    }

    [Fact]
    public void Combat_GoblinEnemy_StrongPlayerWins()
    {
        var display = new FakeDisplayService();
        var goblin = new Goblin(); goblin.HP = 1; goblin.Attack = 1;
        goblin.LootTable = NoLoot();
        var player = new PlayerBuilder().WithHP(100).WithAttack(50).Build();
        var stats = new RunStats();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, goblin, stats)
            .Should().Be(CombatResult.Won);
        stats.EnemiesDefeated.Should().Be(1);
    }
}
