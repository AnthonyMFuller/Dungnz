using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Dungnz.Tests;

/// <summary>
/// Combat balance simulation — runs 100 battles per Phase 2 enemy type and validates:
/// (1) no Phase 2 enemy is unbeatable at the appropriate player level,
/// (2) the Lich King is appropriately challenging at high level (wins possible but not trivial),
/// (3) no combat results in an infinite loop or crash.
///
/// Player stats are derived from the base (HP=100, ATK=10, DEF=5) plus LevelUp() increments
/// (+2 ATK, +1 DEF, +10 MaxHP per level), matching the real game progression.
///
/// Enemy tiers and matched player levels:
///   Giant Rat     (T1)  → Level  2   | HP=15,  ATK=7,  DEF=1
///   Cursed Zombie (T2)  → Level  3   | HP=32,  ATK=9,  DEF=6
///   Blood Hound   (T3)  → Level  4   | HP=42,  ATK=16, DEF=5
///   Iron Guard    (T4)  → Level  5   | HP=50,  ATK=14, DEF=14
///   Night Stalker (T5)  → Level  6   | HP=55,  ATK=20, DEF=8
///   Frost Wyvern  (T6)  → Level  8   | HP=75,  ATK=22, DEF=12
///   Chaos Knight  (T7)  → Level  10  | HP=85,  ATK=24, DEF=16
///   Lich King     (T9)  → Level  12  | HP=170, ATK=38, DEF=20
/// </summary>
public class CombatBalanceSimulationTests
{
    private readonly ITestOutputHelper _output;

    public CombatBalanceSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Phase 2 enemy roster: (Name, HP, ATK, DEF, XP, matchedPlayerLevel)
    // Stats sourced from Data/enemy-stats.json Phase 2 additions.
    private static readonly (string Name, int HP, int ATK, int DEF, int XP, int PlayerLevel)[] Phase2Enemies =
    {
        ("Giant Rat",      15,  7,   1,  12,  2),
        ("Cursed Zombie",  32,  9,   6,  28,  3),
        ("Blood Hound",    42, 16,   5,  38,  4),
        ("Iron Guard",     50, 14,  14,  48,  5),
        ("Night Stalker",  55, 20,   8,  58,  6),
        ("Frost Wyvern",   75, 22,  12,  70,  8),
        ("Chaos Knight",   85, 24,  16,  80, 10),
        ("Lich King",     170, 38,  20, 130, 12),
    };

    private const int SimulationsPerEnemy = 100;

