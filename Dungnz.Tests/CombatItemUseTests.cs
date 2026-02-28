using System.Collections.Generic;
using System.IO;
using System;
using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for the Use Item (I) combat feature added in #649.</summary>
public class CombatItemUseTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static Player MakePlayer(int hp = 100, int maxHp = 100, int atk = 50, int def = 0)
        => new Player { HP = hp, MaxHP = maxHp, Attack = atk, Defense = def, Level = 1 };

    private static Item MakePotion(int heal = 20)
        => new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = heal };

    // Enemy that dies in one hit when player atk=50, def=0 → 50 dmg kills hp≤50
    private static Enemy_Stub WeakEnemy() => new Enemy_Stub(hp: 10, atk: 5, def: 0, xp: 1);

    // ── Test 1: happy-path use consumable heals player and removes item ──────

    [Fact]
    public void HappyPath_UseConsumable_HealsPlayerAndRemovesItem()
    {
        // Arrange: player at 80/100 HP with one Health Potion (+20 HP) in inventory.
        // Input: "I" → opens item menu (FakeDisplayService returns consumables[0])
        //        then enemy turn runs; "A" → player attacks and kills enemy.
        var player = MakePlayer(hp: 80, maxHp: 100);
        var potion = MakePotion(heal: 20);
        player.Inventory.Add(potion);

        // "I" → combat menu selects Use Item
        // "1"  → ShowCombatItemMenuAndSelect reads and discards one line, returns consumables[0]
        // "A"  → combat menu selects Attack (kills the enemy)
        var input = new FakeInputReader("I", "1", "A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9); // no crits, no dodges
        var engine = new CombatEngine(display, input, rng);

        // Act
        var result = engine.RunCombat(player, WeakEnemy());

        // Assert
        result.Should().Be(CombatResult.Won);
        player.Inventory.Should().NotContain(i => i.Name == "Health Potion", "item must be consumed");
        player.HP.Should().BeGreaterThanOrEqualTo(80, "healing should not reduce HP below starting value");
        display.AllOutput.Should().Contain("combat_item_menu", "item menu must have been shown");
    }

    // ── Test 2: no consumables → message shown, turn not consumed ────────────

    [Fact]
    public void UseItem_NoConsumables_MessageShown_TurnNotConsumed()
    {
        // Arrange: player has NO consumables.
        // "I" → "no items" message fires, turn is free (no enemy action on this input)
        // "A" → player attacks and kills the weak enemy; combat resolves normally.
        var player = MakePlayer(hp: 100, atk: 50, def: 0);
        var enemy = WeakEnemy(); // hp=10, atk=5, def=0

        var input = new FakeInputReader("I", "A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var engine = new CombatEngine(display, input, rng);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: "no items" message shown and combat eventually resolves
        result.Should().Be(CombatResult.Won);
        display.Messages.Should().Contain(m => m.Contains("no usable items"),
            "player should be told they have no usable items");
    }

    // ── Test 3: cancel (no consumables path) turn not consumed ───────────────

    [Fact]
    public void UseItem_Cancel_TurnNotConsumed_EnemyDoesNotAct()
    {
        // The "no consumables" path returns AbilityMenuResult.Cancel, which means the
        // item menu is entirely free — enemy does NOT get a turn.
        // Contrast: pressing "B" then cancelling IS NOT free (see AbilityCancel test).
        //
        // Setup: enemy hp=10 dies in one hit from player (atk=50).
        // With I-cancel free: enemy never acts → player HP stays at 100.
        var player = MakePlayer(hp: 100, atk: 50, def: 0);
        var enemy = new Enemy_Stub(hp: 10, atk: 20, def: 0, xp: 1);

        // "I" → no consumables → free cancel (enemy does NOT act)
        // "A" → player kills enemy (50 dmg > 10 hp) — enemy dies before it can act
        var input = new FakeInputReader("I", "A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var engine = new CombatEngine(display, input, rng);

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        // Enemy never dealt damage: I-cancel was free + A killed enemy instantly
        player.HP.Should().Be(100, "I-cancel is free so enemy never got a turn");
    }

    // ── Test 4: unit test for ShowCombatItemMenuAndSelect formatting ──────────

    [Fact]
    public void ShowCombatItemMenuAndSelect_FormatsItemsAndReturnsSelection()
    {
        // Arrange: two consumables; input "1" selects the first item.
        var items = new List<Item>
        {
            new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 },
            new Item { Name = "Mega Potion",   Type = ItemType.Consumable, HealAmount = 60 },
        };

        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            // "1" selects the first option; FakeInputReader is non-interactive so DisplayService
            // uses numbered text input fallback.
            var input = new FakeInputReader("1");
            var svc = new ConsoleDisplayService(input);

            var selected = svc.ShowCombatItemMenuAndSelect(items);

            selected.Should().NotBeNull("a selection of '1' should return the first item");
            selected!.Name.Should().Be("Health Potion");
            sw.ToString().Should().Contain("Health Potion", "item name must appear in the menu");
            sw.ToString().Should().Contain("+30 HP", "heal amount must appear in the menu");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ── Test 5: ShowCombatItemMenuAndSelect cancel returns null ───────────────

    [Fact]
    public void ShowCombatItemMenuAndSelect_Cancel_ReturnsNull()
    {
        var items = new List<Item>
        {
            new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 },
        };

        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            // The cancel option is always the last numbered entry; here that's "2".
            var input = new FakeInputReader("2");
            var svc = new ConsoleDisplayService(input);

            var selected = svc.ShowCombatItemMenuAndSelect(items);

            selected.Should().BeNull("selecting Cancel should return null");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
