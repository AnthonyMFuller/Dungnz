namespace Dungnz.Tests;
using Xunit;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Display;
using Dungnz.Tests.Helpers;

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
        _player = new Player { Name = "TestHero" };
        _enemy = new Enemy_Stub(100, 10, 5, 20) { Name = "TestMonster" };
    }

    [Fact]
    public void GetUnlockedAbilities_Level1_ReturnsPowerStrikeOnly()
    {
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Single(unlocked);
        Assert.Equal(AbilityType.PowerStrike, unlocked[0].Type);
    }

    [Fact]
    public void GetUnlockedAbilities_Level3_ReturnsPowerStrikeAndDefensiveStance()
    {
        _player.LevelUp();
        _player.LevelUp();
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Equal(2, unlocked.Count);
        Assert.Contains(unlocked, a => a.Type == AbilityType.PowerStrike);
        Assert.Contains(unlocked, a => a.Type == AbilityType.DefensiveStance);
    }

    [Fact]
    public void GetUnlockedAbilities_Level5_Returns3Abilities()
    {
        for (int i = 0; i < 4; i++) _player.LevelUp();
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Equal(3, unlocked.Count);
        Assert.Contains(unlocked, a => a.Type == AbilityType.PoisonDart);
    }

    [Fact]
    public void GetUnlockedAbilities_Level7_ReturnsAllAbilities()
    {
        for (int i = 0; i < 6; i++) _player.LevelUp();
        var unlocked = _abilities.GetUnlockedAbilities(_player);
        Assert.Equal(4, unlocked.Count);
        Assert.Contains(unlocked, a => a.Type == AbilityType.SecondWind);
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
        _abilities.PutOnCooldown(AbilityType.PowerStrike, 2);
        var available = _abilities.GetAvailableAbilities(_player);
        Assert.Empty(available);
    }

    [Fact]
    public void TickCooldowns_ReducesCooldownByOne()
    {
        _abilities.PutOnCooldown(AbilityType.PowerStrike, 2);
        _abilities.TickCooldowns();
        Assert.Equal(1, _abilities.GetCooldown(AbilityType.PowerStrike));
    }

    [Fact]
    public void TickCooldowns_CooldownReachesZero_NotNegative()
    {
        _abilities.PutOnCooldown(AbilityType.PowerStrike, 1);
        _abilities.TickCooldowns();
        Assert.Equal(0, _abilities.GetCooldown(AbilityType.PowerStrike));
        _abilities.TickCooldowns();
        Assert.Equal(0, _abilities.GetCooldown(AbilityType.PowerStrike));
    }

    [Fact]
    public void UseAbility_PowerStrike_DealsTwiceNormalDamage()
    {
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.PowerStrike, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.Success, result);
        var expectedDamage = Math.Max(1, _player.Attack * 2 - _enemy.Defense);
        Assert.Equal(100 - expectedDamage, _enemy.HP);
    }

    [Fact]
    public void UseAbility_PowerStrike_DeductsMana()
    {
        var originalMana = _player.Mana;
        _abilities.UseAbility(_player, _enemy, AbilityType.PowerStrike, _statusEffects, _display);
        Assert.Equal(originalMana - 10, _player.Mana);
    }

    [Fact]
    public void UseAbility_PowerStrike_PutsOnCooldown()
    {
        _abilities.UseAbility(_player, _enemy, AbilityType.PowerStrike, _statusEffects, _display);
        Assert.True(_abilities.IsOnCooldown(AbilityType.PowerStrike));
        Assert.Equal(2, _abilities.GetCooldown(AbilityType.PowerStrike));
    }

    [Fact]
    public void UseAbility_InsufficientMana_ReturnsFailure()
    {
        _player.SpendMana(25);
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.PowerStrike, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.InsufficientMana, result);
    }

    [Fact]
    public void UseAbility_OnCooldown_ReturnsFailure()
    {
        _abilities.UseAbility(_player, _enemy, AbilityType.PowerStrike, _statusEffects, _display);
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.PowerStrike, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.OnCooldown, result);
    }

    [Fact]
    public void UseAbility_NotUnlocked_ReturnsFailure()
    {
        var result = _abilities.UseAbility(_player, _enemy, AbilityType.DefensiveStance, _statusEffects, _display);
        Assert.Equal(UseAbilityResult.NotUnlocked, result);
    }

    [Fact]
    public void UseAbility_DefensiveStance_AppliesFortifiedEffect()
    {
        for (int i = 0; i < 2; i++) _player.LevelUp();
        _abilities.UseAbility(_player, _enemy, AbilityType.DefensiveStance, _statusEffects, _display);
        Assert.True(_statusEffects.HasEffect(_player, StatusEffect.Fortified));
    }

    [Fact]
    public void UseAbility_PoisonDart_AppliesPoisonEffect()
    {
        for (int i = 0; i < 4; i++) _player.LevelUp();
        _abilities.UseAbility(_player, _enemy, AbilityType.PoisonDart, _statusEffects, _display);
        Assert.True(_statusEffects.HasEffect(_enemy, StatusEffect.Poison));
    }

    [Fact]
    public void UseAbility_SecondWind_Heals30PercentMaxHP()
    {
        for (int i = 0; i < 6; i++) _player.LevelUp();
        _player.TakeDamage(50);
        var hpBefore = _player.HP;
        _abilities.UseAbility(_player, _enemy, AbilityType.SecondWind, _statusEffects, _display);
        var expectedHeal = (int)(_player.MaxHP * 0.3);
        Assert.Equal(hpBefore + expectedHeal, _player.HP);
    }

    [Fact]
    public void AbilityCosts_MatchSpecification()
    {
        var powerStrike = _abilities.GetAbility(AbilityType.PowerStrike);
        var defensiveStance = _abilities.GetAbility(AbilityType.DefensiveStance);
        var poisonDart = _abilities.GetAbility(AbilityType.PoisonDart);
        var secondWind = _abilities.GetAbility(AbilityType.SecondWind);

        Assert.Equal(10, powerStrike?.ManaCost);
        Assert.Equal(8, defensiveStance?.ManaCost);
        Assert.Equal(12, poisonDart?.ManaCost);
        Assert.Equal(15, secondWind?.ManaCost);
    }

    [Fact]
    public void AbilityCooldowns_MatchSpecification()
    {
        var powerStrike = _abilities.GetAbility(AbilityType.PowerStrike);
        var defensiveStance = _abilities.GetAbility(AbilityType.DefensiveStance);
        var poisonDart = _abilities.GetAbility(AbilityType.PoisonDart);
        var secondWind = _abilities.GetAbility(AbilityType.SecondWind);

        Assert.Equal(2, powerStrike?.CooldownTurns);
        Assert.Equal(3, defensiveStance?.CooldownTurns);
        Assert.Equal(4, poisonDart?.CooldownTurns);
        Assert.Equal(5, secondWind?.CooldownTurns);
    }

    [Fact]
    public void AbilityUnlockLevels_MatchSpecification()
    {
        var powerStrike = _abilities.GetAbility(AbilityType.PowerStrike);
        var defensiveStance = _abilities.GetAbility(AbilityType.DefensiveStance);
        var poisonDart = _abilities.GetAbility(AbilityType.PoisonDart);
        var secondWind = _abilities.GetAbility(AbilityType.SecondWind);

        Assert.Equal(1, powerStrike?.UnlockLevel);
        Assert.Equal(3, defensiveStance?.UnlockLevel);
        Assert.Equal(5, poisonDart?.UnlockLevel);
        Assert.Equal(7, secondWind?.UnlockLevel);
    }
}
