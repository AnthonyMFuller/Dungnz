using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;

namespace Dungnz.Tests;

public class EnemyAITests
{
    // ── CombatContext ───────────────────────────────────────────────

    [Fact]
    public void CombatContext_StoresValues()
    {
        var ctx = new CombatContext(3, 0.75, 2);
        ctx.RoundNumber.Should().Be(3);
        ctx.PlayerHPPercent.Should().BeApproximately(0.75, 0.001);
        ctx.CurrentFloor.Should().Be(2);
    }

    // ── GoblinAI Tests ──────────────────────────────────────────────

    [Fact]
    public void GoblinAI_LowHP_ChoosesFlee()
    {
        var goblin = new Goblin { HP = 5, MaxHP = 20 }; // 25% HP
        var player = new Player { Name = "Test", HP = 50, MaxHP = 100 };
        var context = new CombatContext(RoundNumber: 1, PlayerHPPercent: 0.5, CurrentFloor: 1);
        var ai = new GoblinAI();

        var action = ai.ChooseAction(goblin, player, context);

        action.Type.Should().Be(EnemyActionType.Flee);
    }

    [Fact]
    public void GoblinAI_PlayerWeakened_ChoosesAggressiveAttack()
    {
        var goblin = new Goblin { HP = 15, MaxHP = 20 }; // 75% HP
        var player = new Player { Name = "Test", HP = 40, MaxHP = 100 }; // 40% HP
        var context = new CombatContext(RoundNumber: 1, PlayerHPPercent: 0.4, CurrentFloor: 1);
        var ai = new GoblinAI();

        var action = ai.ChooseAction(goblin, player, context);

        action.Type.Should().Be(EnemyActionType.AggressiveAttack);
        action.Modifier.Should().BeApproximately(1.5, 0.001);
    }

    [Fact]
    public void GoblinAI_HealthyEnemy_ChoosesStandardAttack()
    {
        var goblin = new Goblin { HP = 18, MaxHP = 20 }; // 90% HP
        var player = new Player { Name = "Test", HP = 80, MaxHP = 100 }; // 80% HP
        var context = new CombatContext(RoundNumber: 1, PlayerHPPercent: 0.8, CurrentFloor: 1);
        var ai = new GoblinAI();

        var action = ai.ChooseAction(goblin, player, context);

        action.Type.Should().Be(EnemyActionType.Attack);
    }

    // ── SkeletonAI Tests ────────────────────────────────────────────

    [Fact]
    public void SkeletonAI_EveryThirdRound_ChoosesBoneRattle()
    {
        var skeleton = new Skeleton { HP = 25, MaxHP = 30 };
        var player = new Player { Name = "Test", HP = 50, MaxHP = 100 };
        var context = new CombatContext(RoundNumber: 3, PlayerHPPercent: 0.5, CurrentFloor: 1);
        var ai = new SkeletonAI();

        var action = ai.ChooseAction(skeleton, player, context);

        action.Type.Should().Be(EnemyActionType.BoneRattle);
        action.Modifier.Should().BeApproximately(0.10, 0.001);
    }

    [Fact]
    public void SkeletonAI_NonThirdRound_ChoosesArmorPiercingAttack()
    {
        var skeleton = new Skeleton { HP = 25, MaxHP = 30 };
        var player = new Player { Name = "Test", HP = 50, MaxHP = 100 };
        var context = new CombatContext(RoundNumber: 2, PlayerHPPercent: 0.5, CurrentFloor: 1);
        var ai = new SkeletonAI();

        var action = ai.ChooseAction(skeleton, player, context);

        action.Type.Should().Be(EnemyActionType.ArmorPiercingAttack);
    }

    [Fact]
    public void SkeletonAI_LowHP_NeverFlees()
    {
        var skeleton = new Skeleton { HP = 3, MaxHP = 30 }; // 10% HP, well below goblin flee threshold
        var player = new Player { Name = "Test", HP = 50, MaxHP = 100 };
        var context = new CombatContext(RoundNumber: 1, PlayerHPPercent: 0.5, CurrentFloor: 1);
        var ai = new SkeletonAI();

        var action = ai.ChooseAction(skeleton, player, context);

        action.Type.Should().Be(EnemyActionType.ArmorPiercingAttack);
    }

    // ── EnemyAIRegistry Tests ───────────────────────────────────────

    [Fact]
    public void EnemyAIRegistry_GetGoblinAI_ReturnsGoblinAI()
    {
        var goblin = new Goblin();

        var ai = EnemyAIRegistry.GetAI(goblin);

        ai.Should().NotBeNull();
        ai.Should().BeOfType<GoblinAI>();
    }

    [Fact]
    public void EnemyAIRegistry_GetSkeletonAI_ReturnsSkeletonAI()
    {
        var skeleton = new Skeleton();

        var ai = EnemyAIRegistry.GetAI(skeleton);

        ai.Should().NotBeNull();
        ai.Should().BeOfType<SkeletonAI>();
    }
}
