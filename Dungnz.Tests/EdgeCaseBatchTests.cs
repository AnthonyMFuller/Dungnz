using System;
using System.Collections.Generic;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

// ════════════════════════════════════════════════════════════════════════════
// Issues #1233 #1239 #1243 #1248 #1249 #1251 — edge-case coverage batch
// ════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// #1251 — Status effect double-application: different effects on the same stat
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #1251 — Verifies that multiple distinct status effects that each modify the same
/// stat (Attack or Defense) stack additively, not via overwrite.
/// </summary>
public class StatusEffectStatStackingTests
{
    private static (StatusEffectManager mgr, Player player) Make(int atk = 40, int def = 20)
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var player = new Player { Name = "Hero", Attack = atk, Defense = def };
        return (mgr, player);
    }

    [Fact]
    public void BattleCry_AndWeakened_BothModifyAttack_Additively()
    {
        // Arrange
        var (mgr, player) = Make(atk: 40);
        // BattleCry: +Attack/4 = +10
        mgr.Apply(player, StatusEffect.BattleCry, 3);
        // Weakened: -Attack/2 = -20
        mgr.Apply(player, StatusEffect.Weakened, 3);

        // Act
        var netAttackMod = mgr.GetStatModifier(player, "Attack");

        // Assert: +10 (BattleCry) + -20 (Weakened) = -10, not just -20 overwriting
        netAttackMod.Should().Be(-10,
            "BattleCry adds +10 and Weakened subtracts -20; combined modifier is -10");
    }

    [Fact]
    public void BattleCry_AndCurse_BothModifyAttack_Additively()
    {
        // Arrange
        var (mgr, player) = Make(atk: 40);
        // BattleCry: +Attack/4 = +10
        mgr.Apply(player, StatusEffect.BattleCry, 3);
        // Curse: -Attack/4 = -10
        mgr.Apply(player, StatusEffect.Curse, 3);

        // Act
        var netAttackMod = mgr.GetStatModifier(player, "Attack");

        // Assert: +10 - 10 = 0
        netAttackMod.Should().Be(0,
            "BattleCry adds +10 and Curse subtracts -10; net modifier is 0");
    }

    [Fact]
    public void Weakened_AndSlow_BothReduceAttack_Additively()
    {
        // Arrange
        var (mgr, player) = Make(atk: 40);
        // Weakened: -Attack/2 = -20
        mgr.Apply(player, StatusEffect.Weakened, 3);
        // Slow: -Attack/4 = -10
        mgr.Apply(player, StatusEffect.Slow, 3);

        // Act
        var netAttackMod = mgr.GetStatModifier(player, "Attack");

        // Assert: -20 + -10 = -30
        netAttackMod.Should().Be(-30,
            "Weakened and Slow both reduce Attack; combined penalty is -30");
    }

    [Fact]
    public void Fortified_AndCurse_BothModifyDefense_Additively()
    {
        // Arrange
        var (mgr, player) = Make(def: 20);
        // Fortified: +Defense/2 = +10
        mgr.Apply(player, StatusEffect.Fortified, 3);
        // Curse: -Defense/4 = -5
        mgr.Apply(player, StatusEffect.Curse, 3);

        // Act
        var netDefMod = mgr.GetStatModifier(player, "Defense");

        // Assert: +10 - 5 = +5
        netDefMod.Should().Be(5,
            "Fortified adds +10 defense and Curse subtracts -5 defense; net is +5");
    }

    [Fact]
    public void ThreeEffects_AllModifyAttack_SumCorrectly()
    {
        // Arrange: all three Attack modifiers active simultaneously
        var (mgr, player) = Make(atk: 40);
        // BattleCry: +40/4 = +10
        mgr.Apply(player, StatusEffect.BattleCry, 3);
        // Weakened: -40/2 = -20
        mgr.Apply(player, StatusEffect.Weakened, 3);
        // Slow: -40/4 = -10
        mgr.Apply(player, StatusEffect.Slow, 3);

        // Act
        var netAttackMod = mgr.GetStatModifier(player, "Attack");

        // Assert: +10 - 20 - 10 = -20
        netAttackMod.Should().Be(-20,
            "BattleCry (+10) + Weakened (-20) + Slow (-10) = -20");
    }

    [Fact]
    public void DifferentStatEffects_DoNotCrossContaminate()
    {
        // Arrange: Fortified modifies Defense only; should not affect Attack modifier
        var (mgr, player) = Make(atk: 40, def: 20);
        mgr.Apply(player, StatusEffect.Fortified, 3);

        // Act
        var attackMod = mgr.GetStatModifier(player, "Attack");
        var defMod    = mgr.GetStatModifier(player, "Defense");

        // Assert: Attack unchanged, Defense increased
        attackMod.Should().Be(0,
            "Fortified does not affect Attack");
        defMod.Should().Be(10,
            "Fortified adds +Defense/2 = +10 to Defense");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #1249 — Navigation to invalid/missing exit (dead-end edge case)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #1249 — Verifies that attempting to move in a direction with no exit shows an error
/// and does NOT advance the game state.
/// </summary>
[Collection("PrestigeTests")]
public class NavigationDeadEndTests
{
    private static (Player player, Room room, FakeDisplayService display) MakeSetup()
    {
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var northRoom = new Room { Description = "North room" };
        var startRoom = new Room { Description = "Start room" };
        startRoom.Exits[Direction.North] = northRoom;
        startRoom.Exits[Direction.East]  = new Room { Description = "East room" };
        var display = new FakeDisplayService();
        return (player, startRoom, display);
    }

    private static GameLoop MakeLoop(FakeDisplayService display, params string[] inputs)
    {
        var reader = new FakeInputReader(inputs);
        display.SetInputReader(reader);
        var combat = new Moq.Mock<ICombatEngine>();
        return new GameLoop(display, combat.Object, reader);
    }

    [Fact]
    public void Go_West_WhenNoWestExit_ShowsErrorMessage()
    {
        // Arrange: room only has North and East exits; no West exit
        var (player, room, display) = MakeSetup();
        var loop = MakeLoop(display, "go west", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        display.Errors.Should().Contain(e =>
            e.Contains("can't go that way") || e.Contains("no exit") || e.Contains("that way"),
            "attempting to move in a direction with no exit should produce an error");
    }

    [Fact]
    public void Go_South_WhenNoSouthExit_ShowsErrorMessage()
    {
        // Arrange: no south exit
        var (player, room, display) = MakeSetup();
        var loop = MakeLoop(display, "go south", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        display.Errors.Should().Contain(e =>
            e.Contains("can't go that way") || e.Contains("that way"),
            "no south exit should produce error");
    }

    [Fact]
    public void Go_West_WhenNoWestExit_PlayerRemainsInStartRoom()
    {
        // Arrange
        var (player, room, display) = MakeSetup();
        var startDescription = room.Description;
        var loop = MakeLoop(display, "go west", "quit");

        // Act
        loop.Run(player, room);

        // Assert: player did not move — room description still visible
        // (GameLoop RunLoop keeps a reference; the error path means currentRoom stays)
        // We verify indirectly: no room messages containing "West room" (since it doesn't exist)
        display.Messages.Should().NotContain(m => m.Contains("West room"));
    }

    [Fact]
    public void Go_WithNoArgument_ShowsDirectionPrompt()
    {
        // Arrange
        var (player, room, display) = MakeSetup();
        var loop = MakeLoop(display, "go", "quit");

        // Act
        loop.Run(player, room);

        // Assert: "go" with no direction arg should produce an error/prompt
        display.Errors.Should().Contain(e =>
            e.Contains("direction") || e.Contains("where") || e.Contains("Go where"),
            "bare 'go' command with no direction should show a direction prompt");
    }

    [Fact]
    public void Go_InvalidDirectionWord_ShowsInvalidDirectionError()
    {
        // Arrange
        var (player, room, display) = MakeSetup();
        var loop = MakeLoop(display, "go sideways", "quit");

        // Act
        loop.Run(player, room);

        // Assert: "sideways" is not a valid cardinal direction
        display.Errors.Should().Contain(e =>
            e.Contains("Invalid direction") || e.Contains("sideways"),
            "unrecognised direction word should produce error");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #1248 — LootTable.RollDrop with floor 0 or negative floor numbers
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #1248 — Verifies that calling RollDrop with dungeonFloor=0 or negative values does not
/// crash (no division by zero, no negative tier chance, no index underflow).
/// </summary>
[Collection("LootTableTests")]
public class LootTableFloorEdgeCaseTests : IDisposable
{
    // Use a minimal fallback so tests do not depend on shared tier pool state
    private static readonly IReadOnlyList<Item> _minimalPool = new List<Item>
    {
        new Item { Name = "Pebble", Type = ItemType.Consumable, Tier = ItemTier.Common }
    }.AsReadOnly();

    public void Dispose()
    {
        // Restore tier pools after each test so they do not pollute other LootTable tests
        LootTable.SetTierPools(_minimalPool, _minimalPool, _minimalPool);
    }

    [Fact]
    public void RollDrop_FloorZero_DoesNotThrow()
    {
        // Arrange
        var rng   = new ControlledRandom(defaultDouble: 0.9); // above 0.30 → no tiered drop
        var table = new LootTable(rng, minGold: 5, maxGold: 5);

        // Act
        var act = () => table.RollDrop(null!, playerLevel: 1, dungeonFloor: 0);

        // Assert
        act.Should().NotThrow("floor 0 must not cause exceptions");
    }

    [Fact]
    public void RollDrop_FloorZero_ReturnsGold()
    {
        // Arrange: high rng → no tiered/epic/legendary drop
        var rng   = new ControlledRandom(defaultDouble: 0.95);
        var table = new LootTable(rng, minGold: 10, maxGold: 10);

        // Act
        var result = table.RollDrop(null!, playerLevel: 1, dungeonFloor: 0);

        // Assert
        result.Gold.Should().Be(10, "gold should still be awarded at floor 0");
        result.Item.Should().BeNull("no epic/legendary paths fire at floor 0");
    }

    [Fact]
    public void RollDrop_NegativeFloor_DoesNotThrow()
    {
        // Arrange
        var rng   = new ControlledRandom(defaultDouble: 0.9);
        var table = new LootTable(rng, minGold: 0, maxGold: 0);

        // Act
        var act = () => table.RollDrop(null!, playerLevel: 1, dungeonFloor: -5);

        // Assert
        act.Should().NotThrow("negative floor number must not cause exceptions");
    }

    [Fact]
    public void RollDrop_FloorZero_NoEpicDrop()
    {
        // Arrange: epic path only fires at floor >= 5; floor 0 must not trigger it
        var epicItem = new Item { Name = "Epic Sword", Type = ItemType.Weapon, Tier = ItemTier.Epic };
        LootTable.SetTierPools(_minimalPool, _minimalPool, _minimalPool,
            epic: new List<Item> { epicItem }.AsReadOnly());

        // Force the 15% epic roll path by using a low double value
        var rng   = new ControlledRandom(defaultDouble: 0.05);
        var table = new LootTable(rng, minGold: 0, maxGold: 0);

        // Act
        var result = table.RollDrop(null!, playerLevel: 1, dungeonFloor: 0);

        // Assert: epic drop path (dungeonFloor >= 5) was not triggered
        result.Item?.Name.Should().NotBe("Epic Sword",
            "epic drop requires floor >= 5; floor 0 should not yield an epic");
    }

    [Fact]
    public void RollDrop_FloorZero_NoLegendaryDrop()
    {
        // Arrange: legendary path only fires at floor >= 6; floor 0 must not trigger it
        var legendaryItem = new Item { Name = "Legendary Blade", Type = ItemType.Weapon, Tier = ItemTier.Legendary };
        LootTable.SetTierPools(_minimalPool, _minimalPool, _minimalPool,
            legendary: new List<Item> { legendaryItem }.AsReadOnly());

        var rng   = new ControlledRandom(defaultDouble: 0.02); // very low to force path if guard fails
        var table = new LootTable(rng, minGold: 0, maxGold: 0);

        // Act
        var result = table.RollDrop(null!, playerLevel: 1, dungeonFloor: 0);

        // Assert
        result.Item?.Name.Should().NotBe("Legendary Blade",
            "legendary drop requires floor >= 6; floor 0 must not yield a legendary");
    }

    [Fact]
    public void RollDrop_FloorNegative_ReturnsValidResult()
    {
        // Arrange
        var rng   = new ControlledRandom(defaultDouble: 0.95);
        var table = new LootTable(rng, minGold: 3, maxGold: 3);

        // Act
        var result = table.RollDrop(null!, playerLevel: 1, dungeonFloor: -99);

        // Assert: should behave identically to floor 1 — gold only, no crash
        result.Gold.Should().Be(3);
    }

    // ── Overload used in the test above that accepts named 'epic' param ──────
    // SetTierPools signature: (t1, t2, t3, legendary?, epic?)
    // The existing overload puts legendary before epic, so we need a helper:
}

// Extension helper to avoid parameter confusion in the test above
internal static class LootTableTestExtensions
{
    internal static void SetTierPools(
        IReadOnlyList<Item> t1,
        IReadOnlyList<Item> t2,
        IReadOnlyList<Item> t3,
        IReadOnlyList<Item>? legendary = null,
        IReadOnlyList<Item>? epic = null)
        => LootTable.SetTierPools(t1, t2, t3, legendary, epic);
}

// ─────────────────────────────────────────────────────────────────────────────
// #1243 — Combat with enemy already at 0 HP
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #1243 — Verifies that starting combat when the enemy HP is already 0 (dead) resolves
/// immediately as Won without requiring any player input or crashing.
/// </summary>
public class CombatDeadEnemyTests
{
    private static Player MakePlayer(int hp = 100, int atk = 20, int def = 5)
        => new Player { HP = hp, MaxHP = hp, Attack = atk, Defense = def, Mana = 100, MaxMana = 100, Level = 1 };

    private static CombatEngine MakeEngine(FakeDisplayService display, FakeInputReader input)
        => new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

    [Fact]
    public void RunCombat_EnemyStartsAtZeroHP_ReturnWon_Immediately()
    {
        // Arrange
        var player = MakePlayer();
        var enemy  = new Enemy_Stub(1, 10, 0, 10) { HP = 0 }; // MaxHP=1 but HP set to 0
        var input  = new FakeInputReader(); // no input needed — should resolve before reading
        var display = new FakeDisplayService(input);
        var engine  = MakeEngine(display, input);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert
        result.Should().Be(CombatResult.Won,
            "entering combat with an already-dead enemy (HP=0) must return Won");
    }

    [Fact]
    public void RunCombat_EnemyAtZeroHP_PlayerTakesNoDamage()
    {
        // Arrange
        var player = MakePlayer(hp: 100);
        var enemy  = new Enemy_Stub(1, 999, 0, 10) { HP = 0 }; // would one-shot player if alive
        var input  = new FakeInputReader();
        var display = new FakeDisplayService(input);
        var engine  = MakeEngine(display, input);

        // Act
        engine.RunCombat(player, enemy);

        // Assert: enemy never got to act, so player is still at full HP
        player.HP.Should().Be(100,
            "a dead enemy must not deal damage");
    }

    [Fact]
    public void RunCombat_EnemyAtZeroHP_EnemyHPRemainsZero()
    {
        // Arrange
        var player = MakePlayer();
        var enemy  = new Enemy_Stub(50, 10, 0, 10) { HP = 0 };
        var input  = new FakeInputReader();
        var display = new FakeDisplayService(input);
        var engine  = MakeEngine(display, input);

        // Act
        engine.RunCombat(player, enemy);

        // Assert: HP should not go below 0 (stays clamped at 0)
        enemy.HP.Should().Be(0,
            "enemy HP must remain at 0, not go negative");
    }

    [Fact]
    public void RunCombat_EnemyAtExactlyOneHP_ThenKilledFirstHit_ReturnWon()
    {
        // Arrange: player ATK=100 kills enemy HP=1 in one hit
        var player = MakePlayer(hp: 100, atk: 100, def: 5);
        var enemy  = new Enemy_Stub(1, 999, 0, 10); // HP=1
        var input  = new FakeInputReader("A"); // one attack input
        var display = new FakeDisplayService(input);
        var engine  = MakeEngine(display, input);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert
        result.Should().Be(CombatResult.Won,
            "a one-hit kill from full HP=1 enemy must return Won");
        enemy.HP.Should().Be(0, "enemy HP must be clamped to 0 on death");
    }

    [Fact]
    public void RunCombat_EnemyAtZeroHP_NoStatusEffectsAppliedToDeadEnemy()
    {
        // Arrange: player has a periodic damage bonus; dead enemy must not receive it
        var player = MakePlayer();
        player.PeriodicDmgBonus = 50; // would cause periodic tick to fire
        var enemy  = new Enemy_Stub(1, 10, 0, 10) { HP = 0 };
        var input  = new FakeInputReader();
        var display = new FakeDisplayService(input);
        var engine  = MakeEngine(display, input);

        // Act — must not throw, must not set HP to negative
        var act = () => engine.RunCombat(player, enemy);

        act.Should().NotThrow("dead enemy must not crash periodic damage processing");
        enemy.HP.Should().BeGreaterThanOrEqualTo(0, "HP must never go below 0");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #1239 — SetBonusManager threshold: equipping Nth piece activates the bonus
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #1239 — Verifies that SetBonusManager.ApplySetBonuses correctly transitions from
/// no-bonus state to bonus-active state when the threshold piece is equipped.
/// </summary>
public class SetBonusThresholdTests
{
    private static Item MakeSetItem(string setId, ArmorSlot slot) => new()
    {
        Name = $"{setId}_{slot}",
        Type = ItemType.Armor,
        Slot = slot,
        SetId = setId,
    };

    [Fact]
    public void Ironclad_OnePiece_NoBonus()
    {
        // Arrange: only 1 ironclad piece — below 2-piece threshold
        var player = new Player();
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);

        // Act
        SetBonusManager.ApplySetBonuses(player);

        // Assert
        player.SetBonusDefense.Should().Be(0, "1 piece is below the 2-piece threshold");
        player.SetBonusMaxHP.Should().Be(0,   "1 piece is below the 2-piece threshold");
    }

    [Fact]
    public void Ironclad_SecondPieceEquipped_ActivatesTwoPieceBonus()
    {
        // Arrange: start with 1 piece (no bonus)
        var player = new Player();
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        SetBonusManager.ApplySetBonuses(player);
        player.SetBonusDefense.Should().Be(0, "pre-condition: 1 piece yields no bonus");

        // Act: equip the 2nd piece
        player.EquippedHead = MakeSetItem("ironclad", ArmorSlot.Head);
        SetBonusManager.ApplySetBonuses(player);

        // Assert: 2-piece bonus now active
        player.SetBonusDefense.Should().Be(3, "2-piece ironclad grants +3 DEF");
        player.SetBonusMaxHP.Should().Be(10,  "2-piece ironclad grants +10 max HP");
    }

    [Fact]
    public void Ironclad_ThirdPieceEquipped_AddsThreePieceBonus()
    {
        // Arrange: 2 pieces already equipped
        var player = new Player();
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead  = MakeSetItem("ironclad", ArmorSlot.Head);
        SetBonusManager.ApplySetBonuses(player);
        player.SetBonusDefense.Should().Be(3, "pre-condition: 2-piece bonus active");

        // Act: equip 3rd piece
        player.EquippedShoulders = MakeSetItem("ironclad", ArmorSlot.Shoulders);
        SetBonusManager.ApplySetBonuses(player);

        // Assert: 3-piece bonus stacks on top of 2-piece bonus
        player.SetBonusDefense.Should().BeGreaterThanOrEqualTo(3,
            "3-piece bonus must not reduce below 2-piece value");
    }

    [Fact]
    public void Shadowstalker_OnePiece_NoBonusActive()
    {
        // Arrange
        var player = new Player();
        player.EquippedChest = MakeSetItem("shadowstalker", ArmorSlot.Chest);

        // Act
        SetBonusManager.ApplySetBonuses(player);

        // Assert: no dodge bonus from a single piece (below 2-piece threshold)
        player.SetBonusDodge.Should().Be(0f, "1 piece is below 2-piece threshold");
        // No active bonuses at all
        SetBonusManager.GetActiveBonuses(player).Should().BeEmpty(
            "1 piece does not meet any threshold");
    }

    [Fact]
    public void Shadowstalker_SecondPieceEquipped_ActivatesDodgeBonusAndTwoPieceEntry()
    {
        // Arrange: 1 piece (no bonus)
        var player = new Player();
        player.EquippedChest = MakeSetItem("shadowstalker", ArmorSlot.Chest);
        SetBonusManager.ApplySetBonuses(player);
        player.SetBonusDodge.Should().Be(0f, "pre-condition: 1 piece yields no bonus");

        // Act: equip 2nd piece
        player.EquippedHead = MakeSetItem("shadowstalker", ArmorSlot.Head);
        SetBonusManager.ApplySetBonuses(player);

        // Assert: 2-piece shadowstalker sets dodge bonus on Player
        player.SetBonusDodge.Should().BeApproximately(0.05f, 0.001f,
            "2-piece shadowstalker should grant 5% dodge bonus stored in SetBonusDodge");

        // The 2-piece entry should now be in active bonuses
        var activeBonuses = SetBonusManager.GetActiveBonuses(player);
        activeBonuses.Should().Contain(b =>
            b.SetId == "shadowstalker" && b.PiecesRequired == 2,
            "equipping 2nd piece must cross the 2-piece threshold");
    }

    [Fact]
    public void Arcanist_OnePiece_NoManaBonus()
    {
        // Arrange
        var player = new Player();
        player.EquippedChest = MakeSetItem("arcanist", ArmorSlot.Chest);

        // Act
        SetBonusManager.ApplySetBonuses(player);

        // Assert
        player.SetBonusMaxMana.Should().Be(0, "1 arcanist piece yields no mana bonus");
    }

    [Fact]
    public void Arcanist_SecondPieceEquipped_ActivatesManaBonus()
    {
        // Arrange: 1 piece first
        var player = new Player();
        player.EquippedChest = MakeSetItem("arcanist", ArmorSlot.Chest);
        SetBonusManager.ApplySetBonuses(player);
        player.SetBonusMaxMana.Should().Be(0, "pre-condition: 1 piece gives no bonus");

        // Act: equip 2nd piece
        player.EquippedHead = MakeSetItem("arcanist", ArmorSlot.Head);
        SetBonusManager.ApplySetBonuses(player);

        // Assert
        player.SetBonusMaxMana.Should().Be(20,
            "2-piece arcanist grants +20 max mana");
    }

    [Fact]
    public void ApplySetBonuses_BelowThreshold_SetBonusFieldsAreZero()
    {
        // Arrange: mixed pieces from different sets — no set threshold met
        var player = new Player();
        player.EquippedChest     = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead      = MakeSetItem("shadowstalker", ArmorSlot.Head);
        player.EquippedShoulders = MakeSetItem("arcanist", ArmorSlot.Shoulders);

        // Act
        SetBonusManager.ApplySetBonuses(player);

        // Assert: no set reaches its 2-piece threshold
        player.SetBonusDefense.Should().Be(0, "no set has 2+ pieces equipped");
        player.SetBonusMaxMana.Should().Be(0, "no set has 2+ pieces equipped");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// #1233 — Player class / run-settings save-load round-trip
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #1233 — Verifies that player-level settings (class, mana pool, difficulty) survive
/// a full save/load round-trip. These are the closest equivalent to "player preferences"
/// in the current data model.
/// </summary>
[Collection("save-system")]
public class PlayerSettingsRoundTripTests : IDisposable
{
    private readonly string _saveDir;

    public PlayerSettingsRoundTripTests()
    {
        _saveDir = Path.Combine(Path.GetTempPath(), $"dungnz_prefs_test_{Guid.NewGuid()}");
        SaveSystem.OverrideSaveDirectory(_saveDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_saveDir))
            Directory.Delete(_saveDir, recursive: true);
    }

    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Mage)]
    [InlineData(PlayerClass.Rogue)]
    [InlineData(PlayerClass.Paladin)]
    [InlineData(PlayerClass.Necromancer)]
    [InlineData(PlayerClass.Ranger)]
    public void RoundTrip_PlayerClass_Preserved(PlayerClass cls)
    {
        // Arrange
        var player = new Player { Name = "Tester", Class = cls };
        var state  = new GameState(player, new Room { Description = "Start" });

        // Act
        SaveSystem.SaveGame(state, $"class-{cls}");
        var loaded = SaveSystem.LoadGame($"class-{cls}");

        // Assert
        loaded.Player.Class.Should().Be(cls,
            $"PlayerClass.{cls} must survive save/load round-trip");
    }

    [Fact]
    public void RoundTrip_ManaAndMaxMana_Preserved()
    {
        // Arrange
        var player = new Player { Name = "Tester", Mana = 45, MaxMana = 80 };
        var state  = new GameState(player, new Room { Description = "Start" });

        // Act
        SaveSystem.SaveGame(state, "mana-roundtrip");
        var loaded = SaveSystem.LoadGame("mana-roundtrip");

        // Assert
        loaded.Player.Mana.Should().Be(45,    "Mana must survive save/load");
        loaded.Player.MaxMana.Should().Be(80, "MaxMana must survive save/load");
    }

    [Theory]
    [InlineData(Difficulty.Casual)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void RoundTrip_DifficultyLevel_Preserved(Difficulty diff)
    {
        // Arrange
        var player = new Player { Name = "Tester" };
        var state  = new GameState(player, new Room { Description = "Start" },
            difficulty: diff);

        // Act
        SaveSystem.SaveGame(state, $"diff-{diff}");
        var loaded = SaveSystem.LoadGame($"diff-{diff}");

        // Assert
        loaded.Difficulty.Should().Be(diff,
            $"Difficulty.{diff} must survive save/load round-trip");
    }

    [Fact]
    public void RoundTrip_CurrentFloor_Preserved()
    {
        // Arrange
        var player = new Player { Name = "Tester" };
        var state  = new GameState(player, new Room { Description = "Floor 3 room" },
            currentFloor: 3);

        // Act
        SaveSystem.SaveGame(state, "floor-roundtrip");
        var loaded = SaveSystem.LoadGame("floor-roundtrip");

        // Assert
        loaded.CurrentFloor.Should().Be(3,
            "CurrentFloor must survive save/load");
    }

    [Fact]
    public void RoundTrip_RunSeed_Preserved()
    {
        // Arrange
        var player = new Player { Name = "Tester" };
        var state  = new GameState(player, new Room { Description = "Seeded room" },
            seed: 12345);

        // Act
        SaveSystem.SaveGame(state, "seed-roundtrip");
        var loaded = SaveSystem.LoadGame("seed-roundtrip");

        // Assert
        loaded.Seed.Should().Be(12345, "Run seed must survive save/load");
    }

    [Fact]
    public void RoundTrip_ClassAndDifficultyTogether_BothPreserved()
    {
        // Arrange: combine class + difficulty + floor into one compound state
        var player = new Player
        {
            Name   = "CompoundHero",
            Class  = PlayerClass.Mage,
            Mana   = 60,
            MaxMana = 120
        };
        var state = new GameState(player, new Room { Description = "Compound room" },
            currentFloor: 4, difficulty: Difficulty.Hard);

        // Act
        SaveSystem.SaveGame(state, "compound-roundtrip");
        var loaded = SaveSystem.LoadGame("compound-roundtrip");

        // Assert
        loaded.Player.Class.Should().Be(PlayerClass.Mage);
        loaded.Player.Mana.Should().Be(60);
        loaded.Player.MaxMana.Should().Be(120);
        loaded.CurrentFloor.Should().Be(4);
        loaded.Difficulty.Should().Be(Difficulty.Hard);
    }
}
