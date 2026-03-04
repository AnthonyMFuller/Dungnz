using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for status effect edge cases (#948):
/// applying effect when immune, extending duration, stacking, zero-duration effects.
/// </summary>
public class StatusEffectEdgeCaseTests
{
    private static (StatusEffectManager mgr, TestDisplayService display) MakeManager()
    {
        var display = new TestDisplayService();
        return (new StatusEffectManager(display), display);
    }

    // ── Applying effect when immune ───────────────────────────────────────────

    [Fact]
    public void Apply_ToImmuneEnemy_DoesNotApplyAnyEffect()
    {
        var (mgr, display) = MakeManager();
        var immune = new ImmuneEnemy_Stub();

        mgr.Apply(immune, StatusEffect.Stun, 3);

        mgr.HasEffect(immune, StatusEffect.Stun).Should().BeFalse();
        display.Messages.Should().Contain(m => m.Contains("immune"));
    }

    [Fact]
    public void Apply_MultipleEffects_ToImmuneEnemy_NoneApply()
    {
        var (mgr, _) = MakeManager();
        var immune = new ImmuneEnemy_Stub();

        mgr.Apply(immune, StatusEffect.Poison, 3);
        mgr.Apply(immune, StatusEffect.Bleed, 3);
        mgr.Apply(immune, StatusEffect.Burn, 3);

        mgr.GetActiveEffects(immune).Should().BeEmpty();
    }

    // ── Duration extension ────────────────────────────────────────────────────

    [Fact]
    public void Apply_SameEffect_ExtendsToHigherDuration()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);

        mgr.Apply(enemy, StatusEffect.Poison, 2);
        mgr.Apply(enemy, StatusEffect.Poison, 5);

        var effects = mgr.GetActiveEffects(enemy);
        effects.Should().ContainSingle(e => e.Effect == StatusEffect.Poison);
        effects.First(e => e.Effect == StatusEffect.Poison).RemainingTurns.Should().Be(5);
    }

    [Fact]
    public void Apply_SameEffect_WithLowerDuration_KeepsHigher()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);

        mgr.Apply(enemy, StatusEffect.Poison, 5);
        mgr.Apply(enemy, StatusEffect.Poison, 2);

        var effects = mgr.GetActiveEffects(enemy);
        effects.First(e => e.Effect == StatusEffect.Poison).RemainingTurns.Should().Be(5);
    }

    [Fact]
    public void Apply_SameEffect_WithEqualDuration_DoesNotDuplicate()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);

        mgr.Apply(enemy, StatusEffect.Bleed, 3);
        mgr.Apply(enemy, StatusEffect.Bleed, 3);

        mgr.GetActiveEffects(enemy).Count(e => e.Effect == StatusEffect.Bleed).Should().Be(1);
    }

    // ── Stacking different effects ────────────────────────────────────────────

    [Fact]
    public void Apply_DifferentEffects_AllActive()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);

        mgr.Apply(enemy, StatusEffect.Poison, 3);
        mgr.Apply(enemy, StatusEffect.Bleed, 3);
        mgr.Apply(enemy, StatusEffect.Burn, 2);

        mgr.HasEffect(enemy, StatusEffect.Poison).Should().BeTrue();
        mgr.HasEffect(enemy, StatusEffect.Bleed).Should().BeTrue();
        mgr.HasEffect(enemy, StatusEffect.Burn).Should().BeTrue();
        mgr.GetActiveEffects(enemy).Should().HaveCount(3);
    }

    // ── Zero-duration and 1-turn effects ──────────────────────────────────────

    [Fact]
    public void Apply_OneTurnDuration_ExpiresAfterOneTick()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);

        mgr.Apply(enemy, StatusEffect.Stun, 1);
        mgr.HasEffect(enemy, StatusEffect.Stun).Should().BeTrue();

        mgr.ProcessTurnStart(enemy);

        mgr.HasEffect(enemy, StatusEffect.Stun).Should().BeFalse();
    }

    [Fact]
    public void Apply_ZeroDuration_ExpiresImmediatelyOnTick()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);

        mgr.Apply(enemy, StatusEffect.Silence, 0);

        // ProcessTurnStart decrements and removes effects with remaining <= 0
        mgr.ProcessTurnStart(enemy);

        mgr.HasEffect(enemy, StatusEffect.Silence).Should().BeFalse();
    }

    // ── Clear removes all effects ─────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllEffects()
    {
        var (mgr, _) = MakeManager();
        var enemy = new Enemy_Stub(50, 10, 2, 15);
        mgr.Apply(enemy, StatusEffect.Poison, 3);
        mgr.Apply(enemy, StatusEffect.Stun, 2);

        mgr.Clear(enemy);

        mgr.GetActiveEffects(enemy).Should().BeEmpty();
    }

    // ── RemoveDebuffs only removes debuffs ────────────────────────────────────

    [Fact]
    public void RemoveDebuffs_KeepsBuffs()
    {
        var (mgr, _) = MakeManager();
        var player = new Player { Name = "Hero" };
        mgr.Apply(player, StatusEffect.Poison, 3);
        mgr.Apply(player, StatusEffect.Regen, 3);
        mgr.Apply(player, StatusEffect.BattleCry, 2);

        mgr.RemoveDebuffs(player);

        mgr.HasEffect(player, StatusEffect.Poison).Should().BeFalse();
        mgr.HasEffect(player, StatusEffect.Regen).Should().BeTrue();
        mgr.HasEffect(player, StatusEffect.BattleCry).Should().BeTrue();
    }

    // ── GetActiveEffects for unknown target returns empty ─────────────────────

    [Fact]
    public void GetActiveEffects_UnknownTarget_ReturnsEmpty()
    {
        var (mgr, _) = MakeManager();
        var stranger = new Player { Name = "Nobody" };

        mgr.GetActiveEffects(stranger).Should().BeEmpty();
    }

    // ── HasEffect for unknown target returns false ────────────────────────────

    [Fact]
    public void HasEffect_UnknownTarget_ReturnsFalse()
    {
        var (mgr, _) = MakeManager();
        var stranger = new Player { Name = "Nobody" };

        mgr.HasEffect(stranger, StatusEffect.Poison).Should().BeFalse();
    }

    // ── GetStatModifier for unknown target returns 0 ──────────────────────────

    [Fact]
    public void GetStatModifier_UnknownTarget_ReturnsZero()
    {
        var (mgr, _) = MakeManager();
        var stranger = new Player { Name = "Nobody" };

        mgr.GetStatModifier(stranger, "Attack").Should().Be(0);
    }
}
