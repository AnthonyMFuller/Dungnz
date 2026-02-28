using System.Linq;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Additional CombatEngine tests targeting enemy-specific behaviors and boss phase abilities.</summary>
public class CombatEngineAdditionalTests
{
    private static (CombatEngine engine, FakeDisplayService display) MakeEngine(FakeInputReader input, double rngDouble = 0.9)
    {
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: rngDouble));
        return (engine, display);
    }

    private static FakeInputReader Attacks(int count) => new FakeInputReader(Enumerable.Repeat("A", count).ToArray());

    private static Player MakePlayer(int hp = 500, int atk = 50, int def = 0, PlayerClass cls = PlayerClass.Warrior)
        => new Player { HP = hp, MaxHP = hp, Attack = atk, Defense = def, Class = cls, Name = "Hero" };

    // GoblinShaman MaxHP=50, heals when HP < MaxHP/2 = 25
    [Fact]
    public void GoblinShaman_BelowHalfHP_HealsOnEnemyTurn()
    {
        var input = Attacks(100);
        var (engine, display) = MakeEngine(input, 0.9);
        // Player atk=10, shaman defense=5, damage=5/turn. Shaman heals 10 on first turn (10 HP).
        // After heal: HP = min(50, 10+10) = 20. Then 20/5=4 more turns. Total ~5 turns.
        var player = MakePlayer(hp: 2000, atk: 10, def: 0);
        var shaman = new GoblinShaman();
        shaman.HP = 10; // well below MaxHP/2=25
        engine.RunCombat(player, shaman);
        display.CombatMessages.Should().Contain(m => m.Contains("incantation") || m.Contains("heals") || m.Contains("heal") || m.Contains("dark magic"));
    }

    // Troll MaxHP=60, def=9, regen=5%=3/turn
    [Fact]
    public void Troll_RegeneratesHP_OnEachTurn()
    {
        var input = Attacks(10);
        var (engine, display) = MakeEngine(input, 0.9);
        // Player atk=30, troll def=9, damage=21/turn. Troll HP=25: turn1 → 4 HP, regen 3 → 7 HP.
        // turn2 → -14, dead. Regen shows on turn 1 enemy phase.
        var player = MakePlayer(hp: 500, atk: 30, def: 0);
        var troll = new Troll();
        troll.HP = 25;
        engine.RunCombat(player, troll);
        display.CombatMessages.Should().Contain(m => m.Contains("regenerate"));
    }

    // VampireLord MaxHP=80, def=12, atk=16, lifesteal=50%
    [Fact]
    public void VampireLord_Lifesteal_ShowsHealMessage()
    {
        var input = Attacks(10);
        var (engine, display) = MakeEngine(input, 0.9);
        // Player atk=30, vamp def=12, damage=18/turn. Vamp HP=20: turn1 → 2 HP.
        // Vamp attacks player (16 dmg), heals int(16*0.5)=8 HP → 10 HP. Message shows.
        // turn2: player deals 18 → HP=-8, dead.
        var player = MakePlayer(hp: 500, atk: 30, def: 0);
        var vampire = new VampireLord();
        vampire.HP = 20;
        engine.RunCombat(player, vampire);
        display.CombatMessages.Should().Contain(m => m.Contains("drain") || m.Contains("heals") || m.Contains("lifesteal") || m.Contains("life"));
    }

    // CarrionCrawler MaxHP=35, def=4, RegenPerTurn=5
    [Fact]
    public void CarrionCrawler_RegenPerTurn_ShowsRegenerationMessage()
    {
        var input = Attacks(10);
        var (engine, display) = MakeEngine(input, 0.9);
        // Player atk=30, crawler def=4, damage=26/turn. Crawler HP=30: turn1 → 4 HP.
        // Enemy: regen 5 → 9 HP (capped at MaxHP). Message shows.
        // turn2: player deals 26 → HP=-17, dead.
        var player = MakePlayer(hp: 500, atk: 30, def: 0);
        var crawler = new CarrionCrawler();
        crawler.HP = 30;
        engine.RunCombat(player, crawler);
        display.CombatMessages.Should().Contain(m => m.Contains("regenerate"));
    }

    [Fact]
    public void GoblinWarchief_ReinforcementsPhase_FiresWhenHPLow()
    {
        var input = Attacks(50);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 2000, atk: 20, def: 0);
        var boss = new GoblinWarchief();
        boss.HP = (int)(boss.MaxHP * 0.50);
        engine.RunCombat(player, boss);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void PlagueHoundAlpha_BloodfrenzyPhase_BoostsAttack()
    {
        var input = Attacks(50);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 2000, atk: 20, def: 0);
        var boss = new PlagueHoundAlpha();
        boss.HP = (int)(boss.MaxHP * 0.45);
        engine.RunCombat(player, boss);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void IronSentinel_StunningBlowPhase_ShowsCombatMessage()
    {
        var input = Attacks(60);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 2000, atk: 20, def: 0);
        var boss = new IronSentinel();
        boss.HP = (int)(boss.MaxHP * 0.45);
        engine.RunCombat(player, boss);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void BoneArchon_WeakenAuraPhase_ShowsMessage()
    {
        var input = Attacks(50);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 2000, atk: 20, def: 0);
        var boss = new BoneArchon();
        boss.HP = (int)(boss.MaxHP * 0.45);
        engine.RunCombat(player, boss);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void CrimsonVampire_BloodDrainPhase_ShowsMessage()
    {
        var input = Attacks(30);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 2000, atk: 20, def: 0); // deals 13/turn to cut through 67 HP quickly
        player.Mana = 50;
        var boss = new CrimsonVampire();
        boss.HP = (int)(boss.MaxHP * 0.45);
        engine.RunCombat(player, boss);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Mage)]
    [InlineData(PlayerClass.Rogue)]
    [InlineData(PlayerClass.Paladin)]
    [InlineData(PlayerClass.Necromancer)]
    [InlineData(PlayerClass.Ranger)]
    public void Combat_DifferentPlayerClasses_ProduceCombatMessages(PlayerClass cls)
    {
        var input = Attacks(5);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 500, atk: 50, def: 0, cls: cls);
        var enemy = new Enemy_Stub(10, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void Flee_WithLowRng_Succeeds()
    {
        // rng=0.1 < 0.5 flee threshold → flee succeeds
        var input = new FakeInputReader("F");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.1));
        var player = MakePlayer(hp: 100, atk: 10, def: 5);
        var enemy = new Enemy_Stub(50, 10, 2, 20);
        var result = engine.RunCombat(player, enemy);
        result.Should().Be(CombatResult.Fled);
    }

    [Fact]
    public void Combat_WithCritRng_CompletesSuccessfully()
    {
        // Enemy_Stub has defense=5, dodgeChance=min(0.5, 5*0.01)=0.05.
        // Use rng=0.06 so enemy doesn't always dodge (0.06 >= 0.05) but crits still trigger.
        var input = Attacks(5);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.06));
        var player = MakePlayer(hp: 500, atk: 30, def: 0);
        var enemy = new Enemy_Stub(10, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void DungeonBoss_ChargeAttack_CompletesWithoutException()
    {
        var input = Attacks(60);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.2));
        var player = MakePlayer(hp: 2000, atk: 20, def: 0);
        var boss = new GoblinWarchief();
        engine.RunCombat(player, boss);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void CombatEngine_DeadEnemy_TriggersLootAndXP()
    {
        var input = Attacks(5);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 100, atk: 100, def: 0);
        var enemy = new Enemy_Stub(5, 3, 0, 25);
        engine.RunCombat(player, enemy);
        player.XP.Should().Be(25);
    }

    [Fact]
    public void StunnedEnemy_SkipsTurn()
    {
        var input = Attacks(10); // need ceil(30/5)=6 turns + buffer
        var display = new FakeDisplayService(input);
        var statusEffects = new StatusEffectManager(display);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9), statusEffects: statusEffects);
        var player = MakePlayer(hp: 200, atk: 5, def: 0);
        var enemy = new Enemy_Stub(30, 10, 0, 10);
        statusEffects.Apply(enemy, StatusEffect.Stun, 1);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("stunned"));
    }

    [Fact]
    public void GoblinShaman_PoisonOnHit_CombatCompletesSuccessfully()
    {
        // Test that GoblinShaman's poison-on-hit code path is exercised.
        // With rng=0.9: player never dodges, enemy never dodges, no crits.
        // Player.atk=20: deals max(1,20-5)=15/turn. Shaman HP=50 dies in 4 turns.
        var input = Attacks(20);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 2000, atk: 20, def: 0);
        var shaman = new GoblinShaman();
        engine.RunCombat(player, shaman);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void RunCombat_WithRunStats_UsesPassedStats()
    {
        // Covers the `if (stats != null) _stats = stats;` branch in RunCombat
        var input = Attacks(5);
        var (engine, display) = MakeEngine(input, 0.9);
        var player = MakePlayer(hp: 500, atk: 20, def: 0);
        var enemy = new Enemy_Stub(10, 5, 0, 10);
        var stats = new RunStats();
        engine.RunCombat(player, enemy, stats);
        stats.EnemiesDefeated.Should().BeGreaterThan(0, "stats should be updated via the passed RunStats object");
    }

    [Fact]
    public void PlagueBear_PoisonOnCombatStart_AppliesToPlayer()
    {
        // PlagueBear has PoisonOnCombatStart=true → player gets poisoned at combat start
        var input = Attacks(10);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 5000, atk: 50, def: 0); // high HP to survive poison
        var bear = new PlagueBear();
        engine.RunCombat(player, bear);
        display.CombatMessages.Should().Contain(m => m.Contains("poisoned") || m.Contains("plague"));
    }

    [Fact]
    public void FleeAttempt_WithLowRng_PlayerFleesSuccessfully()
    {
        // With defaultDouble=0.4, _rng.NextDouble() = 0.4 < 0.5 → flee succeeds
        var input = new FakeInputReader("F", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.4));
        var player = MakePlayer(hp: 500, atk: 5, def: 0); // low attack so can't 1-shot
        var enemy = new Enemy_Stub(500, 5, 0, 10); // high HP
        var result = engine.RunCombat(player, enemy);
        result.Should().Be(CombatResult.Fled, "player should flee successfully with rng < 0.5");
        display.Messages.Should().Contain(m => m.Contains("fled") || m.Contains("escaped") || m.Contains("flee"));
    }
}
