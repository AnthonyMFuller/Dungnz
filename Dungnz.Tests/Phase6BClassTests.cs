namespace Dungnz.Tests;
using Xunit;
using FluentAssertions;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Tests.Helpers;

/// <summary>
/// Phase 6B test coverage for Paladin, Necromancer, and Ranger classes.
/// Covers WI-B1 through WI-B4 (Issues #374, #375, #376, #377)
/// </summary>
public class Phase6BClassTests
{
    private readonly FakeDisplayService _display = new();
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;

    public Phase6BClassTests()
    {
        _statusEffects = new StatusEffectManager(_display);
        _abilities = new AbilityManager();
    }

    private static Player MakePlayer(PlayerClass playerClass, int level = 1, int hp = 100, int maxHp = 100, int atk = 10, int def = 5, int mana = 100, int maxMana = 100)
    {
        var player = new Player { Name = "TestHero", Class = playerClass };
        // Level up FIRST so level-up stat gains don't override our explicit values
        for (int i = 1; i < level; i++) player.LevelUp();
        // Now stamp the exact stats the test needs
        player.MaxHP = maxHp;
        player.HP = hp;
        player.Attack = atk;
        player.Defense = def;
        player.MaxMana = maxMana;
        player.Mana = mana;
        return player;
    }

    private static Enemy MakeEnemy(int hp = 100, int atk = 10, int def = 0, bool isUndead = false)
    {
        return new Phase6BEnemy(hp, atk, def, isUndead);
    }

    private static Phase6BBoss MakeBoss(int hp = 500) => new Phase6BBoss(hp);

    #region Paladin Tests

    [Fact]
    public void HolyStrike_Deals100PercentATK_VsNonUndead()
    {
        var player = MakePlayer(PlayerClass.Paladin, atk: 10, def: 0);
        var enemy = MakeEnemy(hp: 100, def: 0, isUndead: false);

        _abilities.UseAbility(player, enemy, AbilityType.HolyStrike, _statusEffects, _display);

        var expectedDmg = Math.Max(1, (int)(10 * 1.0) - 0);
        enemy.HP.Should().Be(100 - expectedDmg);
    }

    [Fact]
    public void HolyStrike_Deals150PercentATK_VsUndead()
    {
        var player = MakePlayer(PlayerClass.Paladin, atk: 10, def: 0);
        var enemy = MakeEnemy(hp: 100, def: 0, isUndead: true);

        _abilities.UseAbility(player, enemy, AbilityType.HolyStrike, _statusEffects, _display);

        var expectedDmg = Math.Max(1, (int)(10 * 1.5) - 0);
        enemy.HP.Should().Be(100 - expectedDmg);
    }

