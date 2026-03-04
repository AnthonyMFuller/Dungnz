using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #1003 — Tests for DungeonBoss.CheckEnrage, Lich-related undead mechanics,
/// boss phase definitions, and charge attack states.
/// </summary>
public class DungeonBossEnrageTests
{
    [Fact]
    public void CheckEnrage_AtExactly40Percent_TriggersEnrage()
    {
        var boss = new DungeonBoss { HP = 40, MaxHP = 100, Attack = 20 };
        boss.CheckEnrage();

        boss.IsEnraged.Should().BeTrue();
        boss.Attack.Should().Be(30, "attack should increase by 50% (20 * 1.5 = 30)");
    }

    [Fact]
    public void CheckEnrage_Below40Percent_TriggersEnrage()
    {
        var boss = new DungeonBoss { HP = 20, MaxHP = 100, Attack = 20 };
        boss.CheckEnrage();

        boss.IsEnraged.Should().BeTrue();
        boss.Attack.Should().Be(30);
    }

    [Fact]
    public void CheckEnrage_Above40Percent_DoesNotTrigger()
    {
        var boss = new DungeonBoss { HP = 50, MaxHP = 100, Attack = 20 };
        boss.CheckEnrage();

        boss.IsEnraged.Should().BeFalse();
        boss.Attack.Should().Be(20);
    }

    [Fact]
    public void CheckEnrage_CalledTwice_OnlyAppliesOnce()
    {
        var boss = new DungeonBoss { HP = 30, MaxHP = 100, Attack = 20 };
        boss.CheckEnrage();
        boss.CheckEnrage();

        boss.Attack.Should().Be(30, "enrage should only apply once, not stack");
    }

    [Fact]
    public void CheckEnrage_AlreadyEnraged_NoDoubleBoost()
    {
        var boss = new DungeonBoss { HP = 10, MaxHP = 100, Attack = 20 };
        boss.IsEnraged = true;
        boss.CheckEnrage();

        boss.Attack.Should().Be(20, "attack shouldn't change if already enraged");
    }

    // ── Charge Attack ────────────────────────────────────────────────────────

    [Fact]
    public void ChargeState_DefaultsFalse()
    {
        var boss = new DungeonBoss();
        boss.IsCharging.Should().BeFalse();
        boss.ChargeActive.Should().BeFalse();
    }

    [Fact]
    public void ChargeState_CanBeToggled()
    {
        var boss = new DungeonBoss();
        boss.IsCharging = true;
        boss.IsCharging.Should().BeTrue();

        boss.ChargeActive = true;
        boss.ChargeActive.Should().BeTrue();
    }

    // ── Lich Undead Mechanics ────────────────────────────────────────────────

    [Fact]
    public void ArchlichSovereign_IsUndead_AndHasPhases()
    {
        var boss = new ArchlichSovereign();
        boss.IsUndead.Should().BeTrue();
        boss.Phases.Should().ContainSingle(p => p.AbilityName == "DeathShroud");
        boss.Phases[0].HpPercent.Should().Be(0.50);
    }

    [Fact]
    public void LichKing_IsUndead_AndAppliesPoison()
    {
        var boss = new LichKing();
        boss.IsUndead.Should().BeTrue();
        boss.AppliesPoisonOnHit.Should().BeTrue();
    }

    [Fact]
    public void BoneArchon_IsUndead_AndHasWeakenAuraPhase()
    {
        var boss = new BoneArchon();
        boss.IsUndead.Should().BeTrue();
        boss.Phases.Should().ContainSingle(p => p.AbilityName == "WeakenAura");
    }

    // ── Boss Phase Definitions ───────────────────────────────────────────────

    [Fact]
    public void GoblinWarchief_HasReinforcementsAt50Percent()
    {
        var boss = new GoblinWarchief();
        boss.Phases.Should().ContainSingle(p =>
            p.AbilityName == "Reinforcements" && p.HpPercent == 0.50);
    }

    [Fact]
    public void InfernalDragon_HasFlameBreathPhase()
    {
        var boss = new InfernalDragon();
        boss.Phases.Should().ContainSingle(p => p.AbilityName == "FlameBreath");
    }

    [Fact]
    public void CrimsonVampire_HasBloodDrainAt25Percent()
    {
        var boss = new CrimsonVampire();
        boss.Phases.Should().ContainSingle(p =>
            p.AbilityName == "BloodDrain" && p.HpPercent == 0.25);
    }

    [Fact]
    public void FiredPhases_InitiallyEmpty()
    {
        var boss = new DungeonBoss();
        boss.FiredPhases.Should().BeEmpty();
    }

    [Fact]
    public void FiredPhases_TracksFiredAbilities()
    {
        var boss = new DungeonBoss();
        boss.FiredPhases.Add("TestAbility");
        boss.FiredPhases.Should().Contain("TestAbility");
    }
}
