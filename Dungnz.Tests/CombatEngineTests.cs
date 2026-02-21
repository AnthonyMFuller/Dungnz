using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class CombatEngineTests
{
    private static (Player player, Enemy enemy, FakeDisplayService display) MakeBasic(
        int playerHp = 100, int playerAtk = 10, int playerDef = 5,
        int enemyHp = 1, int enemyAtk = 8, int enemyDef = 2,
        int enemyXp = 15)
    {
        var player = new Player { HP = playerHp, MaxHP = playerHp, Attack = playerAtk, Defense = playerDef };
        var enemy = new Enemy_Stub(enemyHp, enemyAtk, enemyDef, enemyXp);
        var display = new FakeDisplayService();
        return (player, enemy, display);
    }

    [Fact]
    public void PlayerAttacks_EnemyDies_ReturnsWon()
    {
        var (player, enemy, display) = MakeBasic(enemyHp: 1);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
    }

    [Fact]
    public void EnemyAttacksBack_BeforeDying()
    {
        // Enemy HP=10, player does 8 dmg (10-2), enemy survives
        // Then enemy does 3 dmg (8-5), player takes damage
        // Then player finishes off enemy
        var (player, enemy, display) = MakeBasic(enemyHp: 10);
        var input = new FakeInputReader("A", "A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        player.HP.Should().BeLessThan(100); // took some damage
    }

    [Fact]
    public void PlayerDies_ReturnsPlayerDied()
    {
        // Player with tiny HP vs very high attack enemy
        var (player, enemy, display) = MakeBasic(playerHp: 5, playerDef: 0, enemyHp: 9999, enemyAtk: 100, enemyDef: 0);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.PlayerDied);
    }

    [Fact]
    public void FleeSucceeds_ReturnsFlcd()
    {
        var (player, enemy, display) = MakeBasic();
        var input = new FakeInputReader("F");
        // NextDouble() = 0.1 < 0.5 → flee succeeds
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.1));

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Fled);
    }

    [Fact]
    public void FleeFails_PlayerTakesDamage_CombatContinues()
    {
        var (player, enemy, display) = MakeBasic(enemyHp: 1);
        var input = new FakeInputReader("F", "A");
        // First NextDouble() = 0.9 >= 0.5 → flee fails; second call for loot (doesn't matter)
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        var result = engine.RunCombat(player, enemy);

        // Eventually wins after flee fails and then attacks
        result.Should().Be(CombatResult.Won);
        player.HP.Should().BeLessThan(100); // took damage from failed flee
    }

    [Fact]
    public void FleeFails_PlayerDies_ReturnsPlayerDied()
    {
        // Player very low HP, flee fails, enemy kill blow
        var (player, enemy, display) = MakeBasic(playerHp: 1, playerDef: 0, enemyHp: 9999, enemyAtk: 100);
        var input = new FakeInputReader("F");
        // NextDouble() = 0.9 → flee fails → enemy hits for 100 → player dies
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.PlayerDied);
    }

    [Fact]
    public void XpAwardedOnWin()
    {
        var (player, enemy, display) = MakeBasic(enemyXp: 30);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.XP.Should().Be(30);
    }

    [Fact]
    public void GoldAwardedOnWin()
    {
        var (player, enemy, display) = MakeBasic();
        // Use a loot table with fixed gold
        ((Enemy_Stub)enemy).LootTable = new LootTable(minGold: 10, maxGold: 10);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.Gold.Should().Be(10);
    }

    [Fact]
    public void ItemAwardedOnWin_WhenLootTableDropsIt()
    {
        var (player, enemy, display) = MakeBasic();
        var sword = new Item { Name = "Test Sword", Type = ItemType.Weapon };
        ((Enemy_Stub)enemy).LootTable = new LootTable(new Random(0), 0, 0);
        ((Enemy_Stub)enemy).LootTable.AddDrop(sword, 1.0);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.Inventory.Should().Contain(sword);
    }

    [Fact]
    public void LevelUp_Triggered_WhenXpCrossesThreshold()
    {
        var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, XP = 90, Level = 1 };
        var enemy = new Enemy_Stub(hp: 1, atk: 8, def: 2, xp: 15);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.Level.Should().Be(2);
        player.Attack.Should().Be(12); // +2
        player.Defense.Should().Be(6); // +1
        player.MaxHP.Should().Be(110); // +10
        player.HP.Should().Be(110);    // restored
    }

    [Fact]
    public void DamageFormula_IsMaxOneAttackerMinusDefender()
    {
        // Player Attack=10, Enemy Defense=9 → damage = Max(1, 10-9) = 1
        var (player, enemy, display) = MakeBasic(playerAtk: 10, enemyDef: 9, enemyHp: 1);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("1 damage"));
    }

    [Fact]
    public void MinimumOneDamageAlwaysDealt()
    {
        // Player Attack=1, Enemy Defense=100 → Max(1, 1-100) = 1
        var (player, enemy, display) = MakeBasic(playerAtk: 1, enemyDef: 100, enemyHp: 1);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("1 damage"));
    }
}

/// <summary>Test stub enemy with configurable stats.</summary>
internal class Enemy_Stub : Enemy
{
    public Enemy_Stub(int hp, int atk, int def, int xp)
    {
        Name = "TestEnemy";
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = xp;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f; // deterministic: never dodges in tests
    }
}