    // Hard timeout to detect infinite loops; each combat completes in << 1 s under normal operation.
    private static readonly TimeSpan CombatTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Builds a player at the given level by calling LevelUp() repeatedly from base stats.</summary>
    private static Player BuildPlayerAtLevel(int level)
    {
        var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Level = 1 };
        for (int i = 1; i < level; i++)
            player.LevelUp();
        return player;
    }

    /// <summary>Creates a Phase 2 enemy directly from JSON-sourced stats.</summary>
    private static Enemy BuildEnemy(string name, int hp, int atk, int def, int xp) =>
        new SimEnemy(name, hp, atk, def, xp);

    // ── Test 1: All Phase 2 enemies must be beatable at their matched player level ──

    [Theory]
    [InlineData("Giant Rat",      15,  7,   1,  12,  2)]
    [InlineData("Cursed Zombie",  32,  9,   6,  28,  3)]
    [InlineData("Blood Hound",    42, 16,   5,  38,  4)]
    [InlineData("Iron Guard",     50, 14,  14,  48,  5)]
    [InlineData("Night Stalker",  55, 20,   8,  58,  6)]
    [InlineData("Frost Wyvern",   75, 22,  12,  70,  8)]
    [InlineData("Chaos Knight",   85, 24,  16,  80, 10)]
    [InlineData("Lich King",     170, 38,  20, 130, 12)]
    public async Task Phase2Enemy_IsBeatable_AtMatchedPlayerLevel(
        string name, int hp, int atk, int def, int xp, int playerLevel)
    {
        var wins = 0;
        var losses = 0;
        var timeouts = 0;

        for (int seed = 0; seed < SimulationsPerEnemy; seed++)
        {
            var player = BuildPlayerAtLevel(playerLevel);
            var enemy  = BuildEnemy(name, hp, atk, def, xp);
            var engine = new CombatEngine(new FakeDisplayService(), new AlwaysAttackInputReader(), new Random(seed));

            CombatResult result = CombatResult.PlayerDied;
            bool completed = false;

            var combatTask = Task.Run(() => { result = engine.RunCombat(player, enemy); completed = true; });
            try { await combatTask.WaitAsync(CombatTimeout); }
            catch (TimeoutException) { }

            if (!completed)
                timeouts++;
            else if (result == CombatResult.Won)
                wins++;
            else
                losses++;
        }

        _output.WriteLine($"{name} (Lvl {playerLevel}) — Win: {wins,3} | Loss: {losses,3} | Timeout: {timeouts,3} / {SimulationsPerEnemy}");

        timeouts.Should().Be(0,
            $"combat with '{name}' must never exceed the {CombatTimeout.TotalSeconds}s timeout (possible infinite loop)");

        wins.Should().BeGreaterThan(0,
            $"'{name}' must be beatable by a level-{playerLevel} player — got 0/{SimulationsPerEnemy} wins");
    }

    // ── Test 2: Lich King balance — two-level sweep to characterise difficulty ────
    //
    // Balance finding: simulation reveals whether the Lich King is appropriately
    // challenging at tier-bottom (level 9) and tier-top (level 12).
    // Expected: level 9 is a real fight; level 12 should be winnable but not trivial.
    //
    // If win rate is 100% at BOTH levels the enemy needs a stat buff.
    // This test gates the minimum bar (must be beatable at level 9) and also asserts
    // the tier-bottom is genuinely hard (player must lose at least one fight out of 100).

    [Fact]
    public void LichKing_IsBeatable_AtTierTopLevel()
    {
        // T9-10 top: level 12 player must be able to beat the Lich King.
        const int level = 12;
        const int lichHP = 170, lichATK = 38, lichDEF = 20, lichXP = 130;

        var wins = Enumerable.Range(0, SimulationsPerEnemy)
            .Count(seed =>
            {
                var player = BuildPlayerAtLevel(level);
                var enemy  = BuildEnemy("Lich King", lichHP, lichATK, lichDEF, lichXP);
                return new CombatEngine(new FakeDisplayService(), new AlwaysAttackInputReader(), new Random(seed))
                           .RunCombat(player, enemy) == CombatResult.Won;
            });

        _output.WriteLine($"Lich King @ Lvl {level} — Win rate: {(double)wins / SimulationsPerEnemy:P0} ({wins}/{SimulationsPerEnemy})");

        wins.Should().BeGreaterThan(0,
            "Lich King must be beatable at tier-top (level 12)");
    }

    [Fact]
    public void LichKing_IsChallengingFight_AtTierBottomLevel()
    {
        // T9-10 bottom: at level 9 the Lich King should be a real threat.
        // Player (Lvl 9): ATK=26, DEF=13, MaxHP=180
        // Lich King:      ATK=38, DEF=20, HP=170
        // Raw DPS: player deals Max(1,26-20)=6/turn; Lich deals Max(1,38-13)=25/turn
        // → player needs 29 turns to kill; dies in ~7 — combat is extremely dangerous.
        const int level = 9;
        const int lichHP = 170, lichATK = 38, lichDEF = 20, lichXP = 130;

        var wins = 0;
        var losses = 0;

        for (int seed = 0; seed < SimulationsPerEnemy; seed++)
        {
            var player = BuildPlayerAtLevel(level);
            var enemy  = BuildEnemy("Lich King", lichHP, lichATK, lichDEF, lichXP);
            var result = new CombatEngine(new FakeDisplayService(), new AlwaysAttackInputReader(), new Random(seed))
                             .RunCombat(player, enemy);
            if (result == CombatResult.Won) wins++;
            else losses++;
        }

        _output.WriteLine($"Lich King @ Lvl {level} — Win rate: {(double)wins / SimulationsPerEnemy:P0} ({wins}/{SimulationsPerEnemy}) | Losses: {losses}");

        // At tier-bottom the Lich King must be genuinely dangerous: player should not win every fight.
        // ⚠ BALANCE NOTE: if this assertion fails (0 losses), the Lich King's stats need a buff
        //   so that a level-9 player faces a real threat. Escalate to Barton/Hill for tuning.
        losses.Should().BeGreaterThan(0,
            $"Lich King must be threatening at level {level} (tier bottom) — a 100% win rate means the enemy needs stronger stats");
    }

    // ── Test 3: No combat crashes across entire Phase 2 enemy roster ─────────────

    [Fact]
    public void AllPhase2Enemies_NoCombatCrash_AcrossAllSeeds()
    {
        var exceptions = new List<string>();

        foreach (var (name, hp, atk, def, xp, level) in Phase2Enemies)
        {
            for (int seed = 0; seed < SimulationsPerEnemy; seed++)
            {
                var player = BuildPlayerAtLevel(level);
                var enemy  = BuildEnemy(name, hp, atk, def, xp);
                var engine = new CombatEngine(new FakeDisplayService(), new AlwaysAttackInputReader(), new Random(seed));

                try
                {
                    engine.RunCombat(player, enemy);
                }
                catch (Exception ex)
                {
                    exceptions.Add($"[{name} seed={seed}] {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        if (exceptions.Count > 0)
            _output.WriteLine("Exceptions:\n" + string.Join("\n", exceptions));

        exceptions.Should().BeEmpty("no combat simulation should throw an exception");
    }

    // ── Test 4: Aggregate win/loss report (informational — always passes) ─────────

    [Fact]
    public void WinLossReport_AllPhase2Enemies()
    {
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine("  Phase 2 Combat Balance Report  (100 simulations each)");
        _output.WriteLine("  Player class: Warrior baseline (no equipment, no skills)");
        _output.WriteLine("═══════════════════════════════════════════════════════════");
        _output.WriteLine($"  {"Enemy",-16} {"Lvl",3}  {"Win%",6}  {"Wins",5}  {"Loss",5}");
        _output.WriteLine("  ───────────────────────────────────────────────────────");

        foreach (var (name, hp, atk, def, xp, level) in Phase2Enemies)
        {
            var wins = 0;
            for (int seed = 0; seed < SimulationsPerEnemy; seed++)
            {
                var player = BuildPlayerAtLevel(level);
                var enemy  = BuildEnemy(name, hp, atk, def, xp);
                var engine = new CombatEngine(new FakeDisplayService(), new AlwaysAttackInputReader(), new Random(seed));
                if (engine.RunCombat(player, enemy) == CombatResult.Won) wins++;
            }
            var losses = SimulationsPerEnemy - wins;
            _output.WriteLine($"  {name,-16} {level,3}  {(double)wins / SimulationsPerEnemy,6:P0}  {wins,5}  {losses,5}");
        }

        _output.WriteLine("═══════════════════════════════════════════════════════════");

        // This test always passes — it exists purely to surface the report in CI output.
        Assert.True(true);
    }
}

// ── Test-internal helpers ─────────────────────────────────────────────────────

/// <summary>
/// Lightweight enemy stub parameterised with Phase 2 stats.
/// Dodge is disabled (FlatDodgeChance = 0) so that results depend only on ATK/DEF.
/// </summary>
internal sealed class SimEnemy : Enemy
{
    public SimEnemy(string name, int hp, int atk, int def, int xp)
    {
        Name = name;
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = xp;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f;
    }
}

/// <summary>
/// Input reader that always returns "A" (Attack). Using an unlimited stream means
/// the player will keep attacking until combat terminates naturally; the per-test
/// Task timeout is the backstop against runaway loops.
/// </summary>
internal sealed class AlwaysAttackInputReader : IInputReader
{
    public string? ReadLine() => "A";
    public ConsoleKeyInfo? ReadKey() => null;
    public bool IsInteractive => false;
}
