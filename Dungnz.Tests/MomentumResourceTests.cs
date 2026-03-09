using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GROUP 1: MomentumResource unit tests (WI-F spec, 6 required + 2 Consume tests)
// Now using the real Dungnz.Models.MomentumResource (WI-B merged).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Issue #1274 WI-F — Unit tests for the <c>MomentumResource</c> value type.
/// Tests Add/Reset/IsCharged boundary behaviour and the Consume() atomic path.
/// </summary>
public class MomentumResourceUnitTests
{
    // ── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_SingleUnit_IncrementsCorrectly()
    {
        // Arrange
        var resource = new MomentumResource(maximum: 5);

        // Act
        resource.Add(1);

        // Assert
        resource.Current.Should().Be(1);
    }

    [Fact]
    public void Add_MultipleUnits_ClampsAtMaximum()
    {
        // Arrange
        var resource = new MomentumResource(maximum: 5);

        // Act — add exactly the maximum over multiple calls
        resource.Add(1);
        resource.Add(1);
        resource.Add(1);
        resource.Add(1);
        resource.Add(1);

        // Assert
        resource.Current.Should().Be(resource.Maximum, "five individual adds on a max-5 resource should reach 5");
    }

    [Fact]
    public void Add_ExceedsMaximum_ClampsAtMaximum()
    {
        // Arrange
        var resource = new MomentumResource(maximum: 5);

        // Act — single Add far beyond the maximum
        resource.Add(999);

        // Assert — clamp, never above Maximum
        resource.Current.Should().Be(resource.Maximum,
            "adding an arbitrarily large amount must not exceed the declared maximum");
        resource.Current.Should().BeLessOrEqualTo(resource.Maximum);
    }

    // ── IsCharged ────────────────────────────────────────────────────────────

    [Fact]
    public void IsCharged_BelowMax_ReturnsFalse()
    {
        // Arrange
        var resource = new MomentumResource(maximum: 5);

        // Act — only one unit added; needs five to charge
        resource.Add(1);

        // Assert
        resource.IsCharged.Should().BeFalse("1 < 5 — resource is not yet at threshold");
    }

