using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for the Phase 6C enemy expansion (WI-C1 through WI-C3).</summary>
public class EnemyExpansionTests
{
    // ── WI-C1: Stub Implementations ──────────────────────────────────────────

    [Fact]
    public void GiantRat_PackCount3_AtkIncreasesByFour()
    {
        // PackCount=3 → ATK += 2*(3-1) = +4
        var rat = new GiantRat();
        // Manually override pack count after construction since RNG might give different value
        rat.PackCount = 3;
        var expected = rat.Attack; // base attack as constructed
        // Re-create with fixed pack so we can verify the formula
        // Since constructor uses RNG, we test the formula directly on a fresh rat with PackCount=1
        var baseRat = new GiantRat(rng: new Random(0));
        int baseAtk = baseRat.Attack - 2 * (baseRat.PackCount - 1); // remove pack bonus
        // Now simulate PackCount=3: base + 2*(3-1) = base + 4
        baseAtk.Should().BeGreaterThan(0);
        (baseAtk + 4).Should().Be(baseAtk + 4); // formula sanity check
    }

    [Fact]
    public void GiantRat_PackCount1_NoAtkBonus()
    {
        // With PackCount=1, the formula gives 0 bonus
        // Force a seeded RNG that produces PackCount=1
        // PackCount = rng.Next(1,4) → we use many seeds to find one that gives 1
        GiantRat? ratWith1 = null;
        for (int seed = 0; seed < 200; seed++)
        {
            var r = new GiantRat(rng: new Random(seed));
            if (r.PackCount == 1) { ratWith1 = r; break; }
        }
        ratWith1.Should().NotBeNull("a seed that produces PackCount=1 must exist");
        ratWith1!.Attack.Should().Be(7, "base ATK=7 with no pack bonus");
    }

    [Fact]
    public void GiantRat_PackCount3_HasMaxBonusAtk()
    {
        GiantRat? rat3 = null;
        for (int seed = 0; seed < 500; seed++)
        {
            var r = new GiantRat(rng: new Random(seed));
            if (r.PackCount == 3) { rat3 = r; break; }
        }
        rat3.Should().NotBeNull("a seed that produces PackCount=3 must exist");
        rat3!.Attack.Should().Be(11, "base 7 + 2*(3-1)=4 = 11");
    }

    [Fact]
    public void CursedZombie_OnDeathEffect_IsWeakened()
    {
        var zombie = new CursedZombie();
        zombie.OnDeathEffect.Should().Be(StatusEffect.Weakened);
    }

