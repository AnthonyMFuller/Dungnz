using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Integration tests that exercise multiple systems together using fake
/// input/display services to drive scripted flows.
/// </summary>
public class IntegrationTests
{
    private static FakeDisplayService MakeDisplay() => new FakeDisplayService();

    // ─────────────────────────────────────────────────────────────────
    // 1. Combat → Loot → Inventory flow
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Combat_Loot_ItemEndsUpInInventory()
    {
        var display = MakeDisplay();
        var sword = new Item { Name = "Test Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        var lootTable = new LootTable(new Random(0), minGold: 0, maxGold: 0);
        lootTable.AddDrop(sword, 1.0); // 100% drop

        var player = new Player { Name = "Hero" };
        var enemy = new Enemy_Stub(1, 1, 0, 10) { Name = "Weak Goblin" };
        enemy.LootTable = lootTable;

        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());
        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        player.Inventory.Should().Contain(i => i.Name == "Test Sword");
    }

    // ─────────────────────────────────────────────────────────────────
    // 2. Equipment stat flow
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Equipment_Weapon_IncreasesAndRestoresAttack()
    {
        var player = new Player { Name = "Hero" };
        var baseAttack = player.Attack;

        var weapon = new Item
        {
            Name = "Power Sword",
            Type = ItemType.Weapon,
            AttackBonus = 10,
            IsEquippable = true
        };
        player.Inventory.Add(weapon);
        player.EquipItem(weapon);

        player.Attack.Should().Be(baseAttack + 10);

        player.UnequipItem("weapon");

        player.Attack.Should().Be(baseAttack);
    }

    // ─────────────────────────────────────────────────────────────────
    // 3. Level-up flow (single level)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void LevelUp_IncreasesLevelAndMaxHP()
    {
        var player = new Player { Name = "Hero" };
        var initialLevel = player.Level;
        var initialMaxHP = player.MaxHP;

        // Give exactly 100 XP to trigger level 2 threshold (100/100+1=2 > 1)
        var display = MakeDisplay();
        var enemy = new Enemy_Stub(1, 1, 0, 100);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());
        engine.RunCombat(player, enemy);

        player.Level.Should().Be(initialLevel + 1);
        player.MaxHP.Should().Be(initialMaxHP + 10);
    }

    // ─────────────────────────────────────────────────────────────────
    // 4. Status effect survives turn
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Poison_Applied_EnemyHPReducedOnProcessTurnStart()
    {
        var display = MakeDisplay();
        var statusMgr = new StatusEffectManager(display);
        var enemy = new Enemy_Stub(50, 8, 2, 15);
        var hpBefore = enemy.HP;

        statusMgr.Apply(enemy, StatusEffect.Poison, 3);
        statusMgr.ProcessTurnStart(enemy);

        enemy.HP.Should().Be(hpBefore - 3);
    }

    // ─────────────────────────────────────────────────────────────────
    // 5. Multi-level XP jump (while-loop fix)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void XP250_StartingAtLevel1_ReachesLevel3()
    {
        var player = new Player { Name = "Hero" };

        // Enemy gives 250 XP:
        //   250/100+1 = 3 > 1 → level 2
        //   250/100+1 = 3 > 2 → level 3
        //   250/100+1 = 3 = 3 → stop
        var display = MakeDisplay();
        var enemy = new Enemy_Stub(1, 1, 0, 250);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());
        engine.RunCombat(player, enemy);

        player.Level.Should().Be(3);
    }
}
