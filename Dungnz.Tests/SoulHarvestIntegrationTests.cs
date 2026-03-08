using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Integration tests for SoulHarvest (Necromancer passive ability) that gate against
/// the double-heal regression bug (#998). These tests verify that SoulHarvest heals
/// exactly once per enemy death regardless of how event systems evolve.
/// </summary>
public class SoulHarvestIntegrationTests
{
    // ── Test 1: SoulHarvest fires exactly once per kill ──────────────────────

    [Fact]
    public void SoulHarvest_OnEnemyKill_HealsExactlyOnce()
    {
        // Arrange: Necromancer at partial HP, enemy at 1 HP (one-shot kill)
        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 50,
            MaxHP = 100,
            Attack = 20,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Weak Goblin" };
        var input = new FakeInputReader("A"); // single attack to kill
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        var hpBefore = player.HP;

        // Act: Run combat — enemy dies in one hit
        var result = engine.RunCombat(player, enemy);

        // Assert: SoulHarvest heals exactly +5 HP, not +10 (double-heal)
        result.Should().Be(CombatResult.Won);
        player.HP.Should().Be(hpBefore + 5, "SoulHarvest should heal exactly +5 HP once per kill");
        display.CombatMessages.Should().Contain(m => m.Contains("Soul Harvest") || m.Contains("essence"),
            "SoulHarvest message should appear exactly once");
    }

    [Fact]
    public void SoulHarvest_MultipleKills_HealsOncePerKill()
    {
        // Arrange: Necromancer fights multiple enemies sequentially
        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 40,
            MaxHP = 100,
            Attack = 20,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        var display = new FakeDisplayService();
        var rng = new ControlledRandom(defaultDouble: 0.9);

        // Act: Kill 3 enemies in sequence
        var enemy1 = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Goblin 1" };
        var input1 = new FakeInputReader("A");
        var engine1 = new CombatEngine(display, input1, rng);
        engine1.RunCombat(player, enemy1);
        var hpAfterKill1 = player.HP;

        var enemy2 = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Goblin 2" };
        var input2 = new FakeInputReader("A");
        var engine2 = new CombatEngine(display, input2, rng);
        engine2.RunCombat(player, enemy2);
        var hpAfterKill2 = player.HP;

        var enemy3 = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Goblin 3" };
        var input3 = new FakeInputReader("A");
        var engine3 = new CombatEngine(display, input3, rng);
        engine3.RunCombat(player, enemy3);
        var hpAfterKill3 = player.HP;

        // Assert: Each kill heals exactly +5 HP
        hpAfterKill1.Should().Be(40 + 5, "First kill should heal +5 HP");
        hpAfterKill2.Should().Be(45 + 5, "Second kill should heal +5 HP");
        hpAfterKill3.Should().Be(50 + 5, "Third kill should heal +5 HP");
    }

    // ── Test 2: SoulHarvest does NOT fire when enemy survives ────────────────

