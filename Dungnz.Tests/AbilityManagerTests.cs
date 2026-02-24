namespace Dungnz.Tests;
using Xunit;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Display;
using Dungnz.Tests.Helpers;

/// <summary>
/// Tests for the ability manager using the new class-based ability system (Phase 1-5).
/// These tests use Warrior as the default class with Warrior abilities:
/// - ShieldBash (L1), BattleCry (L2), Fortify (L3), RecklessBlow (L5), LastStand (L7)
/// </summary>
public class AbilityManagerTests
{
    private readonly IDisplayService _display = new TestDisplayService();
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;
    private readonly Player _player;
    private readonly Enemy _enemy;

    public AbilityManagerTests()
    {
        _statusEffects = new StatusEffectManager(_display);
        _abilities = new AbilityManager();
        _player = new Player { Name = "TestHero", Class = PlayerClass.Warrior };
        _enemy = new Enemy_Stub(100, 10, 5, 20) { Name = "TestMonster" };
    }

    [Fact]
    public void GetUnlockedAbilities_Level1_ReturnsShieldBashOnly()
    {
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Single(unlocked);
        Assert.Equal(AbilityType.ShieldBash, unlocked[0].Type);
    }

    [Fact]
    public void GetUnlockedAbilities_Level3_ReturnsShieldBashBattleCryAndFortify()
    {
        _player.LevelUp();
        _player.LevelUp();
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Equal(3, unlocked.Count);
        Assert.Contains(unlocked, a => a.Type == AbilityType.ShieldBash);
        Assert.Contains(unlocked, a => a.Type == AbilityType.BattleCry);
        Assert.Contains(unlocked, a => a.Type == AbilityType.Fortify);
    }

    [Fact]
    public void GetUnlockedAbilities_Level5_Returns4Abilities()
    {
        for (int i = 0; i < 4; i++) _player.LevelUp();
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Equal(4, unlocked.Count);
        Assert.Contains(unlocked, a => a.Type == AbilityType.RecklessBlow);
    }

    [Fact]
    public void GetUnlockedAbilities_Level7_ReturnsAllWarriorAbilities()
    {
        for (int i = 0; i < 6; i++) _player.LevelUp();
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Equal(5, unlocked.Count);
        Assert.Contains(unlocked, a => a.Type == AbilityType.LastStand);
    }

    [Fact]
    public void GetAvailableAbilities_InsufficientMana_ReturnsEmpty()
    {
        _player.SpendMana(25);
        var available = _abilities.GetAvailableAbilities(_player);
        Assert.Empty(available);
    }

    [Fact]
    public void GetAvailableAbilities_OnCooldown_ExcludesAbility()
    {
        _abilities.PutOnCooldown(AbilityType.ShieldBash, 2);
        var available = _abilities.GetAvailableAbilities(_player);
        Assert.Empty(available);
    }

    [Fact]
    public void TickCooldowns_ReducesCooldownByOne()
    {
        _abilities.PutOnCooldown(AbilityType.ShieldBash, 2);
        _abilities.TickCooldowns();
        Assert.Equal(1, _abilities.GetCooldown(AbilityType.ShieldBash));
    }

    [Fact]
    public void TickCooldowns_CooldownReachesZero_NotNegative()
    {
        _abilities.PutOnCooldown(AbilityType.ShieldBash, 1);
        _abilities.TickCooldowns();
        Assert.Equal(0, _abilities.GetCooldown(AbilityType.ShieldBash));
        _abilities.TickCooldowns();
        Assert.Equal(0, _abilities.GetCooldown(AbilityType.ShieldBash));
    }