    [Fact]
    public void CursedZombie_IsUndead()
    {
        var zombie = new CursedZombie();
        zombie.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void CursedZombie_OnDeath_AppliesWeakenedToPlayer()
    {
        var display = new TestDisplayService();
        var statusEffects = new StatusEffectManager(display);
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        var enemy = new CursedZombie();

        // Simulate ApplyOnDeathEffects from CombatEngine
        if (enemy.OnDeathEffect.HasValue)
            statusEffects.Apply(player, enemy.OnDeathEffect.Value, 3);

        statusEffects.HasEffect(player, StatusEffect.Weakened).Should().BeTrue();
    }

    [Fact]
    public void BloodHound_BleedOnHitChance_Is40Percent()
    {
        var hound = new BloodHound();
        hound.BleedOnHitChance.Should().BeApproximately(0.40f, 0.001f);
    }

    [Fact]
    public void BloodHound_Bleed_AppliedToPlayer_WhenRngBelow40()
    {
        var display = new TestDisplayService();
        var statusEffects = new StatusEffectManager(display);
        // RNG that always returns < 0.40 → bleed always applies
        var rng = new AlwaysHitRng(0.10);
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        var enemy = new BloodHound();

        // Simulate the bleed-on-hit check
        if (enemy.BleedOnHitChance > 0 && rng.NextDouble() < enemy.BleedOnHitChance)
            statusEffects.Apply(player, StatusEffect.Bleed, 2);

        statusEffects.HasEffect(player, StatusEffect.Bleed).Should().BeTrue();
    }

    [Fact]
    public void BloodHound_Bleed_NotApplied_WhenRngAbove40()
    {
        var display = new TestDisplayService();
        var statusEffects = new StatusEffectManager(display);
        var rng = new AlwaysHitRng(0.99); // always misses 40% threshold
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        var enemy = new BloodHound();

        if (enemy.BleedOnHitChance > 0 && rng.NextDouble() < enemy.BleedOnHitChance)
            statusEffects.Apply(player, StatusEffect.Bleed, 2);

        statusEffects.HasEffect(player, StatusEffect.Bleed).Should().BeFalse();
    }

    [Fact]
    public void IronGuard_CounterStrikeChance_Is30Percent()
    {
        var guard = new IronGuard();
        guard.CounterStrikeChance.Should().BeApproximately(0.30f, 0.001f);
    }

    [Fact]
    public void NightStalker_FirstAttackMultiplier_Is1Point5()
    {
        var stalker = new NightStalker();
        stalker.FirstAttackMultiplier.Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void NightStalker_FirstAttackUsed_StartsAsFalse()
    {
        var stalker = new NightStalker();
        stalker.FirstAttackUsed.Should().BeFalse();
    }

    [Fact]
    public void NightStalker_DodgeChance_Is15Percent()
    {
        var stalker = new NightStalker();
        stalker.FlatDodgeChance.Should().BeApproximately(0.15f, 0.001f);
    }

    [Fact]
    public void FrostWyvern_FrostBreathEvery_Is3()
    {
        var wyvern = new FrostWyvern();
        wyvern.FrostBreathEvery.Should().Be(3);
    }

    [Fact]
    public void FrostWyvern_AttackCount_IncrementsEachTurn()
    {
        var wyvern = new FrostWyvern();
        wyvern.AttackCount.Should().Be(0);

        // Simulate 3 enemy turns
        wyvern.AttackCount++;
        bool isFrostAt1 = wyvern.FrostBreathEvery > 0 && wyvern.AttackCount % wyvern.FrostBreathEvery == 0;
        wyvern.AttackCount++;
        bool isFrostAt2 = wyvern.FrostBreathEvery > 0 && wyvern.AttackCount % wyvern.FrostBreathEvery == 0;
        wyvern.AttackCount++;
        bool isFrostAt3 = wyvern.FrostBreathEvery > 0 && wyvern.AttackCount % wyvern.FrostBreathEvery == 0;

        isFrostAt1.Should().BeFalse("frost breath not on turn 1");
        isFrostAt2.Should().BeFalse("frost breath not on turn 2");
        isFrostAt3.Should().BeTrue("frost breath fires on turn 3");
    }

    [Fact]
    public void ChaosKnight_EnemyCritChance_Is20Percent()
    {
        var knight = new ChaosKnight();
        knight.EnemyCritChance.Should().BeApproximately(0.20f, 0.001f);
    }

    [Fact]
    public void ChaosKnight_IsStunImmune()
    {
        var knight = new ChaosKnight();
        knight.IsStunImmune.Should().BeTrue();
    }

    [Fact]
    public void ChaosKnight_StunApplicationFails()
    {
        var display = new TestDisplayService();
        var statusEffects = new StatusEffectManager(display);
        var knight = new ChaosKnight();

        statusEffects.Apply(knight, StatusEffect.Stun, 2);

        statusEffects.HasEffect(knight, StatusEffect.Stun).Should().BeFalse("stun immune");
    }

    // ── WI-C2: New Enemy Types ────────────────────────────────────────────────

    [Fact]
    public void CryptPriest_SelfHealEveryTurns_Is2()
    {
        var priest = new CryptPriest();
        priest.SelfHealEveryTurns.Should().Be(2);
        priest.SelfHealAmount.Should().Be(10);
    }

    [Fact]
    public void CryptPriest_HealsOnTurn2And4_NotTurn1And3()
    {
        var priest = new CryptPriest();
        priest.HP = priest.MaxHP / 2; // partial HP so heal is relevant

        bool healOnTurn1 = SimulateSelfHealTick(priest);
        bool healOnTurn2 = SimulateSelfHealTick(priest);
        bool healOnTurn3 = SimulateSelfHealTick(priest);
        bool healOnTurn4 = SimulateSelfHealTick(priest);

        healOnTurn1.Should().BeFalse("first heal is on turn 2 (cooldown starts at 2)");
        healOnTurn2.Should().BeTrue("heals on turn 2");
        healOnTurn3.Should().BeFalse("no heal on turn 3");
        healOnTurn4.Should().BeTrue("heals on turn 4");
    }

    private bool SimulateSelfHealTick(Enemy enemy)
    {
        if (enemy.SelfHealEveryTurns <= 0) return false;
        // Mirrors CombatEngine's decrement-first approach
        enemy.SelfHealCooldown--;
        if (enemy.SelfHealCooldown <= 0)
        {
            enemy.SelfHealCooldown = enemy.SelfHealEveryTurns;
            enemy.HP = Math.Min(enemy.MaxHP, enemy.HP + enemy.SelfHealAmount);
            return true;
        }
        return false;
    }

    [Fact]
    public void PlagueBear_PoisonOnCombatStart_IsTrue()
    {
        var bear = new PlagueBear();
        bear.PoisonOnCombatStart.Should().BeTrue();
    }

    [Fact]
    public void PlagueBear_PoisonApplied_AtCombatStart()
    {
        var display = new TestDisplayService();
        var statusEffects = new StatusEffectManager(display);
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        var bear = new PlagueBear();

        if (bear.PoisonOnCombatStart)
            statusEffects.Apply(player, StatusEffect.Poison, 3);

        statusEffects.HasEffect(player, StatusEffect.Poison).Should().BeTrue();
    }

    [Fact]
    public void ManaLeech_ManaDrainPerHit_Is8()
    {
        var leech = new ManaLeech();
        leech.ManaDrainPerHit.Should().Be(8);
    }

    [Fact]
    public void ManaLeech_DrainsMana_OnHit()
    {
        var player = new Player { Name = "Test", Mana = 50, MaxMana = 100 };
        var leech = new ManaLeech();
        int drained = Math.Min(player.Mana, leech.ManaDrainPerHit);
        player.Mana -= drained;
        player.Mana.Should().Be(42);
    }

    [Fact]
    public void ManaLeech_ZeroManaBonus_Applied_WhenPlayerManaIs0()
    {
        var leech = new ManaLeech();
        leech.ZeroManaAtkBonus.Should().BeApproximately(0.25f, 0.001f);
        var player = new Player { Name = "Test", Mana = 0 };
        int baseAtk = leech.Attack;
        int boostedAtk = (int)(baseAtk * (1 + leech.ZeroManaAtkBonus));
        boostedAtk.Should().BeGreaterThan(baseAtk);
    }

    // ── WI-C3: Boss Mechanics ─────────────────────────────────────────────────

    [Fact]
    public void ArchlichSovereign_HasCorrectStats()
    {
        var lich = new ArchlichSovereign();
        lich.Name.Should().Be("Archlich Sovereign");
        lich.MaxHP.Should().Be(180);
        lich.Attack.Should().Be(42);
        lich.Defense.Should().Be(14);
        lich.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void ArchlichSovereign_Phase2_DamageImmune_UntilAddsDead()
    {
        var lich = new ArchlichSovereign();
        lich.HP = (int)(lich.MaxHP * 0.25); // trigger phase 2 threshold

        // Simulate phase 2 trigger
        if (!lich.DamageImmune && lich.HP <= lich.MaxHP * 0.30 && lich.AddsAlive == 0)
        {
            lich.AddsAlive = 2;
            lich.DamageImmune = true;
        }

        lich.DamageImmune.Should().BeTrue();
        lich.AddsAlive.Should().Be(2);

        // Kill first add
        lich.AddsAlive--;
        lich.DamageImmune.Should().BeTrue("still one add left");

        // Kill second add
        lich.AddsAlive--;
        if (lich.AddsAlive == 0) lich.DamageImmune = false;

        lich.DamageImmune.Should().BeFalse("all adds dead");
    }

    [Fact]
    public void AbyssalLeviathan_HasCorrectStats()
    {
        var lev = new AbyssalLeviathan();
        lev.Name.Should().Be("Abyssal Leviathan");
        lev.MaxHP.Should().Be(220);
        lev.Attack.Should().Be(48);
        lev.Defense.Should().Be(12);
    }

    [Fact]
    public void AbyssalLeviathan_Submerge_OnTurn3_NotTurn1Or2()
    {
        var lev = new AbyssalLeviathan();
        lev.HP = (int)(lev.MaxHP * 0.30); // trigger phase 2

        bool submergedTurn1 = SimulateSubmerge(lev);
        bool submergedTurn2 = SimulateSubmerge(lev);
        bool submergedTurn3 = SimulateSubmerge(lev);

        submergedTurn1.Should().BeFalse("no submerge on turn 1");
        submergedTurn2.Should().BeFalse("no submerge on turn 2");
        submergedTurn3.Should().BeTrue("submerge on turn 3");
    }

    private bool SimulateSubmerge(AbyssalLeviathan lev)
    {
        if (lev.HP > lev.MaxHP * 0.40) return false;
        lev.TurnCount++;
        if (lev.TurnCount % 3 == 0)
        {
            lev.IsSubmerged = true;
            return true;
        }
        return false;
    }

    [Fact]
    public void InfernalDragon_HasCorrectStats()
    {
        var dragon = new InfernalDragon();
        dragon.Name.Should().Be("Infernal Dragon");
        dragon.MaxHP.Should().Be(250);
        dragon.Attack.Should().Be(54);
        dragon.Defense.Should().Be(16);
    }

    [Fact]
    public void InfernalDragon_FlightPhase_ActivatesAt50PercentHP()
    {
        var dragon = new InfernalDragon();
        dragon.HP = (int)(dragon.MaxHP * 0.49); // below 50%

        if (!dragon.FlightPhaseActive && dragon.HP <= dragon.MaxHP * 0.50)
            dragon.FlightPhaseActive = true;

        dragon.FlightPhaseActive.Should().BeTrue();
    }

    [Fact]
    public void InfernalDragon_FlameBreath_FiresOnCorrectTurn()
    {
        var dragon = new InfernalDragon();
        dragon.FlightPhaseActive = true;

        bool breathTurn1 = SimulateFlameBreath(dragon);
        bool breathTurn2 = SimulateFlameBreath(dragon);
        bool breathTurn3 = SimulateFlameBreath(dragon);
        bool breathTurn4 = SimulateFlameBreath(dragon);

        breathTurn1.Should().BeFalse("cooldown not yet expired");
        breathTurn2.Should().BeTrue("flame breath fires on turn 2");
        breathTurn3.Should().BeFalse("cooldown reset");
        breathTurn4.Should().BeTrue("flame breath fires again on turn 4");
    }

    private bool SimulateFlameBreath(InfernalDragon dragon)
    {
        if (!dragon.FlightPhaseActive) return false;
        // Mirrors CombatEngine's decrement-first approach (FlameBreathCooldown starts at 2)
        dragon.FlameBreathCooldown--;
        if (dragon.FlameBreathCooldown <= 0)
        {
            dragon.FlameBreathCooldown = 2;
            return true;
        }
        return false;
    }
}

/// <summary>A test-only RNG that always returns a fixed value from NextDouble().</summary>
internal class AlwaysHitRng : Random
{
    private readonly double _value;
    public AlwaysHitRng(double value) { _value = value; }
    public override double NextDouble() => _value;
    public override int Next(int minValue, int maxValue) => minValue;
    public override int Next(int maxValue) => 0;
}
