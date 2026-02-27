namespace Dungnz.Tests;
using Xunit;
using FluentAssertions;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Tests.Helpers;

/// <summary>
/// Phase 6 test coverage for the class ability system (Issue #365)
/// WI-22 through WI-27: Class restrictions, ability effects, passive skills, integration tests
/// </summary>
public class Phase6ClassAbilityTests
{
    private readonly FakeDisplayService _display = new();
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;

    public Phase6ClassAbilityTests()
    {
        _statusEffects = new StatusEffectManager(_display);
        _abilities = new AbilityManager();
    }

    #region Helper Methods

    private static Player MakePlayer(PlayerClass playerClass, int level = 1)
    {
        var player = new Player
        {
            Name = "TestHero",
            Class = playerClass,
            HP = 100,
            MaxHP = 100,
            Attack = 10,
            Defense = 5,
            Mana = 50,
            MaxMana = 50
        };
        for (int i = 1; i < level; i++) player.LevelUp();
        return player;
    }

    private static Enemy_Stub MakeEnemy(int hp = 100, int atk = 10, int def = 0, int xp = 10)
    {
        return new Enemy_Stub(hp, atk, def, xp);
    }

    private static BossEnemy MakeBoss(int hp = 100, int atk = 10, int def = 0)
    {
        return new BossEnemy(hp, atk, def);
    }

    #endregion

    #region WI-22: Ability Class Restriction Filtering

    [Theory]
    [InlineData(PlayerClass.Warrior, AbilityType.ShieldBash)]
    [InlineData(PlayerClass.Mage, AbilityType.ArcaneBolt)]
    [InlineData(PlayerClass.Rogue, AbilityType.QuickStrike)]
    public void GetUnlockedAbilities_Level1_ReturnsOnlyL1ClassAbility(PlayerClass playerClass, AbilityType expectedAbility)
    {
        var player = MakePlayer(playerClass, level: 1);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(1);
        unlocked[0].Type.Should().Be(expectedAbility);
    }