    [Fact]
    public void UseAbility_ShieldBash_DealsDamage()
    {
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.ShieldBash, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.Success, result);
        var expectedDamage = Math.Max(1, (int)(_player.Attack * 1.2) - _enemy.Defense);
        Assert.Equal(100 - expectedDamage, _enemy.HP);
    }

    [Fact]
    public void UseAbility_ShieldBash_DeductsMana()
    {
        var originalMana = _player.Mana;
        _abilities.UseAbility(_player, _enemy, AbilityType.ShieldBash, _statusEffects, _display);
        Assert.Equal(originalMana - 8, _player.Mana);
    }

    [Fact]
    public void UseAbility_ShieldBash_PutsOnCooldown()
    {
        _abilities.UseAbility(_player, _enemy, AbilityType.ShieldBash, _statusEffects, _display);
        Assert.True(_abilities.IsOnCooldown(AbilityType.ShieldBash));
        Assert.Equal(2, _abilities.GetCooldown(AbilityType.ShieldBash));
    }

    [Fact]
    public void UseAbility_InsufficientMana_ReturnsFailure()
    {
        _player.SpendMana(25);
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.ShieldBash, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.InsufficientMana, result);
    }

    [Fact]
    public void UseAbility_OnCooldown_ReturnsFailure()
    {
        _abilities.UseAbility(_player, _enemy, AbilityType.ShieldBash, _statusEffects, _display);
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.ShieldBash, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.OnCooldown, result);
    }

    [Fact]
    public void UseAbility_NotUnlocked_ReturnsFailure()
    {
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.Fortify, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.NotUnlocked, result);
    }

    [Fact]
    public void UseAbility_BattleCry_AppliesBattleCryEffect()
    {
        for (int i = 0; i < 1; i++) _player.LevelUp();
        _abilities.UseAbility(_player, _enemy, AbilityType.BattleCry, _statusEffects, _display);
        Assert.True(_statusEffects.HasEffect(_player, StatusEffect.BattleCry));
    }

    [Fact]
    public void UseAbility_Fortify_AppliesFortifiedEffect()
    {
        for (int i = 0; i < 2; i++) _player.LevelUp();
        _abilities.UseAbility(_player, _enemy, AbilityType.Fortify, _statusEffects, _display);
        Assert.True(_statusEffects.HasEffect(_player, StatusEffect.Fortified));
    }

    [Fact]
    public void UseAbility_RecklessBlow_DealsHighDamage()
    {
        for (int i = 0; i < 4; i++) _player.LevelUp();
        var hpBefore = _enemy.HP;
        _abilities.UseAbility(_player, _enemy, AbilityType.RecklessBlow, _statusEffects, _display);
        Assert.True(_enemy.HP < hpBefore);
    }

    [Fact]
    public void AbilityCosts_MatchSpecification()
    {
        var shieldBash = _abilities.GetAbility(AbilityType.ShieldBash);
        var battleCry = _abilities.GetAbility(AbilityType.BattleCry);
        var fortify = _abilities.GetAbility(AbilityType.Fortify);
        var recklessBlow = _abilities.GetAbility(AbilityType.RecklessBlow);

        Assert.Equal(8, shieldBash?.ManaCost);
        Assert.Equal(10, battleCry?.ManaCost);
        Assert.Equal(12, fortify?.ManaCost);
        Assert.Equal(15, recklessBlow?.ManaCost);
    }

    [Fact]
    public void AbilityCooldowns_MatchSpecification()
    {
        var shieldBash = _abilities.GetAbility(AbilityType.ShieldBash);
        var battleCry = _abilities.GetAbility(AbilityType.BattleCry);
        var fortify = _abilities.GetAbility(AbilityType.Fortify);
        var recklessBlow = _abilities.GetAbility(AbilityType.RecklessBlow);

        Assert.Equal(2, shieldBash?.CooldownTurns);
        Assert.Equal(4, battleCry?.CooldownTurns);
        Assert.Equal(3, fortify?.CooldownTurns);
        Assert.Equal(3, recklessBlow?.CooldownTurns);
    }

    [Fact]
    public void AbilityUnlockLevels_MatchSpecification()
    {
        var shieldBash = _abilities.GetAbility(AbilityType.ShieldBash);
        var battleCry = _abilities.GetAbility(AbilityType.BattleCry);
        var fortify = _abilities.GetAbility(AbilityType.Fortify);
        var recklessBlow = _abilities.GetAbility(AbilityType.RecklessBlow);

        Assert.Equal(1, shieldBash?.UnlockLevel);
        Assert.Equal(2, battleCry?.UnlockLevel);
        Assert.Equal(3, fortify?.UnlockLevel);
        Assert.Equal(5, recklessBlow?.UnlockLevel);
    }
}
