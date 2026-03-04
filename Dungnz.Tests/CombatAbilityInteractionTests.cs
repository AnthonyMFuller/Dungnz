using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for combat ability interactions (#947):
/// Silence prevents spell casting, Stun prevents action, and related edge cases.
/// </summary>
public class CombatAbilityInteractionTests
{
    private static (StatusEffectManager mgr, TestDisplayService display, Player player, Enemy_Stub enemy) MakeFixture()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var player = new Player { Name = "Hero", Mana = 50, MaxMana = 50, Class = PlayerClass.Warrior, Level = 5 };
        var enemy = new Enemy_Stub(50, 10, 2, 15);
        return (mgr, display, player, enemy);
    }

    // ── Silence blocks abilities ──────────────────────────────────────────────

    [Fact]
    public void Silence_IsApplied_HasEffectReturnsTrue()
    {
        var (mgr, _, player, _) = MakeFixture();

        mgr.Apply(player, StatusEffect.Silence, 3);

        mgr.HasEffect(player, StatusEffect.Silence).Should().BeTrue();
    }

    [Fact]
    public void Silence_ExpiresAfterDuration()
    {
        var (mgr, _, player, _) = MakeFixture();
        mgr.Apply(player, StatusEffect.Silence, 1);

        mgr.ProcessTurnStart(player); // duration 1 → 0, removed

        mgr.HasEffect(player, StatusEffect.Silence).Should().BeFalse();
    }

    [Fact]
    public void Silence_DoesNotDealTickDamage()
    {
        var (mgr, _, player, _) = MakeFixture();
        player.SetHPDirect(100);
        mgr.Apply(player, StatusEffect.Silence, 3);

        mgr.ProcessTurnStart(player);

        player.HP.Should().Be(100);
    }

    // ── Stun blocks action ────────────────────────────────────────────────────

    [Fact]
    public void Stun_IsApplied_HasEffectReturnsTrue()
    {
        var (mgr, _, _, enemy) = MakeFixture();

        mgr.Apply(enemy, StatusEffect.Stun, 2);

        mgr.HasEffect(enemy, StatusEffect.Stun).Should().BeTrue();
    }

    [Fact]
    public void Stun_ExpiresAfterDuration()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Stun, 1);

        mgr.ProcessTurnStart(enemy);

        mgr.HasEffect(enemy, StatusEffect.Stun).Should().BeFalse();
    }

    [Fact]
    public void Stun_OnPlayer_HasEffectReturnsTrue()
    {
        var (mgr, _, player, _) = MakeFixture();

        mgr.Apply(player, StatusEffect.Stun, 2);

        mgr.HasEffect(player, StatusEffect.Stun).Should().BeTrue();
    }

    // ── ChaosKnight stun immunity ─────────────────────────────────────────────

    [Fact]
    public void Stun_AgainstStunImmuneEnemy_DoesNotApply()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var stunImmuneEnemy = new StunImmuneEnemy_Stub();

        mgr.Apply(stunImmuneEnemy, StatusEffect.Stun, 2);

        mgr.HasEffect(stunImmuneEnemy, StatusEffect.Stun).Should().BeFalse();
        display.Messages.Should().Contain(m => m.Contains("shrugs off"));
    }

    // ── Sentinel 4pc player stun immunity ─────────────────────────────────────

    [Fact]
    public void Stun_AgainstStunImmunePlayer_DoesNotApply()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var player = new Player { Name = "Tank", IsStunImmune = true };

        mgr.Apply(player, StatusEffect.Stun, 2);

        mgr.HasEffect(player, StatusEffect.Stun).Should().BeFalse();
    }

    // ── Freeze broken by physical damage ──────────────────────────────────────

    [Fact]
    public void Freeze_BrokenByPhysicalDamage()
    {
        var (mgr, display, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Freeze, 3);
        mgr.HasEffect(enemy, StatusEffect.Freeze).Should().BeTrue();

        mgr.NotifyPhysicalDamage(enemy);

        mgr.HasEffect(enemy, StatusEffect.Freeze).Should().BeFalse();
        display.CombatMessages.Should().Contain(m => m.Contains("Freeze broken"));
    }

    // ── Weakened reduces attack modifier ──────────────────────────────────────

    [Fact]
    public void Weakened_ReducesAttackStatModifier()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Weakened, 2);

        var atkMod = mgr.GetStatModifier(enemy, "Attack");

        atkMod.Should().BeLessThan(0);
    }

    // ── BattleCry increases attack modifier ───────────────────────────────────

    [Fact]
    public void BattleCry_IncreasesAttackStatModifier()
    {
        var (mgr, _, player, _) = MakeFixture();
        mgr.Apply(player, StatusEffect.BattleCry, 2);

        var atkMod = mgr.GetStatModifier(player, "Attack");

        atkMod.Should().BeGreaterThan(0);
    }

    // ── Fortified increases defense modifier ──────────────────────────────────

    [Fact]
    public void Fortified_IncreasesDefenseStatModifier()
    {
        var (mgr, _, player, _) = MakeFixture();
        mgr.Apply(player, StatusEffect.Fortified, 2);

        var defMod = mgr.GetStatModifier(player, "Defense");

        defMod.Should().BeGreaterThan(0);
    }

    // ── Curse reduces attack and defense ──────────────────────────────────────

    [Fact]
    public void Curse_ReducesAttack()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var enemy = new Enemy_Stub(80, 20, 20, 15);
        mgr.Apply(enemy, StatusEffect.Curse, 4);

        mgr.GetStatModifier(enemy, "Attack").Should().BeLessThan(0);
    }

    [Fact]
    public void Curse_ReducesDefense()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var enemy = new Enemy_Stub(80, 20, 20, 15);
        mgr.Apply(enemy, StatusEffect.Curse, 4);

        mgr.GetStatModifier(enemy, "Defense").Should().BeLessThan(0);
    }
}

internal class StunImmuneEnemy_Stub : Enemy
{
    public StunImmuneEnemy_Stub()
    {
        Name = "ChaosKnight";
        HP = MaxHP = 80;
        Attack = 15;
        Defense = 5;
        XPValue = 20;
        IsStunImmune = true;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
    }
}