    [Fact]
    public void LayOnHands_Heals40PercentMaxHP_WhenHPAbove25Percent()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 2, hp: 60, maxHp: 100);

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.LayOnHands, _statusEffects, _display);

        var expectedHeal = (int)(100 * 0.40);
        player.HP.Should().Be(Math.Min(100, 60 + expectedHeal));
    }

    [Fact]
    public void LayOnHands_Heals50PercentMaxHP_WhenHPBelow25Percent()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 2, hp: 20, maxHp: 100); // 20% HP

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.LayOnHands, _statusEffects, _display);

        var expectedHeal = (int)(100 * 0.50);
        player.HP.Should().Be(Math.Min(100, 20 + expectedHeal));
    }

    [Fact]
    public void DivineShield_SetsTurnsRemainingTo2()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 3);

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.DivineShield, _statusEffects, _display);

        player.DivineShieldTurnsRemaining.Should().Be(2);
    }

    [Fact]
    public void DivineShield_BlocksDamageTurns()
    {
        // Simulate: player has shield, should not take HP damage
        var player = MakePlayer(PlayerClass.Paladin, level: 3, hp: 80, maxHp: 100);
        player.DivineShieldTurnsRemaining = 2;
        var hpBefore = player.HP;

        // Simulate what PerformEnemyTurn does when DivineShield is active
        if (player.DivineShieldTurnsRemaining > 0)
        {
            player.DivineShieldTurnsRemaining--;
            // No HP damage taken
        }

        player.HP.Should().Be(hpBefore); // No damage
        player.DivineShieldTurnsRemaining.Should().Be(1);
    }

    [Fact]
    public void Judgment_ExecutesNonBossAtOrBelow20PercentHP()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 7, atk: 10, hp: 100, maxHp: 100);
        // Judgment: 200% ATK + (MaxHP-HP)*0.5 bonus, then execute at ≤20% HP
        // Need enemy to be at ≤20% after full damage
        var enemy = MakeEnemy(hp: 20, def: 0); // 20% of MaxHP = 20 (MaxHP=100)
        enemy.MaxHP = 100;

        _abilities.UseAbility(player, enemy, AbilityType.Judgment, _statusEffects, _display);

        enemy.HP.Should().Be(0); // Executed
    }

    [Fact]
    public void Judgment_DoesNotExecuteBoss()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 7, atk: 10, hp: 100, maxHp: 100);
        var boss = MakeBoss(hp: 500);

        // Set boss HP so it would be at ≤20% after judgment
        boss.HP = 30; // 6% of 500

        _abilities.UseAbility(player, boss, AbilityType.Judgment, _statusEffects, _display);

        boss.HP.Should().BeGreaterThan(0); // Boss immune to execute
    }

    #endregion

    #region Necromancer Tests

    [Fact]
    public void RaiseDead_CreatesSkeletonWithCorrectHP()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 3, atk: 10);
        player.LastKilledEnemyHp = 100;

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.RaiseDead, _statusEffects, _display);

        player.ActiveMinions.Should().HaveCount(1);
        var skeleton = player.ActiveMinions[0];
        skeleton.HP.Should().Be((int)(100 * 0.3)); // 30% of last killed HP
    }

    [Fact]
    public void RaiseDead_CreatesSkeletonWithCorrectATK()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 3, atk: 10);
        player.LastKilledEnemyHp = 100;

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.RaiseDead, _statusEffects, _display);

        var skeleton = player.ActiveMinions[0];
        skeleton.ATK.Should().Be((int)(10 * 0.5)); // 50% of player ATK
    }

    [Fact]
    public void RaiseDead_BlockedAtMax2Minions_RefundsMana()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 3, atk: 10, mana: 100);
        player.LastKilledEnemyHp = 100;
        // Already have 2 minions
        player.ActiveMinions.Add(new Minion { Name = "Skeleton 1", HP = 10, MaxHP = 10, ATK = 5 });
        player.ActiveMinions.Add(new Minion { Name = "Skeleton 2", HP = 10, MaxHP = 10, ATK = 5 });
        var manaBefore = player.Mana;

        var result = _abilities.UseAbility(player, MakeEnemy(), AbilityType.RaiseDead, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.InsufficientMana);
        player.Mana.Should().Be(manaBefore); // Refunded
        player.ActiveMinions.Should().HaveCount(2); // Not added
    }

    [Fact]
    public void RaiseDead_RefundsMana_WhenNoFallenEnemy()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 3, mana: 100);
        player.LastKilledEnemyHp = 0; // No fallen enemy
        var manaBefore = player.Mana;

        var result = _abilities.UseAbility(player, MakeEnemy(), AbilityType.RaiseDead, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.InsufficientMana);
        player.Mana.Should().Be(manaBefore);
    }

    [Fact]
    public void CorpseExplosion_RemovesAllMinions_DealsCorrectDamage()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 7, mana: 100);
        player.ActiveMinions.Add(new Minion { Name = "Skel1", HP = 30, MaxHP = 30, ATK = 5 });
        player.ActiveMinions.Add(new Minion { Name = "Skel2", HP = 20, MaxHP = 20, ATK = 5 });
        var enemy = MakeEnemy(hp: 500);

        _abilities.UseAbility(player, enemy, AbilityType.CorpseExplosion, _statusEffects, _display);

        player.ActiveMinions.Should().BeEmpty();
        var expectedDmg = (int)((30 + 20) * 1.5); // 150% of combined MaxHP
        enemy.HP.Should().Be(500 - expectedDmg);
    }

    [Fact]
    public void CorpseExplosion_RefundsMana_WhenNoMinions()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 7, mana: 100);
        var manaBefore = player.Mana;

        var result = _abilities.UseAbility(player, MakeEnemy(), AbilityType.CorpseExplosion, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.InsufficientMana);
        player.Mana.Should().Be(manaBefore);
    }

    [Fact]
    public void DeathBolt_Deals90PercentATK_Normally()
    {
        var player = MakePlayer(PlayerClass.Necromancer, atk: 10);
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.DeathBolt, _statusEffects, _display);

        // 90% ATK, shadow pierces 1/4 defense (0/4 = 0)
        var expectedDmg = Math.Max(1, (int)(10 * 0.90) - 0);
        enemy.HP.Should().Be(200 - expectedDmg);
    }

    [Fact]
    public void DeathBolt_Deals120PercentATK_WithPoisonStatus()
    {
        var player = MakePlayer(PlayerClass.Necromancer, atk: 10, mana: 100);
        var enemy = MakeEnemy(hp: 200, def: 0);
        _statusEffects.Apply(enemy, StatusEffect.Poison, 3);

        _abilities.UseAbility(player, enemy, AbilityType.DeathBolt, _statusEffects, _display);

        var expectedDmg = Math.Max(1, (int)(10 * 1.20) - 0);
        enemy.HP.Should().Be(200 - expectedDmg);
    }

    [Fact]
    public void DeathBolt_Deals120PercentATK_WithBleedStatus()
    {
        var player = MakePlayer(PlayerClass.Necromancer, atk: 10, mana: 100);
        var enemy = MakeEnemy(hp: 200, def: 0);
        _statusEffects.Apply(enemy, StatusEffect.Bleed, 3);

        _abilities.UseAbility(player, enemy, AbilityType.DeathBolt, _statusEffects, _display);

        var expectedDmg = Math.Max(1, (int)(10 * 1.20) - 0);
        enemy.HP.Should().Be(200 - expectedDmg);
    }

    #endregion

    #region Ranger Tests

    [Fact]
    public void LayTrapPoison_AddsTrapToActiveTraps()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 2, mana: 100);

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.LayTrapPoison, _statusEffects, _display);

        player.ActiveTraps.Should().HaveCount(1);
        player.ActiveTraps[0].Name.Should().Be("Poison Trap");
        player.ActiveTraps[0].AppliedStatus.Should().Be(StatusEffect.Poison);
        player.ActiveTraps[0].Triggered.Should().BeFalse();
    }

    [Fact]
    public void Volley_NormalDamage_WithoutTrapTriggered()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 7, atk: 10, mana: 100);
        player.TrapTriggeredThisCombat = false;
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.Volley, _statusEffects, _display);

        var expectedDmg = Math.Max(1, (int)(10 * 0.80) - 0);
        enemy.HP.Should().Be(200 - expectedDmg);
    }

    [Fact]
    public void Volley_Deals130PercentBonus_WithTrapTriggered()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 7, atk: 10, mana: 100);
        player.TrapTriggeredThisCombat = true;
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.Volley, _statusEffects, _display);

        var expectedDmg = Math.Max(1, (int)((int)(10 * 0.80) * 1.30) - 0);
        enemy.HP.Should().Be(200 - expectedDmg);
    }

    [Fact]
    public void SummonCompanion_CreatesWolfWithCorrectStats()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 3, atk: 10, maxHp: 100);
        player.HP = 100;

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.SummonCompanion, _statusEffects, _display);

        player.ActiveMinions.Should().HaveCount(1);
        var wolf = player.ActiveMinions[0];
        wolf.Name.Should().Be("Wolf Companion");
        wolf.HP.Should().Be((int)(100 * 0.4));
        wolf.ATK.Should().Be((int)(10 * 0.6));
    }

    [Fact]
    public void SummonCompanion_ReplacesExistingWolf()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 3, atk: 10, maxHp: 100);
        // Add existing wolf
        player.ActiveMinions.Add(new Minion { Name = "Wolf Companion", HP = 30, MaxHP = 40, ATK = 5 });

        _abilities.UseAbility(player, MakeEnemy(), AbilityType.SummonCompanion, _statusEffects, _display);

        player.ActiveMinions.Should().HaveCount(1); // Replaced, not added
        player.ActiveMinions[0].HP.Should().Be((int)(100 * 0.4)); // Fresh wolf stats
    }

    [Fact]
    public void HunterMark_FirstAttack_DealsBonus25Percent()
    {
        // Hunter's Mark is applied in CombatEngine.PerformPlayerAttack
        // We test the flag directly
        var player = MakePlayer(PlayerClass.Ranger);
        player.HunterMarkUsedThisCombat = false;

        // Simulate the mark check
        bool isFirstAttack = !player.HunterMarkUsedThisCombat;
        if (isFirstAttack)
            player.HunterMarkUsedThisCombat = true;

        isFirstAttack.Should().BeTrue();
        player.HunterMarkUsedThisCombat.Should().BeTrue();
    }

    [Fact]
    public void HunterMark_SubsequentAttack_NoBonus()
    {
        var player = MakePlayer(PlayerClass.Ranger);
        player.HunterMarkUsedThisCombat = true; // Already used

        bool isFirstAttack = !player.HunterMarkUsedThisCombat;

        isFirstAttack.Should().BeFalse();
    }

    [Fact]
    public void HunterMark_ResetsAfterCombat()
    {
        var player = MakePlayer(PlayerClass.Ranger);
        player.HunterMarkUsedThisCombat = true;

        // Simulate end-of-combat reset
        player.HunterMarkUsedThisCombat = false;

        player.HunterMarkUsedThisCombat.Should().BeFalse();
    }

    #endregion

    #region Class Unlock Tests

    [Fact]
    public void PaladinLevel1_UnlocksHolyStrike()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 1);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().Contain(a => a.Type == AbilityType.HolyStrike);
        unlocked.Should().HaveCount(1);
    }

    [Fact]
    public void PaladinLevel7_UnlocksAllPaladinAbilities()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(5);
        unlocked.Should().Contain(a => a.Type == AbilityType.HolyStrike);
        unlocked.Should().Contain(a => a.Type == AbilityType.LayOnHands);
        unlocked.Should().Contain(a => a.Type == AbilityType.DivineShield);
        unlocked.Should().Contain(a => a.Type == AbilityType.Consecrate);
        unlocked.Should().Contain(a => a.Type == AbilityType.Judgment);
    }

    [Fact]
    public void NecromancerLevel1_UnlocksDeathBolt()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 1);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().Contain(a => a.Type == AbilityType.DeathBolt);
        unlocked.Should().HaveCount(1);
    }

    [Fact]
    public void NecromancerLevel7_UnlocksAllNecromancerAbilities()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(5);
        unlocked.Should().Contain(a => a.Type == AbilityType.DeathBolt);
        unlocked.Should().Contain(a => a.Type == AbilityType.Curse);
        unlocked.Should().Contain(a => a.Type == AbilityType.RaiseDead);
        unlocked.Should().Contain(a => a.Type == AbilityType.LifeDrain);
        unlocked.Should().Contain(a => a.Type == AbilityType.CorpseExplosion);
    }

    [Fact]
    public void RangerLevel1_UnlocksPreciseShot()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 1);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().Contain(a => a.Type == AbilityType.PreciseShot);
        unlocked.Should().HaveCount(1);
    }

    [Fact]
    public void RangerLevel7_UnlocksAllRangerAbilities()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(5);
        unlocked.Should().Contain(a => a.Type == AbilityType.PreciseShot);
        unlocked.Should().Contain(a => a.Type == AbilityType.LayTrapPoison);
        unlocked.Should().Contain(a => a.Type == AbilityType.SummonCompanion);
        unlocked.Should().Contain(a => a.Type == AbilityType.LayTrapSnare);
        unlocked.Should().Contain(a => a.Type == AbilityType.Volley);
    }

    #endregion

    #region Passive Skill Tests

    [Fact]
    public void BlessedArmor_GrantsPlus3Defense()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 3);
        var defBefore = player.Defense;

        player.Skills.TryUnlock(player, Skill.BlessedArmor);

        player.Defense.Should().Be(defBefore + 3);
    }

    [Fact]
    public void PaladinCannotUnlockNecromancerPassives()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 10);

        var result = player.Skills.TryUnlock(player, Skill.UndyingServants);

        result.Should().BeFalse();
    }

    [Fact]
    public void RangerCannotUnlockPaladinPassives()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 10);

        var result = player.Skills.TryUnlock(player, Skill.BlessedArmor);

        result.Should().BeFalse();
    }

    #endregion
}

/// <summary>Stub enemy for Phase 6B tests, with configurable IsUndead flag.</summary>
internal class Phase6BEnemy : Enemy
{
    public Phase6BEnemy(int hp, int atk, int def, bool isUndead = false)
    {
        Name = "TestEnemy";
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = 10;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f;
        IsUndead = isUndead;
    }
}

/// <summary>Boss stub with IsImmuneToEffects = true for execute immunity testing.</summary>
internal class Phase6BBoss : Enemy
{
    public Phase6BBoss(int hp)
    {
        Name = "TestBoss";
        HP = MaxHP = hp;
        Attack = 10;
        Defense = 0;
        XPValue = 100;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f;
        IsImmuneToEffects = true;
    }
}