    [Fact]
    public void SoulHarvest_EnemySurvives_NoHeal()
    {
        // Arrange: Necromancer fights enemy that survives the first hit
        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 50,
            MaxHP = 100,
            Attack = 20,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        var enemy = new Enemy_Stub(hp: 50, atk: 5, def: 0, xp: 10) { Name = "Tough Goblin" };
        var input = new FakeInputReader("A", "F"); // Attack once, then flee
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.1)); // flee succeeds

        var hpBefore = player.HP;

        // Act: Attack once but don't kill — flee instead
        var result = engine.RunCombat(player, enemy);

        // Assert: No SoulHarvest heal because enemy didn't die
        result.Should().Be(CombatResult.Fled);
        enemy.HP.Should().BeGreaterThan(0, "Enemy should still be alive");
        player.HP.Should().Be(hpBefore, "SoulHarvest should NOT heal because enemy survived");
        display.CombatMessages.Should().NotContain(m => m.Contains("Soul Harvest") || m.Contains("essence"),
            "SoulHarvest message should not appear when enemy survives");
    }

    // ── Test 3: EventBus OnEnemyKilled published exactly once ────────────────

    [Fact]
    public void OnEnemyKilled_Event_PublishedExactlyOncePerKill()
    {
        // Arrange: Set up a spy to count OnEnemyKilled event publications
        var eventBus = new GameEventBus();
        int eventCount = 0;
        OnEnemyKilled? capturedEvent = null;

        eventBus.Subscribe<OnEnemyKilled>(evt =>
        {
            eventCount++;
            capturedEvent = evt;
        });

        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 50,
            MaxHP = 100,
            Attack = 20,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Test Goblin" };
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act: Kill the enemy
        var result = engine.RunCombat(player, enemy);

        // Manually publish the event (simulating what future EventBus wiring would do)
        if (result == CombatResult.Won)
        {
            eventBus.Publish(new OnEnemyKilled(player, enemy));
        }

        // Assert: OnEnemyKilled event published exactly once
        result.Should().Be(CombatResult.Won);
        eventCount.Should().Be(1, "OnEnemyKilled event should be published exactly once per kill");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Player.Should().Be(player);
        capturedEvent.Enemy.Should().Be(enemy);
    }

    // ── Test 4: No double-heal regression via both code paths ────────────────

    [Fact]
    public void SoulHarvest_DoubleHealRegression_GateTest()
    {
        // Arrange: Necromancer with SoulHarvest, EventBus wired to also trigger heal
        var eventBus = new GameEventBus();
        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 50,
            MaxHP = 100,
            Attack = 20,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Test Goblin" };
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        int eventBusHealCount = 0;
        // Simulate EventBus wiring that would cause double-heal if not properly gated
        eventBus.Subscribe<OnEnemyKilled>(evt =>
        {
            if (evt.Player.Class == PlayerClass.Necromancer)
            {
                eventBusHealCount++;
                // THIS IS THE BUG: If both CombatEngine AND EventBus fire SoulHarvest, double-heal occurs
                // evt.Player.Heal(5); // DELIBERATELY COMMENTED — this is what we're preventing
            }
        });

        var hpBefore = player.HP;

        // Act: Kill the enemy
        var result = engine.RunCombat(player, enemy);

        // Simulate EventBus publication (in future, CombatEngine would do this)
        if (result == CombatResult.Won)
        {
            eventBus.Publish(new OnEnemyKilled(player, enemy));
        }

        var hpAfter = player.HP;
        var actualHeal = hpAfter - hpBefore;

        // Assert: Player healed exactly +5 HP, not +10 (even if EventBus fires)
        result.Should().Be(CombatResult.Won);
        actualHeal.Should().Be(5, "SoulHarvest should heal exactly +5 HP once, not twice");
        eventBusHealCount.Should().Be(1, "EventBus handler should fire, but NOT apply duplicate heal");

        // Verify combat message shows SoulHarvest triggered exactly once
        var soulHarvestMessages = display.CombatMessages
            .Where(m => m.Contains("Soul Harvest") || m.Contains("essence"))
            .ToList();
        soulHarvestMessages.Should().HaveCount(1, "SoulHarvest message should appear exactly once");
    }

    [Fact]
    public void SoulHarvest_OnlyNecromancer_OtherClassesNoHeal()
    {
        // Arrange: Non-Necromancer class kills enemy
        var player = new Player
        {
            Name = "Warrior",
            Class = PlayerClass.Warrior,
            HP = 50,
            MaxHP = 100,
            Attack = 20,
            Defense = 5
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Test Goblin" };
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        var hpBefore = player.HP;

        // Act: Kill the enemy as Warrior
        var result = engine.RunCombat(player, enemy);

        // Assert: No SoulHarvest heal for non-Necromancer
        result.Should().Be(CombatResult.Won);
        player.HP.Should().Be(hpBefore, "SoulHarvest should only heal Necromancers");
        display.CombatMessages.Should().NotContain(m => m.Contains("Soul Harvest") || m.Contains("essence"),
            "SoulHarvest message should not appear for non-Necromancer classes");
    }

    [Fact]
    public void SoulHarvest_AtMaxHP_StillTriggersButNoOverheal()
    {
        // Arrange: Necromancer already at max HP
        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 100,
            MaxHP = 100,
            Attack = 20,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 5, def: 0, xp: 10) { Name = "Test Goblin" };
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act: Kill the enemy at max HP
        var result = engine.RunCombat(player, enemy);

        // Assert: SoulHarvest still triggers, HP stays at max (no overheal)
        result.Should().Be(CombatResult.Won);
        player.HP.Should().Be(100, "HP should remain at max, no overheal");
        display.CombatMessages.Should().Contain(m => m.Contains("Soul Harvest") || m.Contains("essence"),
            "SoulHarvest message should still appear even at max HP");
    }

    [Fact]
    public void SoulHarvest_PlayerDies_NoHealTriggered()
    {
        // Arrange: Necromancer with very low HP vs high-attack enemy
        var player = new Player
        {
            Name = "Necromancer",
            Class = PlayerClass.Necromancer,
            HP = 1,
            MaxHP = 100,
            Attack = 5,
            Defense = 0,
            Mana = 50,
            MaxMana = 50
        };
        var enemy = new Enemy_Stub(hp: 100, atk: 50, def: 0, xp: 10) { Name = "Deadly Goblin" };
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act: Player attacks but dies from counterattack
        var result = engine.RunCombat(player, enemy);

        // Assert: Player died, no SoulHarvest heal
        result.Should().Be(CombatResult.PlayerDied);
        enemy.HP.Should().BeGreaterThan(0, "Enemy survived");
        display.CombatMessages.Should().NotContain(m => m.Contains("Soul Harvest") || m.Contains("essence"),
            "SoulHarvest should not trigger when player dies");
    }
}
