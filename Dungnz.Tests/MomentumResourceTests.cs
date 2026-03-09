using Dungnz.Models;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// TEMPORARY STUB — Remove this block once WI-B (Dungnz.Models/MomentumResource.cs)
// is merged. The real class lives in Dungnz.Models; this file-scoped copy lets
// MomentumResourceUnitTests compile and run against the spec before the
// implementation ships.
//
// When removing: add `using Dungnz.Models;` to the top of this file and delete
// the `file sealed class MomentumResource` block below.
// ─────────────────────────────────────────────────────────────────────────────
file sealed class MomentumResource
{
    public int Current { get; private set; }
    public int Maximum { get; }
    public bool IsCharged => Current >= Maximum;

    public MomentumResource(int maximum) { Maximum = maximum; }

    public void Add(int amount = 1) => Current = Math.Clamp(Current + amount, 0, Maximum);

    /// <summary>
    /// Consumes a full charge: returns true and resets Current to 0 if charged,
    /// returns false and does nothing if not yet charged.
    /// </summary>
    public bool Consume()
    {
        if (!IsCharged) return false;
        Current = 0;
        return true;
    }

    public void Reset() => Current = 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// GROUP 1: MomentumResource unit tests (WI-F spec, 6 required + 2 Consume tests)
// These run immediately using the file-scoped stub above.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Issue #1274 WI-F — Unit tests for the <c>MomentumResource</c> value type.
/// Tests Add/Reset/IsCharged boundary behaviour. Uses a file-scoped stub until
/// WI-B (Dungnz.Models/MomentumResource.cs) is merged.
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

    // ── Consume (Coulson spec — WI-D integration path) ────────────────────────

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
// GROUP 2: Player class initialisation tests
// SKIPPED until WI-B (Player.Momentum wiring) merges.
// When WI-B ships: remove [Fact(Skip = ...)] and uncomment assertion bodies.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Issue #1274 WI-F — Verifies that each <see cref="PlayerClass"/> initialises
/// <see cref="Player.Momentum"/> with the correct maximum (or null for classes
/// that use a different resource, e.g. Rogue with ComboPoints).
/// Skipped until WI-B merges.
/// </summary>
public class MomentumResourcePlayerInitTests
{
    [Fact(Skip = "WI-B pending — Player.Momentum not yet wired to PlayerClassDefinition")]
    public void Player_Warrior_HasMomentumWithMax5()
    {
        // TODO: Uncomment and run once WI-B (Player.Momentum init) merges.
        //
        // var player = new PlayerBuilder().WithClass(PlayerClass.Warrior).Build();
        // player.Momentum.Should().NotBeNull("Warrior uses Fury momentum resource");
        // player.Momentum!.Maximum.Should().Be(5, "Warrior Fury threshold is 5");
    }

    [Fact(Skip = "WI-B pending — Player.Momentum not yet wired to PlayerClassDefinition")]
    public void Player_Mage_HasMomentumWithMax3()
    {
        // TODO: Uncomment and run once WI-B (Player.Momentum init) merges.
        //
        // var player = new PlayerBuilder().WithClass(PlayerClass.Mage).Build();
        // player.Momentum.Should().NotBeNull("Mage uses Arcane Charge momentum resource");
        // player.Momentum!.Maximum.Should().Be(3, "Mage Arcane Charge threshold is 3");
    }

    [Fact(Skip = "WI-B pending — Player.Momentum not yet wired to PlayerClassDefinition")]
    public void Player_Rogue_HasNullMomentum()
    {
        // TODO: Uncomment and run once WI-B (Player.Momentum init) merges.
        // Rogue uses ComboPoints (separate resource) — Momentum should be null.
        //
        // var player = new PlayerBuilder().WithClass(PlayerClass.Rogue).Build();
        // player.Momentum.Should().BeNull("Rogue uses ComboPoints, not Momentum");
    }

    [Fact(Skip = "WI-B pending — Player.Momentum not yet wired to PlayerClassDefinition")]
    public void Player_Ranger_HasMomentumWithMax3()
    {
        // TODO: Uncomment and run once WI-B (Player.Momentum init) merges.
        //
        // var player = new PlayerBuilder().WithClass(PlayerClass.Ranger).Build();
        // player.Momentum.Should().NotBeNull("Ranger uses Focus momentum resource");
        // player.Momentum!.Maximum.Should().Be(3, "Ranger Focus threshold is 3");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GROUP 3: CombatEngine integration tests
// SKIPPED until WI-C and WI-D (CombatEngine momentum hooks) merge.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Issue #1274 WI-F — Integration tests verifying that the <see cref="CombatEngine"/>
/// correctly increments and consumes Momentum for each class.
/// All tests are skipped until WI-C/WI-D merge — they are present so CI shows the
/// pending coverage gap and they don't block CI.
/// </summary>
public class MomentumEngineIntegrationTests
{
    // ── Warrior Fury ─────────────────────────────────────────────────────────

    /// <summary>
    /// Warrior taking damage should increment Fury (Momentum.Current > 0 after hit).
    /// WI-C wires Momentum.Add() in PerformEnemyTurn when enemy deals damage.
    /// </summary>
    [Fact(Skip = "WI-C/WI-D pending — CombatEngine momentum hooks not yet implemented")]
    public void Warrior_TakingDamage_IncrementsFury()
    {
        // TODO: Implement once WI-C merges.
        //
        // Arrange
        // var player = new PlayerBuilder()
        //     .WithClass(PlayerClass.Warrior)
        //     .WithHP(100).WithMaxHP(100).WithDefense(0)
        //     .Build();
        // var enemy = new EnemyBuilder().WithHP(9999).WithAttack(10).Build();
        // var input = new FakeInputReader("F"); // flee immediately after first enemy hit
        // var display = new FakeDisplayService(input);
        // var rng = new ControlledRandom(defaultDouble: 0.9); // no crits, flee fails but that's OK
        // var engine = new CombatEngine(display, input, rng);
        //
        // Act — run one turn where enemy attacks
        // engine.RunCombat(player, enemy);
        //
        // Assert
        // player.Momentum.Should().NotBeNull();
        // player.Momentum!.Current.Should().BeGreaterThan(0,
        //     "Warrior Fury should increment when the warrior takes damage");
    }

    /// <summary>
    /// When Warrior Fury is fully charged (Current == 5), the next attack should
    /// deal doubled damage and consume the charge.
    /// WI-D wires the threshold effect and Consume() call.
    /// </summary>
    [Fact(Skip = "WI-C/WI-D pending — CombatEngine momentum hooks not yet implemented")]
    public void Warrior_FuryCharged_DoublesNextAttack()
    {
        // TODO: Implement once WI-D merges.
        //
        // Arrange — pre-set Fury to 5 (charged) via direct field access
        // var player = new PlayerBuilder()
        //     .WithClass(PlayerClass.Warrior)
        //     .WithHP(100).WithAttack(20).WithDefense(5)
        //     .Build();
        // player.Momentum!.Add(5); // pre-charge
        // var enemy = new EnemyBuilder().WithHP(1000).WithDefense(0).Build();
        // var input = new FakeInputReader("A"); // attack
        // var display = new FakeDisplayService(input);
        // var rng = new ControlledRandom(defaultDouble: 0.9);
        // var engine = new CombatEngine(display, input, rng);
        // int normalDamage = player.Attack; // baseline without Fury
        //
        // Act
        // engine.RunCombat(player, enemy);
        //
        // Assert — enemy took >= 2× attack (Fury doubles damage on the charged swing)
        // var dmgDealt = 1000 - enemy.HP;
        // dmgDealt.Should().BeGreaterOrEqualTo(normalDamage * 2,
        //     "a fully-charged Fury swing deals at least double the base attack");
        // player.Momentum!.Current.Should().Be(0,
        //     "Fury resets to zero after the charged attack fires");
    }

    // ── Mage Arcane Charge ───────────────────────────────────────────────────

    /// <summary>
    /// Casting an ability should increment Mage's Arcane Charge by one.
    /// WI-C wires Momentum.Add() in AbilityProcessor after any ability resolves.
    /// </summary>
    [Fact(Skip = "WI-C/WI-D pending — CombatEngine momentum hooks not yet implemented")]
    public void Mage_CastingAbility_IncrementsCharge()
    {
        // TODO: Implement once WI-C merges.
        //
        // Arrange
        // var player = new PlayerBuilder()
        //     .WithClass(PlayerClass.Mage)
        //     .WithHP(100).WithMana(50).WithMaxMana(50)
        //     .Build();
        // var enemy = new EnemyBuilder().WithHP(9999).Build();
        // var input = new FakeInputReader("2"); // cast first ability (slot 2 in combat menu)
        // var display = new FakeDisplayService(input);
        // var rng = new ControlledRandom(defaultDouble: 0.9);
        // var engine = new CombatEngine(display, input, rng);
        //
        // Act
        // engine.RunCombat(player, enemy);
        //
        // Assert
        // player.Momentum.Should().NotBeNull();
        // player.Momentum!.Current.Should().BeGreaterThan(0,
        //     "Arcane Charge should increment after any ability cast");
    }

    /// <summary>
    /// When Mage Arcane Charge reaches 3, the next ability should cost 0 mana.
    /// WI-D wires the zero-mana override in the ability resolve path.
    /// </summary>
    [Fact(Skip = "WI-C/WI-D pending — CombatEngine momentum hooks not yet implemented")]
    public void Mage_ArcaneCharged_ZeroManaCost()
    {
        // TODO: Implement once WI-D merges.
        //
        // Arrange — pre-charge to 3 (IsCharged == true)
        // var player = new PlayerBuilder()
        //     .WithClass(PlayerClass.Mage)
        //     .WithHP(100).WithMana(5).WithMaxMana(100)
        //     .Build();
        // player.Momentum!.Add(3); // pre-charge
        // int manaBeforeCast = player.Mana; // == 5
        // var enemy = new EnemyBuilder().WithHP(9999).Build();
        // var input = new FakeInputReader("2"); // cast ability
        // var display = new FakeDisplayService(input);
        // var rng = new ControlledRandom(defaultDouble: 0.9);
        // var engine = new CombatEngine(display, input, rng);
        //
        // Act
        // engine.RunCombat(player, enemy);
        //
        // Assert — mana unchanged (0-cost cast consumed the charge)
        // player.Mana.Should().Be(manaBeforeCast,
        //     "ArcaneCharged ability costs 0 mana — Mana should not decrease");
        // player.Momentum!.Current.Should().Be(0, "Charge consumed after zero-mana cast");
    }

    // ── Ranger Focus ─────────────────────────────────────────────────────────

    /// <summary>
    /// When the enemy's attack deals 0 damage (dodge/miss), Ranger Focus should
    /// increment by 1.
    /// WI-C wires Momentum.Add() in PerformEnemyTurn when damage dealt == 0.
    /// </summary>
    [Fact(Skip = "WI-C/WI-D pending — CombatEngine momentum hooks not yet implemented")]
    public void Ranger_TakingNoDamage_IncrementsFocus()
    {
        // TODO: Implement once WI-C merges.
        //
        // Arrange — give Ranger very high DEF to ensure 0 net damage from enemy
        // var player = new PlayerBuilder()
        //     .WithClass(PlayerClass.Ranger)
        //     .WithHP(100).WithDefense(9999)
        //     .Build();
        // var enemy = new EnemyBuilder().WithHP(9999).WithAttack(1).Build();
        // var input = new FakeInputReader("F"); // flee after one turn
        // var display = new FakeDisplayService(input);
        // var rng = new ControlledRandom(defaultDouble: 0.9); // no dodge proc needed; raw dmg is 0
        // var engine = new CombatEngine(display, input, rng);
        //
        // Act
        // engine.RunCombat(player, enemy);
        //
        // Assert
        // player.Momentum.Should().NotBeNull();
        // player.Momentum!.Current.Should().BeGreaterThan(0,
        //     "Ranger Focus increments when the enemy attack deals 0 net damage");
    }

    /// <summary>
    /// When Ranger already has 2 Focus charges and then takes actual HP damage,
    /// Focus should reset to 0.
    /// WI-C wires Momentum.Reset() in PerformEnemyTurn when HP damage > 0.
    /// </summary>
    [Fact(Skip = "WI-C/WI-D pending — CombatEngine momentum hooks not yet implemented")]
    public void Ranger_TakingDamage_ResetsFocus()
    {
        // TODO: Implement once WI-C merges.
        //
        // Arrange — pre-charge Focus to 2, then expose ranger to real damage
        // var player = new PlayerBuilder()
        //     .WithClass(PlayerClass.Ranger)
        //     .WithHP(100).WithDefense(0)
        //     .Build();
        // player.Momentum!.Add(2);
        // player.Momentum.Current.Should().Be(2, "pre-condition: Focus is 2");
        // var enemy = new EnemyBuilder().WithHP(9999).WithAttack(10).Build();
        // var input = new FakeInputReader("F"); // flee after enemy hits
        // var display = new FakeDisplayService(input);
        // var rng = new ControlledRandom(defaultDouble: 0.9);
        // var engine = new CombatEngine(display, input, rng);
        //
        // Act
        // engine.RunCombat(player, enemy);
        //
        // Assert
        // player.Momentum!.Current.Should().Be(0,
        //     "Focus resets to 0 whenever the Ranger takes actual HP damage");
    }
}
