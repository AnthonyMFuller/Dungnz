using System.Linq;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Issue #1273 — 11 baseline combat tests that pin mechanics (turn-loop order, cooldowns,
/// status interactions, boss phase transitions, narration hooks) before new combat features land.
/// </summary>
public class CombatBaselineTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static Player MakePlayer(
        int hp = 100, int atk = 20, int def = 5,
        PlayerClass cls = PlayerClass.Warrior,
        int mana = 100, int level = 5)
        => new Player
        {
            HP = hp, MaxHP = hp, Attack = atk, Defense = def,
            Class = cls, Mana = mana, MaxMana = mana, Level = level
        };

    // ════════════════════════════════════════════════════════════════════════
    // Tests 1–4: Turn-loop phase ordering (status → passive → player → enemy)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test 1 — Status-effect phase executes BEFORE the player acts.
    /// A Poison tick that reduces player HP to 0 kills the player before they attack,
    /// which is only possible if the status phase runs first.
    /// </summary>
    [Fact]
    public void TurnLoop_StatusPhase_TicksBeforePlayerActs()
    {
        // Arrange: player HP=3, Poison deals 3 dmg on its first tick → lethal before player acts.
        var player = MakePlayer(hp: 3, atk: 1, def: 0);
        player.ActiveEffects.Add(new ActiveEffect(StatusEffect.Poison, 1));
        var enemy = new Enemy_Stub(9999, 1, 0, 10); // unkillable in one hit
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: player dies from Poison before their attack executes
        result.Should().Be(CombatResult.PlayerDied);
        enemy.HP.Should().Be(9999, "player never got to act; the status-tick phase ran first");
    }

    /// <summary>
    /// Test 2 — Player phase executes BEFORE the enemy phase on the same turn.
    /// A player who one-shots the enemy prevents the enemy from ever retaliating,
    /// which is only possible if the player acts first.
    /// </summary>
    [Fact]
    public void TurnLoop_PlayerPhase_ExecutesBeforeEnemyPhase()
    {
        // Arrange: player one-shots enemy; enemy would one-shot player if given a turn.
        var player = MakePlayer(hp: 5, atk: 100, def: 0);
        var enemy = new Enemy_Stub(1, 100, 0, 10); // ATK=100 would kill player
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: player killed enemy first; enemy never got its turn
        result.Should().Be(CombatResult.Won, "player acts before enemy — kills enemy before retaliation");
        player.HP.Should().Be(5, "enemy never executed its turn");
    }

    /// <summary>
    /// Test 3 — Enemy phase executes AFTER the player phase when the enemy survives.
    /// Player-dealt damage is too low to kill in one hit, so the enemy retaliates.
    /// </summary>
    [Fact]
    public void TurnLoop_EnemyPhase_ExecutesAfterPlayerPhase()
    {
        // Arrange: player ATK=10 kills enemy HP=50 in ~5 turns; enemy ATK=5 deals damage each turn.
        //         Player HP=500 is high enough to outlast all 5 enemy attacks.
        var player = MakePlayer(hp: 500, atk: 10, def: 0);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        var input = new FakeInputReader(Enumerable.Repeat("A", 20).ToArray());
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: combat resolved AND player took damage (enemy attacked after each player turn)
        result.Should().Be(CombatResult.Won);
        player.HP.Should().BeLessThan(player.MaxHP, "enemy attacked after the player's turn each round");
    }

    /// <summary>
    /// Test 4 — A status-effect tick at turn start can kill the enemy before the enemy acts.
    /// An injected StatusEffectManager with Poison pre-applied to the enemy verifies this.
    /// </summary>
    [Fact]
    public void TurnLoop_EnemyStatusTick_KillsEnemyBeforeEnemyActs()
    {
        // Arrange: enemy HP=3, Poison deals exactly 3 damage on tick → dies before acting.
        var display = new FakeDisplayService();
        var sem = new StatusEffectManager(display);
        var enemy = new Enemy_Stub(3, 5, 0, 10);
        sem.Apply(enemy, StatusEffect.Poison, 1); // 3-dmg tick kills HP=3 enemy instantly

        var player = MakePlayer(hp: 200, atk: 1, def: 100); // player ATK=1, DEF=100 — survives
        var input = new FakeInputReader("A", "A");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9),
            statusEffects: sem);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: enemy died from DoT before it could attack; player HP is unchanged
        result.Should().Be(CombatResult.Won);
        player.HP.Should().Be(200, "enemy died from a status tick before its turn — player never took damage");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tests 5–6: Cooldown mechanics
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test 5 — An ability with a remaining cooldown is blocked from the available list.
    /// </summary>
    [Fact]
    public void Cooldown_Ability_IsBlocked_WhenCooldownGreaterThanZero()
    {
        // Arrange
        var abilities = new AbilityManager();
        var player = MakePlayer(cls: PlayerClass.Warrior, mana: 100, level: 5);

        // Act: put Shield Bash on a 2-turn cooldown
        abilities.PutOnCooldown(AbilityType.ShieldBash, 2);

        // Assert: blocked by IsOnCooldown AND excluded from the available list
        abilities.IsOnCooldown(AbilityType.ShieldBash).Should().BeTrue();
        abilities.GetAvailableAbilities(player)
            .Should().NotContain(a => a.Type == AbilityType.ShieldBash,
                "an ability on cooldown must not appear in the available list");
    }

    /// <summary>
    /// Test 6 — A 3-turn cooldown expires after exactly 3 ticks and not before.
    /// </summary>
    [Fact]
    public void Cooldown_ThreeTurnCooldown_ExpiresAfterExactlyThreeTicks()
    {
        // Arrange: Fortify has a natural 3-turn cooldown
        var abilities = new AbilityManager();
        abilities.PutOnCooldown(AbilityType.Fortify, 3);

        // Tick 1 — still blocked
        abilities.TickCooldowns();
        abilities.IsOnCooldown(AbilityType.Fortify).Should().BeTrue("still on cooldown after 1 tick");

        // Tick 2 — still blocked
        abilities.TickCooldowns();
        abilities.IsOnCooldown(AbilityType.Fortify).Should().BeTrue("still on cooldown after 2 ticks");

        // Tick 3 — cooldown reaches 0; must be cleared
        abilities.TickCooldowns();
        abilities.IsOnCooldown(AbilityType.Fortify).Should().BeFalse("cooldown must expire after exactly 3 ticks");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Test 7: Ability damage quantification — Theory, one case per class
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test 7 (×4) — Each class's primary damage ability reduces enemy HP when used.
    /// Tests the ability pipeline end-to-end for Warrior, Mage, Rogue, and Paladin.
    /// </summary>
    [Theory]
    [InlineData(PlayerClass.Warrior, AbilityType.ShieldBash)]  // Math.Max(1, ATK*1.2 − DEF)
    [InlineData(PlayerClass.Mage,    AbilityType.ArcaneBolt)]  // ATK*1.5 + mana/10 − DEF/4
    [InlineData(PlayerClass.Rogue,   AbilityType.QuickStrike)] // ATK − DEF
    [InlineData(PlayerClass.Paladin, AbilityType.HolyStrike)]  // ATK − DEF (1.5× vs undead)
    public void AbilityDamage_PrimaryAbility_DealsDamageToEnemy(
        PlayerClass cls, AbilityType abilityType)
    {
        // Arrange
        var display = new FakeDisplayService();
        var abilities = new AbilityManager();
        var player = MakePlayer(hp: 100, atk: 20, def: 5, cls: cls, mana: 100, level: 5);
        var enemy = new Enemy_Stub(200, 5, 0, 10); // DEF=0 — no reduction
        var sem = new StatusEffectManager(display);
        int hpBefore = enemy.HP;

        // Act: call UseAbility directly — no full combat loop needed
        abilities.UseAbility(player, enemy, abilityType, sem, display);

        // Assert: enemy HP dropped — proves damage > 0 through the ability pipeline
        enemy.HP.Should().BeLessThan(hpBefore,
            $"{cls} ability {abilityType} must deal positive damage to the enemy");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tests 8–9: Multi-effect status interactions (Burn + Poison)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test 8 — Burn and Poison both apply their tick damage in the same ProcessTurnStart call.
    /// </summary>
    [Fact]
    public void StatusEffect_BurnAndPoison_BothTickOnSameTurn()
    {
        // Arrange
        var display = new FakeDisplayService();
        var sem = new StatusEffectManager(display);
        var enemy = new Enemy_Stub(200, 5, 0, 10);
        sem.Apply(enemy, StatusEffect.Poison, 3);
        sem.Apply(enemy, StatusEffect.Burn, 3);
        int hpBefore = enemy.HP;

        // Act: process one turn-start tick
        sem.ProcessTurnStart(enemy);

        // Assert: combined damage ≥ Poison fallback (3) + Burn fallback (8) = 11
        int damage = hpBefore - enemy.HP;
        damage.Should().BeGreaterThanOrEqualTo(11,
            "Poison (≥3 dmg) and Burn (≥8 dmg) must both tick on the same turn");
    }

    /// <summary>
    /// Test 9 — Burn and Poison both expire (are removed) after their stated duration.
    /// </summary>
    [Fact]
    public void StatusEffect_BurnAndPoison_BothExpire_AfterDuration()
    {
        // Arrange: 2-turn effects
        var display = new FakeDisplayService();
        var sem = new StatusEffectManager(display);
        var enemy = new Enemy_Stub(9999, 5, 0, 10);
        sem.Apply(enemy, StatusEffect.Poison, 2);
        sem.Apply(enemy, StatusEffect.Burn, 2);

        // Tick 1: duration 2→1; effects still active
        sem.ProcessTurnStart(enemy);
        sem.HasEffect(enemy, StatusEffect.Poison).Should().BeTrue("Poison active after 1 of 2 turns");
        sem.HasEffect(enemy, StatusEffect.Burn).Should().BeTrue("Burn active after 1 of 2 turns");

        // Tick 2: duration 1→0; both removed
        sem.ProcessTurnStart(enemy);
        sem.HasEffect(enemy, StatusEffect.Poison).Should().BeFalse("Poison must expire after 2 turns");
        sem.HasEffect(enemy, StatusEffect.Burn).Should().BeFalse("Burn must expire after 2 turns");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Test 10: Boss phase transition
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test 10 — A boss phase ability fires exactly once when HP crosses its threshold.
    /// FiredPhases is the authoritative record; it prevents the phase from re-triggering.
    /// </summary>
    [Fact]
    public void BossPhase_HPThreshold_FiresExactlyOnce()
    {
        // Arrange: default DungeonBoss (HP=100, DEF=15). Player ATK=80 → deals 65 per hit.
        //   Turn 1: phase check at 100% (>50%) — no fire. Player hits: HP=100−65=35.
        //   Turn 2: phase check at 35% (≤50%) — fires. Player hits: HP=35−65=−30, dead.
        var boss = new DungeonBoss();
        boss.Defense = 0; // remove defense so damage is deterministic (ATK−0 = ATK)
        boss.Phases.Add(new BossPhase(0.50, "WeakenAura"));

        var player = MakePlayer(hp: 500, atk: 60, def: 100); // ATK=60 → 60 dmg/turn; DEF=100 absorbs boss
        var input = new FakeInputReader("A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act
        engine.RunCombat(player, boss);

        // Assert: FiredPhases proves the ability triggered — and only once
        boss.FiredPhases.Should().Contain("WeakenAura",
            "phase must fire when boss HP falls to or below 50%");
        boss.FiredPhases.Should().HaveCount(1, "no additional phases should have fired");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Test 11: Narration hook timing (NarrationSpy pattern)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test 11 — Combat intro narration fires at combat start, before any per-turn output.
    /// Uses a NarrationSpy pattern: asserts the hook fired and its position — not its content.
    /// </summary>
    [Fact]
    public void NarrationHook_CombatIntro_FiresAtCombatStart_BeforeFirstTurn()
    {
        // Arrange: simple 1-turn combat; enemy killed on first attack
        var player = MakePlayer(hp: 100, atk: 100, def: 5);
        var enemy = new Enemy_Stub(1, 5, 0, 10);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        // Act
        engine.RunCombat(player, enemy);

        // Assert (spy — no string content checks):
        // ShowCombat (narration) entries are prefixed "combat:" in AllOutput.
        // ShowCombatStatus (turn HUD) entries are prefixed "status:".
        // The intro narration must appear before any per-turn status update.
        var narrateEntries = display.AllOutput.Where(x => x.StartsWith("combat:")).ToList();
        narrateEntries.Should().NotBeEmpty("combat intro narration must fire at least once");

        int firstNarrateIdx = display.AllOutput.IndexOf(narrateEntries.First());
        int firstStatusIdx  = display.AllOutput.FindIndex(x => x.StartsWith("status:"));

        if (firstStatusIdx >= 0)
            firstNarrateIdx.Should().BeLessThan(firstStatusIdx,
                "intro narration must fire before the first per-turn ShowCombatStatus call");
    }
}
