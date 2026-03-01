using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;

namespace Dungnz.Tests;

public class EnemyAITests
{
    private static Player CreatePlayer(int hp = 100, int maxHp = 100) =>
        new() { Name = "Test", HP = hp, MaxHP = maxHp, Attack = 10, Defense = 5 };

    // ── GoblinShamanAI ─────────────────────────────────────────────

    [Fact]
    public void GoblinShamanAI_HealsWhenBelowHalfHP()
    {
        var ai = new GoblinShamanAI();
        var shaman = new GoblinShaman();
        shaman.HP = shaman.MaxHP / 4; // well below 50%
        var ctx = new CombatContext(1, 1.0, 1);

        ai.TakeTurn(shaman, CreatePlayer(), ctx);

        ai.LastAction.Should().Be(EnemyAction.Heal);
        ai.LastHealAmount.Should().BeGreaterThan(0);
        shaman.HP.Should().BeGreaterThan(shaman.MaxHP / 4);
    }

    [Fact]
    public void GoblinShamanAI_AttacksWhenAboveHalfHP()
    {
        var ai = new GoblinShamanAI();
        var shaman = new GoblinShaman();
        // HP is full (above 50%)
        var ctx = new CombatContext(1, 1.0, 1);

        ai.TakeTurn(shaman, CreatePlayer(), ctx);

        ai.LastAction.Should().Be(EnemyAction.Attack);
    }

    [Fact]
    public void GoblinShamanAI_HealCooldown_PreventsConsecutiveHeals()
    {
        var ai = new GoblinShamanAI();
        var shaman = new GoblinShaman();
        shaman.HP = shaman.MaxHP / 4;
        var ctx = new CombatContext(1, 1.0, 1);

        ai.TakeTurn(shaman, CreatePlayer(), ctx);
        ai.LastAction.Should().Be(EnemyAction.Heal);
        ai.HealCooldown.Should().Be(3);

        // Second turn: still low HP but on cooldown
        shaman.HP = shaman.MaxHP / 4;
        ai.TakeTurn(shaman, CreatePlayer(), ctx);
        ai.LastAction.Should().Be(EnemyAction.Attack);
    }

    // ── CryptPriestAI ──────────────────────────────────────────────

    [Fact]
    public void CryptPriestAI_SelfHealsOnSchedule()
    {
        var ai = new CryptPriestAI();
        var priest = new CryptPriest();
        priest.HP = priest.MaxHP / 2;
        var ctx = new CombatContext(1, 1.0, 1);

        // First turn: cooldown starts at 1, decrements to 0
        ai.TakeTurn(priest, CreatePlayer(), ctx);
        bool firstHeal = ai.DidSelfHeal;

        // Second turn: cooldown should be 0, so heal fires
        ai.TakeTurn(priest, CreatePlayer(), ctx);
        bool secondHeal = ai.DidSelfHeal;

        // At least one of the first two turns should heal
        (firstHeal || secondHeal).Should().BeTrue();
    }

    [Fact]
    public void CryptPriestAI_RespectsHealCooldown()
    {
        var ai = new CryptPriestAI();
        var priest = new CryptPriest();
        priest.HP = priest.MaxHP / 2;
        var ctx = new CombatContext(1, 1.0, 1);

        int heals = 0;
        for (int i = 0; i < 6; i++)
        {
            ai.TakeTurn(priest, CreatePlayer(), ctx);
            if (ai.DidSelfHeal) heals++;
        }

        // Over 6 turns with SelfHealEveryTurns=2, should heal roughly 3 times
        heals.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(4);
    }

    [Fact]
    public void CryptPriestAI_DoesNotExceedMaxHP()
    {
        var ai = new CryptPriestAI();
        var priest = new CryptPriest();
        // HP is already at max
        var ctx = new CombatContext(1, 1.0, 1);

        for (int i = 0; i < 5; i++)
            ai.TakeTurn(priest, CreatePlayer(), ctx);

        priest.HP.Should().BeLessThanOrEqualTo(priest.MaxHP);
    }

    // ── CombatContext ───────────────────────────────────────────────

    [Fact]
    public void CombatContext_StoresValues()
    {
        var ctx = new CombatContext(3, 0.75, 2);
        ctx.RoundNumber.Should().Be(3);
        ctx.PlayerHPPercent.Should().BeApproximately(0.75, 0.001);
        ctx.CurrentFloor.Should().Be(2);
    }
}
