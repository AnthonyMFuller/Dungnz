using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class StatusEffectLifecycleIntegrationTests
{
    private static StatusEffectManager SM() => new StatusEffectManager(new FakeDisplayService());

    [Fact]
    public void Poison_Applied_EnemyLoses3HPOnFirstTick()
    {
        var sm = SM(); var e = new Enemy_Stub(hp: 50, atk: 5, def: 0, xp: 10); int hp = e.HP;
        sm.Apply(e, StatusEffect.Poison, 3); sm.ProcessTurnStart(e);
        e.HP.Should().Be(hp - 3);
    }

    [Fact]
    public void Poison_ThreeDurationTurns_TotalDamage9_ThenExpires()
    {
        var sm = SM(); var e = new Enemy_Stub(hp: 100, atk: 5, def: 0, xp: 10); int hp = e.HP;
        sm.Apply(e, StatusEffect.Poison, 3);
        sm.ProcessTurnStart(e); sm.ProcessTurnStart(e); sm.ProcessTurnStart(e);
        e.HP.Should().Be(hp - 9);
        sm.HasEffect(e, StatusEffect.Poison).Should().BeFalse();
    }

    [Fact]
    public void Poison_AfterExpiry_NoDamageOnSubsequentTick()
    {
        var sm = SM(); var e = new Enemy_Stub(hp: 100, atk: 5, def: 0, xp: 10);
        sm.Apply(e, StatusEffect.Poison, 2); sm.ProcessTurnStart(e); sm.ProcessTurnStart(e);
        int hp = e.HP; sm.ProcessTurnStart(e);
        e.HP.Should().Be(hp);
    }

    [Fact]
    public void Bleed_Applied_EnemyLoses5HPOnFirstTick()
    {
        var sm = SM(); var e = new Enemy_Stub(hp: 50, atk: 5, def: 0, xp: 10); int hp = e.HP;
        sm.Apply(e, StatusEffect.Bleed, 2); sm.ProcessTurnStart(e);
        e.HP.Should().Be(hp - 5);
    }

    [Fact]
    public void Burn_Applied_EnemyLoses8HPOnFirstTick()
    {
        var sm = SM(); var e = new Enemy_Stub(hp: 100, atk: 5, def: 0, xp: 10); int hp = e.HP;
        sm.Apply(e, StatusEffect.Burn, 2); sm.ProcessTurnStart(e);
        e.HP.Should().Be(hp - 8);
    }

    [Fact]
    public void Regen_Applied_PlayerGains4HPOnFirstTick()
    {
        var sm = SM(); var p = new Player { Name = "Hero", MaxHP = 100 }; p.SetHPDirect(60); int hp = p.HP;
        sm.Apply(p, StatusEffect.Regen, 3); sm.ProcessTurnStart(p);
        p.HP.Should().Be(hp + 4);
    }

    [Fact]
    public void Regen_AtMaxHP_DoesNotOverheal()
    {
        var sm = SM(); var p = new Player { Name = "Hero", MaxHP = 100 }; p.SetHPDirect(100);
        sm.Apply(p, StatusEffect.Regen, 2); sm.ProcessTurnStart(p);
        p.HP.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void StatusEffect_ReapplySameEffect_StillActiveAfterThreeMoreTicks()
    {
        // After 2-turn apply + 1 tick consumed + reapply with 5 turns,
        // there are 5 remaining ticks. After 3 more, 2 remain so still active.
        var sm = SM(); var e = new Enemy_Stub(hp: 100, atk: 5, def: 0, xp: 10);
        sm.Apply(e, StatusEffect.Poison, 2);
        sm.ProcessTurnStart(e);
        sm.Apply(e, StatusEffect.Poison, 5);
        sm.ProcessTurnStart(e); sm.ProcessTurnStart(e); sm.ProcessTurnStart(e);
        sm.HasEffect(e, StatusEffect.Poison).Should().BeTrue("2 turns remain after reapply and 3 ticks");
    }

    [Fact]
    public void Poison_AppliedToPlayer_ReducesPlayerHP()
    {
        var sm = SM(); var p = new Player { Name = "Hero", MaxHP = 100 }; p.SetHPDirect(80); int hp = p.HP;
        sm.Apply(p, StatusEffect.Poison, 2); sm.ProcessTurnStart(p);
        p.HP.Should().Be(hp - 3);
    }

    [Fact]
    public void HasEffect_BeforeAndAfterApply_ReturnsCorrectly()
    {
        var sm = SM(); var e = new Enemy_Stub(hp: 50, atk: 5, def: 0, xp: 10);
        sm.HasEffect(e, StatusEffect.Burn).Should().BeFalse();
        sm.Apply(e, StatusEffect.Burn, 3);
        sm.HasEffect(e, StatusEffect.Burn).Should().BeTrue();
    }
}