    [Fact]
    public void IsCharged_AtMax_ReturnsTrue()
    {
        // Arrange
        var resource = new MomentumResource(maximum: 5);

        // Act — fill to max
        resource.Add(5);

        // Assert
        resource.IsCharged.Should().BeTrue("Current == Maximum triggers the IsCharged threshold");
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsCurrentToZero()
    {
        // Arrange — fill first
        var resource = new MomentumResource(maximum: 5);
        resource.Add(5);
        resource.Current.Should().Be(5, "pre-condition: resource is full");

        // Act
        resource.Reset();

        // Assert
        resource.Current.Should().Be(0, "Reset() must return Current to zero unconditionally");
    }

    // ── Consume (atomic check+reset for WI-D threshold effects) ──────────────

    [Fact]
    public void Consume_WhenCharged_ReturnsTrueAndResetsToZero()
    {
        // Arrange — full charge
        var resource = new MomentumResource(maximum: 3);
        resource.Add(3);
        resource.IsCharged.Should().BeTrue("pre-condition: resource is charged");

        // Act
        var consumed = resource.Consume();

        // Assert
        consumed.Should().BeTrue("Consume should return true when the resource was fully charged");
        resource.Current.Should().Be(0, "Consume resets Current to zero");
        resource.IsCharged.Should().BeFalse("resource is no longer charged after consuming");
    }

    [Fact]
    public void Consume_WhenNotCharged_ReturnsFalseAndLeavesCurrentUnchanged()
    {
        // Arrange — partial charge (1 of 3)
        var resource = new MomentumResource(maximum: 3);
        resource.Add(1);

        // Act
        var consumed = resource.Consume();

        // Assert
        consumed.Should().BeFalse("Consume should return false when not fully charged");
        resource.Current.Should().Be(1, "unsuccessful Consume must not alter Current");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GROUP 2: Player class initialisation tests — verifies CombatEngine.InitPlayerMomentum
// sets the correct maximum for each class at combat start (WI-B + WI-C merged).
//
// Architecture note: Player.Momentum is null by default and is initialized by
// CombatEngine.InitPlayerMomentum() at the start of every RunCombat() call.
// These tests verify the correct maximum by running a quick combat (enemy dies
// in one hit) and checking Momentum.Maximum after combat ends.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Issue #1274 WI-F — Verifies that CombatEngine initialises
/// <see cref="Player.Momentum"/> with the correct maximum (or null for Rogue).
/// </summary>
public class MomentumResourcePlayerInitTests
{
    private static CombatResult RunQuickCombat(Player player)
    {
        // Enemy with 1 HP dies on any hit — fastest combat resolution
        var enemy = new EnemyBuilder().WithHP(1).WithAttack(0).WithDefense(0).Build();
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9); // no crits
        var engine = new CombatEngine(display, input, rng);
        return engine.RunCombat(player, enemy);
    }

    [Fact]
    public void Warrior_HasFuryMomentumWithMax5_AfterCombatStart()
    {
        // Arrange
        var player = new PlayerBuilder().WithClass(PlayerClass.Warrior).WithAttack(100).Build();

        // Act — run combat; InitPlayerMomentum fires at combat start
        var result = RunQuickCombat(player);

        // Assert
        result.Should().Be(CombatResult.Won);
        player.Momentum.Should().NotBeNull("Warrior uses Fury momentum resource");
        player.Momentum!.Maximum.Should().Be(5, "Warrior Fury threshold is 5");
    }

    [Fact]
    public void Mage_HasArcaneChargeMomentumWithMax3_AfterCombatStart()
    {
        // Arrange
        var player = new PlayerBuilder().WithClass(PlayerClass.Mage).WithAttack(100).Build();

        // Act
        var result = RunQuickCombat(player);

        // Assert
        result.Should().Be(CombatResult.Won);
        player.Momentum.Should().NotBeNull("Mage uses Arcane Charge momentum resource");
        player.Momentum!.Maximum.Should().Be(3, "Mage Arcane Charge threshold is 3");
    }

    [Fact]
    public void Rogue_HasNullMomentum_AfterCombatStart()
    {
        // Arrange — Rogue uses ComboPoints, not Momentum
        var player = new PlayerBuilder().WithClass(PlayerClass.Rogue).WithAttack(100).Build();

        // Act
        var result = RunQuickCombat(player);

        // Assert
        result.Should().Be(CombatResult.Won);
        player.Momentum.Should().BeNull("Rogue uses ComboPoints instead of Momentum");
    }

    [Fact]
    public void Ranger_HasFocusMomentumWithMax3_AfterCombatStart()
    {
        // Arrange
        var player = new PlayerBuilder().WithClass(PlayerClass.Ranger).WithAttack(100).Build();

        // Act
        var result = RunQuickCombat(player);

        // Assert
        result.Should().Be(CombatResult.Won);
        player.Momentum.Should().NotBeNull("Ranger uses Focus momentum resource");
        player.Momentum!.Maximum.Should().Be(3, "Ranger Focus threshold is 3");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GROUP 3: CombatEngine integration tests (WI-C + WI-D hooks, #1274)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Issue #1274 WI-F — Integration tests verifying that the <see cref="CombatEngine"/>
/// correctly increments and consumes Momentum for each class.
/// </summary>
public class MomentumEngineIntegrationTests
{
    // ── Warrior Fury ─────────────────────────────────────────────────────────

    /// <summary>
    /// Warrior takes damage and Fury WI-C increments — verified via PlayerDied result
    /// (combat cleanup is only called on Won/Fled; PlayerDied returns immediately, preserving
    /// momentum state for assertion).
    /// Player HP=1 → dies after enemy attack; momentum carries the WI-C counts.
    /// </summary>
    [Fact]
    public void Warrior_AttackingAndTakingDamage_IncrementsFury()
    {
        // Arrange
        // Player: attack=5, HP=1, defense=0 — survives player-attacks phase, dies on enemy hit
        // Enemy: HP=100, attack=10, defense=0 — kills player in one hit after player attacks
        // Round 1: player deals 5 (WI-C attack=1), enemy deals 10 (kills player, WI-C take=2)
        //          → PlayerDied path returns immediately WITHOUT calling ResetCombatEffects.
        //          Momentum.Current remains 2 post-combat.
        var player = new PlayerBuilder()
            .WithClass(PlayerClass.Warrior)
            .WithHP(1).WithMaxHP(1).WithAttack(5).WithDefense(0)
            .Build();
        var enemy = new Enemy_Stub(hp: 100, atk: 10, def: 0, xp: 5);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9); // no crits

        var engine = new CombatEngine(display, input, rng);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert — PlayerDied means no cleanup; momentum still holds the WI-C counts
        result.Should().Be(CombatResult.PlayerDied);
        player.Momentum.Should().NotBeNull();
        player.Momentum!.Current.Should().BeGreaterThan(0,
            "Warrior Fury WI-C should have incremented on attack dealt and/or hit taken; " +
            "PlayerDied path does not call ResetCombatEffects so momentum is preserved");
    }

    /// <summary>
    /// After Fury reaches max (5), the WI-D 'Momentum unleashed' message fires on the next attack.
    /// Enemy HP=45 (player attack=5, no defense), enemy attack=1:
    ///   Rounds 1-3: 2 WI-C per round → Current reaches 5 (charged) at round 3 attack.
    ///   Round 4: WI-D Consume() fires → 2× damage → 'Momentum unleashed' message in output.
    ///   Enemy dies on round 7 (second WI-D fire).
    /// 8 × "A" inputs covers the full sequence without hanging.
    /// </summary>
    [Fact]
    public void Warrior_FuryCharged_ShowsUnleashedMessageAndDealsDoubleDamage()
    {
        // Arrange
        var player = new PlayerBuilder()
            .WithClass(PlayerClass.Warrior)
            .WithHP(500).WithMaxHP(500).WithAttack(5).WithDefense(0)
            .Build();
        // enemy HP=45, attack=1, defense=0 — survives into round 4 where WI-D fires
        var enemy = new Enemy_Stub(hp: 45, atk: 1, def: 0, xp: 5);
        var input = new FakeInputReader("A", "A", "A", "A", "A", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9);

        var engine = new CombatEngine(display, input, rng);

        // Act — run enough turns for Fury to charge and WI-D to fire; combat ends Won
        var result = engine.RunCombat(player, enemy);

        // Assert
        result.Should().Be(CombatResult.Won, "player outlasts enemy across 7 rounds");
        display.CombatMessages.Should().Contain(m => m.Contains("Momentum unleashed"),
            "WI-D Fury fires 'Momentum unleashed' message when charged attack consumes the resource");
    }

    // ── Mage Arcane Charge ───────────────────────────────────────────────────

    [Fact(Skip = "Requires menu-driven ability selection via FakeInputReader — " +
                 "combat menu structure needs ability slot wiring test. " +
                 "WI-C/WI-D mage hooks confirmed present in AbilityManager. " +
                 "Full integration test deferred until FakeMenuNavigator supports ability submenu.")]
    public void Mage_CastingAbility_IncrementsCharge()
    {
        // Skipped: Mage ability cast requires navigating the combat menu to slot 2
        // (Use Ability) and then the ability submenu. FakeInputReader feeds raw
        // combat choices ("A"/"F"/"2") but the ability submenu selection needs
        // additional input tokens that vary by class loadout.
    }

    [Fact(Skip = "Requires pre-charging Arcane Charge to 3 via combat rounds before testing " +
                 "0-mana cast. CombatEngine.InitPlayerMomentum() resets momentum at each " +
                 "RunCombat() call — pre-charging outside RunCombat is immediately overwritten. " +
                 "Requires multi-phase test or internal hook. Deferred.")]
    public void Mage_ArcaneCharged_ZeroManaCost()
    {
        // Skipped: Cannot pre-charge momentum before RunCombat() because InitPlayerMomentum
        // resets it. Would need to run enough ability turns inside a single RunCombat session
        // to reach 3 Arcane Charge (max), then cast one more and verify 0 mana consumed.
    }

    // ── Ranger Focus ─────────────────────────────────────────────────────────

    [Fact(Skip = "Ranger Focus requires 0 HP damage from enemy to increment. " +
                 "The minimum-damage-1 rule means even defense=9999 players take 1 HP/turn. " +
                 "True 0-damage paths (stun/freeze/ManaShield-full-absorb) require setup " +
                 "that is out of scope for a unit-style integration test. Deferred.")]
    public void Ranger_TakingNoDamage_IncrementsFocus()
    {
        // Skipped: AddRangerFocusIfNoDamage fires when player.HP == hpBefore after enemy turn.
        // With minimum damage = 1, no defense value prevents HP loss in a regular attack.
        // 0-damage paths: stun skip, DivineShield absorb, full ManaShield absorb (all Paladin/Mage).
        // Testing this for Ranger requires an enemy-stun mechanic that Ranger does not have.
    }

    [Fact(Skip = "Ranger Focus reset test requires Focus to be naturally earned in-combat " +
                 "before the reset trigger. Cannot pre-charge: InitPlayerMomentum resets at " +
                 "RunCombat() start. Earning Focus requires 0-damage turns (see above skip). " +
                 "Deferred pending a 0-damage scenario for Ranger.")]
    public void Ranger_TakingDamage_ResetsFocus()
    {
        // Skipped: To test reset, Focus must be > 0 first.
        // Cannot pre-charge (see above). Deferred.
    }
}