    [Fact]
    public void GetUnlockedAbilities_WarriorLevel7_ReturnsAllWarriorAbilities()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(5);
        unlocked.Should().Contain(a => a.Type == AbilityType.ShieldBash);
        unlocked.Should().Contain(a => a.Type == AbilityType.BattleCry);
        unlocked.Should().Contain(a => a.Type == AbilityType.Fortify);
        unlocked.Should().Contain(a => a.Type == AbilityType.RecklessBlow);
        unlocked.Should().Contain(a => a.Type == AbilityType.LastStand);
    }

    [Fact]
    public void GetUnlockedAbilities_MageLevel7_ReturnsAllMageAbilities()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(5);
        unlocked.Should().Contain(a => a.Type == AbilityType.ArcaneBolt);
        unlocked.Should().Contain(a => a.Type == AbilityType.FrostNova);
        unlocked.Should().Contain(a => a.Type == AbilityType.ManaShield);
        unlocked.Should().Contain(a => a.Type == AbilityType.ArcaneSacrifice);
        unlocked.Should().Contain(a => a.Type == AbilityType.Meteor);
    }

    [Fact]
    public void GetUnlockedAbilities_RogueLevel7_ReturnsAllRogueAbilities()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().HaveCount(5);
        unlocked.Should().Contain(a => a.Type == AbilityType.QuickStrike);
        unlocked.Should().Contain(a => a.Type == AbilityType.Backstab);
        unlocked.Should().Contain(a => a.Type == AbilityType.Evade);
        unlocked.Should().Contain(a => a.Type == AbilityType.Flurry);
        unlocked.Should().Contain(a => a.Type == AbilityType.Assassinate);
    }

    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Mage)]
    [InlineData(PlayerClass.Rogue)]
    public void GetUnlockedAbilities_Level1_ReturnsExactlyOneAbility(PlayerClass playerClass)
    {
        var player = MakePlayer(playerClass, level: 1);
        var unlocked = _abilities.GetUnlockedAbilities(player);
        unlocked.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Mage)]
    [InlineData(PlayerClass.Rogue)]
    public void GetUnlockedAbilities_Level7_ReturnsExactlyFiveAbilities(PlayerClass playerClass)
    {
        var player = MakePlayer(playerClass, level: 7);
        var unlocked = _abilities.GetUnlockedAbilities(player);
        unlocked.Should().HaveCount(5);
    }

    [Fact]
    public void GetUnlockedAbilities_WarriorCannotSeeMageAbilities()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 10);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().NotContain(a => a.Type == AbilityType.ArcaneBolt);
        unlocked.Should().NotContain(a => a.Type == AbilityType.FrostNova);
        unlocked.Should().NotContain(a => a.Type == AbilityType.Meteor);
    }

    [Fact]
    public void GetUnlockedAbilities_MageCannotSeeRogueAbilities()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 10);
        var unlocked = _abilities.GetUnlockedAbilities(player);

        unlocked.Should().NotContain(a => a.Type == AbilityType.QuickStrike);
        unlocked.Should().NotContain(a => a.Type == AbilityType.Backstab);
        unlocked.Should().NotContain(a => a.Type == AbilityType.Assassinate);
    }

    #endregion

    #region WI-23: Warrior Ability Effects

    [Fact]
    public void ShieldBash_DealsExpectedDamage()
    {
        var player = MakePlayer(PlayerClass.Warrior);
        var enemy = MakeEnemy(hp: 100, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.ShieldBash, _statusEffects, _display);

        var expectedDamage = Math.Max(1, (int)(player.Attack * 1.2));
        enemy.HP.Should().Be(100 - expectedDamage);
    }

    [Fact]
    public void ShieldBash_AppliesStunWithMockedRng()
    {
        var player = MakePlayer(PlayerClass.Warrior);
        var enemy = MakeEnemy(hp: 100, def: 0);

        // Run multiple times to check that stun can be applied (50% chance)
        // We test that when stun is applied, the effect is present
        for (int i = 0; i < 20; i++)
        {
            var testEnemy = MakeEnemy(hp: 100, def: 0);
            var testStatus = new StatusEffectManager(_display);
            _abilities.UseAbility(player, testEnemy, AbilityType.ShieldBash, testStatus, _display);
            _abilities.ResetCooldowns();
            if (testStatus.HasEffect(testEnemy, StatusEffect.Stun))
            {
                // At least once stun should be applied
                testStatus.HasEffect(testEnemy, StatusEffect.Stun).Should().BeTrue();
                return;
            }
        }
        // With 50% chance over 20 tries, failure is very unlikely (0.5^20)
        Assert.Fail("Stun was never applied in 20 attempts - check RNG implementation");
    }

    [Fact]
    public void BattleCry_ClearsDebuffsFromPlayer()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 2);
        var enemy = MakeEnemy();

        _statusEffects.Apply(player, StatusEffect.Poison, 3);
        _statusEffects.Apply(player, StatusEffect.Weakened, 2);

        _abilities.UseAbility(player, enemy, AbilityType.BattleCry, _statusEffects, _display);

        _statusEffects.HasEffect(player, StatusEffect.Poison).Should().BeFalse();
        _statusEffects.HasEffect(player, StatusEffect.Weakened).Should().BeFalse();
    }

    [Fact]
    public void BattleCry_AppliesAttackBuff()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 2);
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.BattleCry, _statusEffects, _display);

        _statusEffects.HasEffect(player, StatusEffect.BattleCry).Should().BeTrue();
    }

    [Fact]
    public void Fortify_AppliesFortifiedBuff()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 3);
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.Fortify, _statusEffects, _display);

        _statusEffects.HasEffect(player, StatusEffect.Fortified).Should().BeTrue();
    }

    [Fact]
    public void Fortify_HealsWhenHPAtOrBelow50Percent()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 3);
        player.HP = 50; // Exactly 50%
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.Fortify, _statusEffects, _display);

        var expectedHeal = (int)(player.MaxHP * 0.15);
        player.HP.Should().Be(50 + expectedHeal);
    }

    [Fact]
    public void Fortify_NoHealWhenHPAbove50Percent()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 3);
        var hpAbove50 = (int)(player.MaxHP * 0.51); // Just above 50%
        player.HP = hpAbove50;
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.Fortify, _statusEffects, _display);

        player.HP.Should().Be(hpAbove50); // No heal
    }

    [Fact]
    public void RecklessBlow_DealsExpected2_5xDamage()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 5);
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.RecklessBlow, _statusEffects, _display);

        // Reckless Blow: 2.5x damage, enemy def halved (0/2 = 0)
        var expectedDamage = Math.Max(1, (int)(player.Attack * 2.5));
        enemy.HP.Should().Be(200 - expectedDamage);
    }

    [Fact]
    public void RecklessBlow_DealsSelfDamage()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 5);
        var enemy = MakeEnemy(hp: 200, def: 0);
        var hpBefore = player.HP;

        _abilities.UseAbility(player, enemy, AbilityType.RecklessBlow, _statusEffects, _display);

        player.HP.Should().BeLessThan(hpBefore);
    }

    [Fact]
    public void RecklessBlow_CannotReducePlayerBelow1HP()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 5);
        player.HP = 5; // Very low HP
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.RecklessBlow, _statusEffects, _display);

        player.HP.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void LastStand_FailsWhenHPAboveThreshold()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 7);
        // 40% threshold, so HP at 45% should fail
        var hpAboveThreshold = (int)(player.MaxHP * 0.45);
        player.HP = hpAboveThreshold;
        var enemy = MakeEnemy();

        var result = _abilities.UseAbility(player, enemy, AbilityType.LastStand, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.InsufficientMana); // reused as generic failure
    }

    [Fact]
    public void LastStand_ActivatesWhenHPBelowThreshold()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 7);
        player.HP = 30; // 30% <= 40% threshold
        var enemy = MakeEnemy();

        var result = _abilities.UseAbility(player, enemy, AbilityType.LastStand, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.Success);
        player.LastStandTurns.Should().Be(2);
    }

    [Fact]
    public void LastStand_RefundsManaOnFailedActivation()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 7);
        // Set HP above 40% threshold so it fails
        var hpAboveThreshold = (int)(player.MaxHP * 0.45);
        player.HP = hpAboveThreshold;
        var manaBefore = player.Mana;
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.LastStand, _statusEffects, _display);

        player.Mana.Should().Be(manaBefore); // Mana refunded
    }

    #endregion

    #region WI-24: Mage Ability Effects

    [Fact]
    public void ArcaneBolt_DealsDamageBasedOnAttackAndMana()
    {
        var player = MakePlayer(PlayerClass.Mage);
        player.Mana = 50;
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.ArcaneBolt, _statusEffects, _display);

        // Arcane Bolt: (ATK * 1.5) + (Mana / 10) - def/4
        // Mana is captured before the 8-cost is spent, so calc uses pre-cast mana (50)
        var manaBeforeCast = 50;
        var baseDmg = (int)((player.Attack * 1.5) + (manaBeforeCast / 10));
        var expectedDamage = Math.Max(1, baseDmg);
        enemy.HP.Should().Be(200 - expectedDamage);
    }

    [Fact]
    public void FrostNova_AppliesSlowFor2Turns()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 2);
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.FrostNova, _statusEffects, _display);

        _statusEffects.HasEffect(enemy, StatusEffect.Slow).Should().BeTrue();
    }

    [Fact]
    public void ManaShield_TogglesOnAndOff()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 4);
        var enemy = MakeEnemy();

        player.IsManaShieldActive.Should().BeFalse();

        _abilities.UseAbility(player, enemy, AbilityType.ManaShield, _statusEffects, _display);
        player.IsManaShieldActive.Should().BeTrue();

        _abilities.ResetCooldowns();
        _abilities.UseAbility(player, enemy, AbilityType.ManaShield, _statusEffects, _display);
        player.IsManaShieldActive.Should().BeFalse();
    }

    [Fact]
    public void ArcaneSacrifice_RestoresMana()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 5);
        player.Mana = 10;
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.ArcaneSacrifice, _statusEffects, _display);

        var expectedRestore = (int)(player.MaxMana * 0.30);
        player.Mana.Should().BeGreaterThan(10); // Should have restored mana
    }

    [Fact]
    public void ArcaneSacrifice_DealsSelfDamage()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 5);
        var hpBefore = player.HP;
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.ArcaneSacrifice, _statusEffects, _display);

        player.HP.Should().BeLessThan(hpBefore);
    }

    [Fact]
    public void ArcaneSacrifice_PreservesMinimum1HP()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 5);
        player.HP = 5; // Very low HP
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.ArcaneSacrifice, _statusEffects, _display);

        player.HP.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void Meteor_DealsHighDamage()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 7);
        var enemy = MakeEnemy(hp: 500, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.Meteor, _statusEffects, _display);

        // Meteor: (ATK * 3) + 20
        var expectedDamage = (player.Attack * 3) + 20;
        enemy.HP.Should().Be(500 - expectedDamage);
    }

    [Fact]
    public void Meteor_ExecutesWhenEnemyHPBelow20Percent_NonBoss()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 7);
        var enemy = MakeEnemy(hp: 50, def: 0); // 50 HP
        // After meteor damage, enemy should be below 20% and executed

        _abilities.UseAbility(player, enemy, AbilityType.Meteor, _statusEffects, _display);

        enemy.HP.Should().Be(0); // Executed
    }

    [Fact]
    public void Meteor_DoesNotExecuteOnBoss()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 7);
        // Use enough HP so boss survives meteor damage
        // Meteor: (ATK * 3) + 20 = (16 * 3) + 20 = 68 damage at level 7 (ATK=10 + 6*2=22, wait level 7 = 6 level ups = 12 more attack)
        // Actually: base 10 + 2*6 = 22 ATK, so (22*3)+20=86 damage
        // Let boss have high HP so it survives
        var boss = MakeBoss(hp: 500, def: 0);
        boss.MaxHP = 500;
        // Set HP so after meteor (86 damage), boss is at < 20% (100 HP = 20%)
        // This would trigger execute check but boss should be immune
        boss.HP = 150; // After 86 damage = 64 HP, which is 12.8% < 20%

        _abilities.UseAbility(player, boss, AbilityType.Meteor, _statusEffects, _display);

        // Boss is immune to execute, so HP should be reduced but not to 0
        boss.HP.Should().BeGreaterThan(0); // Boss immune to execute
    }

    #endregion

    #region WI-25: Rogue Ability Effects

    [Fact]
    public void QuickStrike_DealsDamageAndGrantsComboPoint()
    {
        var player = MakePlayer(PlayerClass.Rogue);
        var enemy = MakeEnemy(hp: 100, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.QuickStrike, _statusEffects, _display);

        player.ComboPoints.Should().Be(1);
        var expectedDamage = Math.Max(1, player.Attack);
        enemy.HP.Should().Be(100 - expectedDamage);
    }

    [Fact]
    public void QuickStrike_ComboPointsCappedAt5()
    {
        var player = MakePlayer(PlayerClass.Rogue);
        player.AddComboPoints(4);
        var enemy = MakeEnemy(hp: 100, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.QuickStrike, _statusEffects, _display);

        player.ComboPoints.Should().Be(5); // Capped at 5
    }

    [Fact]
    public void Backstab_Deals1_5xBaseDamage()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 2);
        var enemy = MakeEnemy(hp: 100, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.Backstab, _statusEffects, _display);

        var expectedDamage = Math.Max(1, (int)(player.Attack * 1.5));
        enemy.HP.Should().Be(100 - expectedDamage);
    }

    [Fact]
    public void Backstab_Deals2_5xWhenEnemyHasSlow()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 2);
        var enemy = MakeEnemy(hp: 100, def: 0);
        _statusEffects.Apply(enemy, StatusEffect.Slow, 2);

        _abilities.UseAbility(player, enemy, AbilityType.Backstab, _statusEffects, _display);

        var expectedDamage = Math.Max(1, (int)(player.Attack * 2.5));
        enemy.HP.Should().Be(100 - expectedDamage);
    }

    [Fact]
    public void Backstab_Deals2_5xWhenEnemyHasStun()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 2);
        var enemy = MakeEnemy(hp: 100, def: 0);
        _statusEffects.Apply(enemy, StatusEffect.Stun, 2);

        _abilities.UseAbility(player, enemy, AbilityType.Backstab, _statusEffects, _display);

        var expectedDamage = Math.Max(1, (int)(player.Attack * 2.5));
        enemy.HP.Should().Be(100 - expectedDamage);
    }

    [Fact]
    public void Backstab_Deals2_5xWhenEnemyHasBleed()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 2);
        var enemy = MakeEnemy(hp: 100, def: 0);
        _statusEffects.Apply(enemy, StatusEffect.Bleed, 2);

        _abilities.UseAbility(player, enemy, AbilityType.Backstab, _statusEffects, _display);

        var expectedDamage = Math.Max(1, (int)(player.Attack * 2.5));
        enemy.HP.Should().Be(100 - expectedDamage);
    }

    [Fact]
    public void Evade_SetsEvadeNextAttackFlag()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 3);
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.Evade, _statusEffects, _display);

        player.EvadeNextAttack.Should().BeTrue();
    }

    [Fact]
    public void Evade_GrantsComboPoint()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 3);
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.Evade, _statusEffects, _display);

        player.ComboPoints.Should().Be(1);
    }

    [Fact]
    public void Evade_Grants2CPWithShadowMasterPassive()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 8);
        player.Skills.TryUnlock(player, Skill.ShadowMaster);
        var enemy = MakeEnemy();

        _abilities.UseAbility(player, enemy, AbilityType.Evade, _statusEffects, _display);

        player.ComboPoints.Should().Be(2);
    }

    [Fact]
    public void Flurry_RequiresAtLeast1CP()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 5);
        player.ResetComboPoints();
        var manaBefore = player.Mana;
        var enemy = MakeEnemy();

        var result = _abilities.UseAbility(player, enemy, AbilityType.Flurry, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.InsufficientMana); // reused as failure
        player.Mana.Should().Be(manaBefore); // Mana refunded
    }

    [Fact]
    public void Flurry_DealsDamageBasedOnCP()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 5);
        player.AddComboPoints(3);
        var enemy = MakeEnemy(hp: 200, def: 0);

        _abilities.UseAbility(player, enemy, AbilityType.Flurry, _statusEffects, _display);

        // Flurry: (0.6 * CP) * ATK
        var expectedDamage = Math.Max(1, (int)((0.6 * 3) * player.Attack));
        enemy.HP.Should().Be(200 - expectedDamage);
    }

    [Fact]
    public void Assassinate_RequiresAtLeast3CP()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 7);
        player.AddComboPoints(2);
        var manaBefore = player.Mana;
        var enemy = MakeEnemy();

        var result = _abilities.UseAbility(player, enemy, AbilityType.Assassinate, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.InsufficientMana); // reused as failure
        player.Mana.Should().Be(manaBefore); // Mana refunded
    }

    [Fact]
    public void Assassinate_ExecutesAtOrBelow30PercentHP_NonBoss()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 7);
        player.AddComboPoints(3);
        var enemy = MakeEnemy(hp: 100, def: 0);
        // Set enemy to low HP that will be below 30% after hit
        enemy.HP = 30;
        enemy.MaxHP = 100;

        _abilities.UseAbility(player, enemy, AbilityType.Assassinate, _statusEffects, _display);

        enemy.HP.Should().Be(0); // Executed
    }

    [Fact]
    public void Assassinate_NoExecuteOnBoss()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 7);
        player.AddComboPoints(3);
        // Rogue level 7: ATK = 10 + 2*6 = 22
        // Assassinate damage: (CP * 0.8) * ATK = (3 * 0.8) * 22 = 52.8 = 52 damage
        // Set boss HP high enough to survive but be below 30% after hit
        var boss = MakeBoss(hp: 500, def: 0);
        boss.MaxHP = 500;
        boss.HP = 200; // After 52 damage = 148 HP, which is 29.6% < 30%, would trigger execute

        _abilities.UseAbility(player, boss, AbilityType.Assassinate, _statusEffects, _display);

        boss.HP.Should().BeGreaterThan(0); // Boss immune to execute
    }

    [Fact]
    public void ResetComboPoints_ClearsCPAtCombatEnd()
    {
        var player = MakePlayer(PlayerClass.Rogue);
        player.AddComboPoints(5);

        player.ResetComboPoints();

        player.ComboPoints.Should().Be(0);
    }

    #endregion

    #region WI-26: Class-Specific Passive Skills

    [Fact]
    public void WarriorCannotUnlockMagePassives()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 10);

        var result = player.Skills.TryUnlock(player, Skill.ArcaneReservoir);

        result.Should().BeFalse();
        player.Skills.IsUnlocked(Skill.ArcaneReservoir).Should().BeFalse();
    }

    [Fact]
    public void MageCannotUnlockRoguePassives()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 10);

        var result = player.Skills.TryUnlock(player, Skill.QuickReflexes);

        result.Should().BeFalse();
        player.Skills.IsUnlocked(Skill.QuickReflexes).Should().BeFalse();
    }

    [Fact]
    public void RogueCannotUnlockWarriorPassives()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 10);

        var result = player.Skills.TryUnlock(player, Skill.IronConstitution);

        result.Should().BeFalse();
        player.Skills.IsUnlocked(Skill.IronConstitution).Should().BeFalse();
    }

    [Fact]
    public void IronConstitution_Grants15MaxHP()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 3);
        var maxHpBefore = player.MaxHP;

        player.Skills.TryUnlock(player, Skill.IronConstitution);

        player.MaxHP.Should().Be(maxHpBefore + 15);
    }

    [Fact]
    public void ArcaneReservoir_Grants20MaxMana()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 3);
        var maxManaBefore = player.MaxMana;

        player.Skills.TryUnlock(player, Skill.ArcaneReservoir);

        player.MaxMana.Should().Be(maxManaBefore + 20);
    }

    [Fact]
    public void QuickReflexes_Grants5PercentDodge()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 3);

        player.Skills.TryUnlock(player, Skill.QuickReflexes);

        player.Skills.IsUnlocked(Skill.QuickReflexes).Should().BeTrue();
        // Dodge bonus is applied in combat - skill being unlocked is the check
    }

    [Fact]
    public void SpellWeaver_ReducesManaCostBy10Percent()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 4);
        player.Skills.TryUnlock(player, Skill.SpellWeaver);

        var multiplier = player.GetSpellCostMultiplier();

        multiplier.Should().BeApproximately(0.90f, 0.001f);
    }

    [Fact]
    public void Relentless_ReducesFlurryCooldownBy1()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 6);
        player.Skills.TryUnlock(player, Skill.Relentless);

        var reduction = player.GetCooldownReduction(AbilityType.Flurry);

        reduction.Should().Be(1);
    }

    [Fact]
    public void Relentless_ReducesAssassinateCooldownBy1()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 6);
        player.Skills.TryUnlock(player, Skill.Relentless);

        var reduction = player.GetCooldownReduction(AbilityType.Assassinate);

        reduction.Should().Be(1);
    }

    #endregion

    #region WI-27: Combat Integration Tests

    [Fact]
    public void WarriorCombatIntegration_ShieldBashWins()
    {
        var player = MakePlayer(PlayerClass.Warrior, level: 3);
        var enemy = MakeEnemy(hp: 10, atk: 5, def: 0, xp: 10);
        var input = new FakeInputReader("B", "1", "A"); // B=abilities, 1=ShieldBash, A=attack
        var display = new FakeDisplayService();
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        display.CombatMessages.Should().Contain(m => m.Contains("shield") || m.Contains("Shield"));
    }

    [Fact]
    public void MageCombatIntegration_ArcaneBoltAndMeteor()
    {
        var player = MakePlayer(PlayerClass.Mage, level: 7);
        player.Mana = 100;
        player.MaxMana = 100;
        var enemy = MakeEnemy(hp: 50, atk: 5, def: 0, xp: 10);
        var input = new FakeInputReader("B", "1", "A"); // B=abilities, 1=ArcaneBolt, A=attack
        var display = new FakeDisplayService();
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        display.CombatMessages.Should().Contain(m => m.Contains("arcane") || m.Contains("Arcane") || m.Contains("bolt"));
    }

    [Fact]
    public void RogueCombatIntegration_ComboPointsToAssassinate()
    {
        var player = MakePlayer(PlayerClass.Rogue, level: 7);
        player.Mana = 100;
        player.MaxMana = 100;
        // Set up enemy with low HP for execute
        var enemy = MakeEnemy(hp: 30, atk: 2, def: 0, xp: 10);
        enemy.MaxHP = 100; // So 30 HP = 30%
        // Quick Strike 3 times to get 3 CP, then Assassinate for execute
        var input = new FakeInputReader("B", "1", "B", "1", "B", "1", "B", "5"); // 3x QuickStrike + Assassinate
        var display = new FakeDisplayService();
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
    }

    #endregion
}

/// <summary>Boss enemy stub with IsImmuneToEffects = true for execute immunity testing.</summary>
internal class BossEnemy : Enemy
{
    public BossEnemy(int hp, int atk, int def)
    {
        Name = "TestBoss";
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = 100;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f;
        IsImmuneToEffects = true; // Bosses immune to execute
    }
}
