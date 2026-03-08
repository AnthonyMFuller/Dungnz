using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
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

    // ── CombatEngine Integration Tests ─────────────────────────────

    [Fact]
    public void CombatEngine_GoblinAI_FleeTriggeredAtLowHP()
    {
        // Arrange: low-HP goblin vs player
        var display = new TestDisplayService();
        var input = new FakeInputReader("1"); // attack once
        var rng = new Random(42); // seed for deterministic flee roll
        var goblin = new Goblin { HP = 5, MaxHP = 20, Attack = 10, Defense = 5 }; // 25% HP
        var player = new Player { Name = "Test", HP = 100, MaxHP = 100, Attack = 20, Defense = 10 };

        var engine = new CombatEngine(display, input, rng);
        engine.DungeonFloor = 1;

        // Act: trigger combat - goblin should attempt to flee on its turn
        var result = engine.RunCombat(player, goblin);

        // Assert: check for flee attempt message
        display.CombatMessages.Should().Contain(m => m.Contains("flee") || m.Contains("escape"));
    }

    [Fact]
    public void CombatEngine_SkeletonAI_BoneRattleEveryThirdRound()
    {
        // Arrange: skeleton vs player with RNG controlled
        var display = new TestDisplayService();
        var input = new FakeInputReader("1", "1", "1", "1", "1", "1", "1", "1", "1", "1"); // many attack commands
        var rng = new Random(123); // seed for deterministic results
        var skeleton = new Skeleton { HP = 100, MaxHP = 100, Attack = 12, Defense = 8 };
        var player = new Player
        {
            Name = "Test",
            HP = 200,
            MaxHP = 200,
            Attack = 5, // low attack to keep skeleton alive for 3+ turns
            Defense = 20 // high defense to survive
        };

        var engine = new CombatEngine(display, input, rng);
        engine.DungeonFloor = 1;

        // Act: simulate combat (skeleton should use BoneRattle on round 3)
        var result = engine.RunCombat(player, skeleton);

        // Assert: check for BoneRattle message on round 3
        display.CombatMessages.Should().Contain(m => m.Contains("rattles its bones") || m.Contains("Weakened"));
    }
}
