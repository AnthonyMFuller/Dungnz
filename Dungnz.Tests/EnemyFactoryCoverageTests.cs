using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Additional EnemyFactory tests to cover CreateBoss floor branches and CreateScaled.</summary>
[Collection("EnemyFactory")]
public class EnemyFactoryCoverageTests
{
    // ── CreateBoss for all floors ─────────────────────────────────────────────

    [Theory]
    [InlineData(1, typeof(GoblinWarchief))]
    [InlineData(2, typeof(PlagueHoundAlpha))]
    [InlineData(3, typeof(IronSentinel))]
    [InlineData(4, typeof(BoneArchon))]
    [InlineData(5, typeof(CrimsonVampire))]
    [InlineData(6, typeof(ArchlichSovereign))]
    [InlineData(7, typeof(AbyssalLeviathan))]
    [InlineData(8, typeof(InfernalDragon))]
    public void CreateBoss_EachFloor_ReturnsCorrectBossType(int floor, Type expectedType)
    {
        var rng = new Random(1);
        var boss = EnemyFactory.CreateBoss(rng, floor);
        boss.Should().BeOfType(expectedType);
    }

    [Fact]
    public void CreateBoss_Floor0_FallsBackToGoblinWarchief()
    {
        var rng = new Random(1);
        var boss = EnemyFactory.CreateBoss(rng, 0);
        boss.Should().BeOfType<GoblinWarchief>("floor 0 uses the fallback");
    }

    [Fact]
    public void CreateBoss_Floor9_FallsBackToGoblinWarchief()
    {
        var rng = new Random(1);
        var boss = EnemyFactory.CreateBoss(rng, 9);
        boss.Should().BeOfType<GoblinWarchief>("unknown floor uses the fallback");
    }

    // ── CreateRandom elite variant ─────────────────────────────────────────

    [Fact]
    public void CreateRandom_EliteVariant_HasEliteFlag()
    {
        // Test many seeds to find an elite spawn (5% chance)
        bool foundElite = false;
        for (int seed = 0; seed < 500; seed++)
        {
            var rng = new Random(seed);
            var enemy = EnemyFactory.CreateRandom(rng);
            if (enemy.IsElite)
            {
                foundElite = true;
                enemy.Name.Should().StartWith("Elite");
                break;
            }
        }
        foundElite.Should().BeTrue("at least one elite should spawn in 500 attempts");
    }

    // ── CreateScaled for various enemy types ──────────────────────────────────

    [Theory]
    [InlineData("goblin")]
    [InlineData("skeleton")]
    [InlineData("troll")]
    [InlineData("darkknight")]
    [InlineData("goblinshaman")]
    [InlineData("stonegolem")]
    [InlineData("wraith")]
    [InlineData("vampirelord")]
    [InlineData("mimic")]
    [InlineData("giantrat")]
    [InlineData("cursedzombie")]
    [InlineData("bloodhound")]
    [InlineData("ironguard")]
    [InlineData("nightstalker")]
    [InlineData("frostwyvern")]
    [InlineData("chaosknight")]
    [InlineData("shadowimp")]
    [InlineData("carrioncrawler")]
    [InlineData("darksorcerer")]
    [InlineData("bonearcher")]
    [InlineData("cryptpriest")]
    [InlineData("plaguebear")]
    [InlineData("siegeogre")]
    [InlineData("bladedancer")]
    [InlineData("manaleech")]
    [InlineData("shieldbreaker")]
    public void CreateScaled_ValidEnemyType_ReturnsScaledEnemy(string enemyType)
    {
        var enemy = EnemyFactory.CreateScaled(enemyType, playerLevel: 3, floorMultiplier: 1.5f);
        enemy.Should().NotBeNull($"CreateScaled({enemyType}) should succeed");
        enemy.HP.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateScaled_UnknownType_ThrowsArgumentException()
    {
        var act = () => EnemyFactory.CreateScaled("totally_unknown_enemy", 1);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown enemy type*");
    }

    [Fact]
    public void CreateScaled_HigherLevel_HasIncreasedStats()
    {
        var level1 = EnemyFactory.CreateScaled("goblin", playerLevel: 1);
        var level5 = EnemyFactory.CreateScaled("goblin", playerLevel: 5);

        level5.HP.Should().BeGreaterThan(level1.HP, "higher level should scale HP up");
        level5.Attack.Should().BeGreaterThanOrEqualTo(level1.Attack, "higher level should scale attack up");
    }
}
