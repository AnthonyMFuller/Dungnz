using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class StatusEffectManagerTests
{
    private static (StatusEffectManager mgr, TestDisplayService display, Player player, Enemy_Stub enemy) MakeFixture()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var player = new Player { Name = "Hero" };
        var enemy = new Enemy_Stub(50, 10, 2, 15);
        return (mgr, display, player, enemy);
    }

    [Fact]
    public void Poison_ReducesEnemyHP_EachTurn()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Poison, 3);
        var hpBefore = enemy.HP;

        mgr.ProcessTurnStart(enemy);

        enemy.HP.Should().Be(hpBefore - 3);
    }

    [Fact]
    public void Poison_NotApplied_WhenEnemyIsImmuneToEffects()
    {
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var immune = new ImmuneEnemy_Stub();

        mgr.Apply(immune, StatusEffect.Poison, 3);

        mgr.HasEffect(immune, StatusEffect.Poison).Should().BeFalse();
    }

    [Fact]
    public void Stun_HasEffect_ReturnsTrue()
    {
        var (mgr, _, _, enemy) = MakeFixture();

        mgr.Apply(enemy, StatusEffect.Stun, 2);

        mgr.HasEffect(enemy, StatusEffect.Stun).Should().BeTrue();
    }

    [Fact]
    public void Stun_EnemyCannotAct_WhenStunned()
    {
        var (mgr, display, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Stun, 1);

        mgr.ProcessTurnStart(enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("stunned"));
    }

    [Fact]
    public void Regen_IncreasesPlayerHP_EachTurn()
    {
        var (mgr, _, player, _) = MakeFixture();
        player.TakeDamage(20);
        var hpBefore = player.HP;

        mgr.Apply(player, StatusEffect.Regen, 3);
        mgr.ProcessTurnStart(player);

        player.HP.Should().Be(hpBefore + 4);
    }

    [Fact]
    public void Bleed_ReducesEnemyHP_EachTurn()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Bleed, 3);
        var hpBefore = enemy.HP;

        mgr.ProcessTurnStart(enemy);

        enemy.HP.Should().Be(hpBefore - 5);
    }

    [Fact]
    public void Weakened_DurationTrackedCorrectly()
    {
        var (mgr, _, player, _) = MakeFixture();

        mgr.Apply(player, StatusEffect.Weakened, 3);

        var effects = mgr.GetActiveEffects(player);
        effects.Should().ContainSingle(e => e.Effect == StatusEffect.Weakened);
        effects.First(e => e.Effect == StatusEffect.Weakened).RemainingTurns.Should().Be(3);
    }

    [Fact]
    public void Duration_ExpiresAfterNTurns()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Poison, 2);

        mgr.ProcessTurnStart(enemy);
        mgr.ProcessTurnStart(enemy);

        mgr.HasEffect(enemy, StatusEffect.Poison).Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllEffects()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Poison, 3);
        mgr.Apply(enemy, StatusEffect.Stun, 2);

        mgr.Clear(enemy);

        mgr.HasEffect(enemy, StatusEffect.Poison).Should().BeFalse();
        mgr.HasEffect(enemy, StatusEffect.Stun).Should().BeFalse();
    }

    [Fact]
    public void HasEffect_ReturnsFalse_AfterExpiry()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Bleed, 1);

        mgr.ProcessTurnStart(enemy);

        mgr.HasEffect(enemy, StatusEffect.Bleed).Should().BeFalse();
    }

    [Fact]
    public void RemoveDebuffs_RemovesNegativeEffects_KeepsRegen()
    {
        var (mgr, _, player, _) = MakeFixture();
        player.TakeDamage(20);
        mgr.Apply(player, StatusEffect.Poison, 3);
        mgr.Apply(player, StatusEffect.Regen, 3);

        mgr.RemoveDebuffs(player);

        mgr.HasEffect(player, StatusEffect.Poison).Should().BeFalse();
        mgr.HasEffect(player, StatusEffect.Regen).Should().BeTrue();
    }

    [Fact]
    public void ApplySameEffect_Twice_RefreshesDuration_NoDoubleStack()
    {
        var (mgr, _, _, enemy) = MakeFixture();
        mgr.Apply(enemy, StatusEffect.Poison, 2);
        mgr.Apply(enemy, StatusEffect.Poison, 5);

        var effects = mgr.GetActiveEffects(enemy);
        effects.Where(e => e.Effect == StatusEffect.Poison).Should().HaveCount(1);
        effects.First(e => e.Effect == StatusEffect.Poison).RemainingTurns.Should().Be(5);

        // Only one instance: 3 damage per turn, not 6
        var hpBefore = enemy.HP;
        mgr.ProcessTurnStart(enemy);
        enemy.HP.Should().Be(hpBefore - 3);
    }
}

internal class ImmuneEnemy_Stub : Enemy
{
    public ImmuneEnemy_Stub()
    {
        Name = "Immune";
        HP = MaxHP = 100;
        Attack = 5;
        Defense = 5;
        XPValue = 10;
        IsImmuneToEffects = true;
        LootTable = new Dungnz.Models.LootTable(minGold: 0, maxGold: 0);
    }
}
