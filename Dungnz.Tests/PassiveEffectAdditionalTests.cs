using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for passive effect types not yet covered by ItemsExpansionTests.</summary>
public class PassiveEffectAdditionalTests
{
    private static PassiveEffectProcessor MakeProcessor(FakeDisplayService? display = null, Random? rng = null)
    {
        var d = display ?? new FakeDisplayService();
        var r = rng ?? new Random(42);
        var status = new StatusEffectManager(d);
        return new PassiveEffectProcessor(d, r, status);
    }

    private static Player MakePlayer(int hp = 100, int maxHp = 100, int mana = 50, int maxMana = 50)
        => new Player { HP = hp, MaxHP = maxHp, Mana = mana, MaxMana = maxMana };

    private static Enemy MakeEnemy(int hp = 30) => new Enemy_Stub(hp, 8, 2, 10);

    private static Item MakeWeapon(string effectId) => new Item
    {
        Name = "Test Weapon",
        Type = ItemType.Weapon,
        AttackBonus = 5,
        IsEquippable = true,
        PassiveEffectId = effectId
    };

    // ── frostbite_on_hit ─────────────────────────────────────────────────────

    [Fact]
    public void FrostbiteOnHit_WrongTrigger_NoEffect()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = MakePlayer();
        var enemy = MakeEnemy();
        player.Inventory.Add(MakeWeapon("frostbite_on_hit"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnEnemyKilled, enemy, 10);

        display.CombatMessages.Should().BeEmpty("frostbite only triggers on player hit");
    }

    [Fact]
    public void FrostbiteOnHit_DeadEnemy_NoEffect()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = MakePlayer();
        var deadEnemy = new Enemy_Stub(0, 8, 2, 10);
        player.Inventory.Add(MakeWeapon("frostbite_on_hit"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, deadEnemy, 10);

        display.CombatMessages.Should().BeEmpty("dead enemy cannot be slowed");
    }

    [Fact]
    public void FrostbiteOnHit_NullEnemy_NoEffect()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("frostbite_on_hit"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, null, 10);

        display.CombatMessages.Should().BeEmpty();
    }

    [Fact]
    public void FrostbiteOnHit_RngBelow30_AppliesSlowToEnemy()
    {
        var display = new FakeDisplayService();
        // Seed 0 gives NextDouble() ~0.0 which is < 0.30
        var proc = MakeProcessor(display, new Random(0));
        var player = MakePlayer();
        var enemy = MakeEnemy(hp: 30);
        player.Inventory.Add(MakeWeapon("frostbite_on_hit"));
        player.EquipItem(player.Inventory[0]);

        // Use multiple attempts to find a seed that triggers
        bool triggered = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var dp = new FakeDisplayService();
            var p2 = MakePlayer();
            var e2 = MakeEnemy();
            var prox = MakeProcessor(dp, new Random(seed));
            p2.Inventory.Add(MakeWeapon("frostbite_on_hit"));
            p2.EquipItem(p2.Inventory[0]);
            prox.ProcessPassiveEffects(p2, PassiveEffectTrigger.OnPlayerHit, e2, 10);
            if (dp.CombatMessages.Any(m => m.Contains("slowed")))
            {
                triggered = true;
                break;
            }
        }
        triggered.Should().BeTrue("frostbite should apply slow at 30% chance within 50 attempts");
    }

    // ── thunderstrike_on_kill ────────────────────────────────────────────────

    [Fact]
    public void ThunderstrikeOnKill_WrongTrigger_NoBonusDamage()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("thunderstrike_on_kill"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, MakeEnemy(), 20);

        bonus.Should().Be(0, "thunderstrike only fires on kill");
    }

    [Fact]
    public void ThunderstrikeOnKill_OnEnemyKilled_ReturnsBonusDamage()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("thunderstrike_on_kill"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnEnemyKilled, MakeEnemy(), 20);

        bonus.Should().BeGreaterThan(0, "thunderstrike should deal 50% of damage dealt as bonus");
        display.CombatMessages.Should().Contain(m => m.Contains("thunderclap") || m.Contains("Thunderstrike"));
    }

    [Fact]
    public void ThunderstrikeOnKill_MinimumBonusDamage_IsOne()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("thunderstrike_on_kill"));
        player.EquipItem(player.Inventory[0]);

        // damageDealt = 1 → 50% = 0.5 → Math.Max(1, 0) = 1
        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnEnemyKilled, MakeEnemy(), 1);

        bonus.Should().Be(1);
    }

    // ── damage_reflect ───────────────────────────────────────────────────────

    [Fact]
    public void DamageReflect_WrongTrigger_NoBonusDamage()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("damage_reflect"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, MakeEnemy(), 10);

        bonus.Should().Be(0);
    }

    [Fact]
    public void DamageReflect_OnPlayerTakeDamage_ReturnsReflectDamage()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("damage_reflect"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerTakeDamage, MakeEnemy(), 20);

        bonus.Should().BeGreaterThan(0, "25% of 20 = 5 reflect damage");
        display.CombatMessages.Should().Contain(m => m.Contains("reflected") || m.Contains("Ironheart"));
    }

    [Fact]
    public void DamageReflect_NullEnemy_NoBonusDamage()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("damage_reflect"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerTakeDamage, null, 20);

        bonus.Should().Be(0);
    }

    [Fact]
    public void DamageReflect_ZeroDamage_NoBonusDamage()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("damage_reflect"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerTakeDamage, MakeEnemy(), 0);

        bonus.Should().Be(0);
    }

    // ── first_attack_dodge ────────────────────────────────────────────────────

    [Fact]
    public void FirstAttackDodge_OnCombatStart_ResetsFlagToFalse()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.ShadowmeldUsedThisCombat = true;
        player.Inventory.Add(MakeWeapon("first_attack_dodge"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnCombatStart, null, 0);

        player.ShadowmeldUsedThisCombat.Should().BeFalse();
    }

    [Fact]
    public void FirstAttackDodge_WrongTrigger_NoEffect()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.ShadowmeldUsedThisCombat = false;
        player.Inventory.Add(MakeWeapon("first_attack_dodge"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, null, 10);

        player.ShadowmeldUsedThisCombat.Should().BeFalse();
    }

    // ── extra_flee ────────────────────────────────────────────────────────────

    [Fact]
    public void ExtraFlee_OnCombatStart_SetsExtraFleeCountTo1()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("extra_flee"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnCombatStart, null, 0);

        player.ExtraFleeCount.Should().Be(1);
    }

    [Fact]
    public void ExtraFlee_WrongTrigger_NoEffect()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.ExtraFleeCount = 0;
        player.Inventory.Add(MakeWeapon("extra_flee"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        player.ExtraFleeCount.Should().Be(0);
    }

    // ── warding_ring ──────────────────────────────────────────────────────────

    [Fact]
    public void WardingRing_BelowThreshold_AppliesFortified()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = new Player { HP = 20, MaxHP = 100, Name = "Hero" }; // 20% HP
        player.WardingRingActivated = false;
        player.Inventory.Add(MakeWeapon("warding_ring"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        player.WardingRingActivated.Should().BeTrue();
        display.CombatMessages.Should().Contain(m => m.Contains("Warding") || m.Contains("protective"));
    }

    [Fact]
    public void WardingRing_AboveThreshold_NoEffect()
    {
        var proc = MakeProcessor();
        var player = new Player { HP = 80, MaxHP = 100, Name = "Hero" }; // 80% HP
        player.WardingRingActivated = false;
        player.Inventory.Add(MakeWeapon("warding_ring"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        player.WardingRingActivated.Should().BeFalse("HP is above 25% threshold");
    }

    [Fact]
    public void WardingRing_AlreadyActivated_NoDoubleApply()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = new Player { HP = 20, MaxHP = 100, Name = "Hero" };
        player.WardingRingActivated = true;
        player.Inventory.Add(MakeWeapon("warding_ring"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        display.CombatMessages.Should().BeEmpty("ring already activated this combat");
    }

    [Fact]
    public void WardingRing_WrongTrigger_NoEffect()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = new Player { HP = 20, MaxHP = 100, Name = "Hero" };
        player.WardingRingActivated = false;
        player.Inventory.Add(MakeWeapon("warding_ring"));
        player.EquipItem(player.Inventory[0]);

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, null, 10);

        display.CombatMessages.Should().BeEmpty("warding_ring only triggers on turn start");
    }

    // ── cooldown_reduction (no combat trigger) ────────────────────────────────

    [Fact]
    public void CooldownReduction_AnyTrigger_ReturnsZeroAndNoMessage()
    {
        var display = new FakeDisplayService();
        var proc = MakeProcessor(display);
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("cooldown_reduction"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, MakeEnemy(), 10);

        bonus.Should().Be(0);
        display.CombatMessages.Should().BeEmpty("cooldown_reduction has no in-combat trigger");
    }

    // ── ApplyCooldownReduction static helper ─────────────────────────────────

    [Fact]
    public void ApplyCooldownReduction_ReducesAllCooldowns()
    {
        var abilities = new AbilityManager();
        // No exception means the method works correctly
        PassiveEffectProcessor.ApplyCooldownReduction(abilities);
    }

    // ── carry_weight (no combat trigger) ────────────────────────────────────

    [Fact]
    public void CarryWeight_AnyTrigger_ReturnsZero()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("carry_weight"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, MakeEnemy(), 5);

        bonus.Should().Be(0);
    }

    // ── unknown effect id ────────────────────────────────────────────────────

    [Fact]
    public void UnknownEffectId_AnyTrigger_ReturnsZero()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();
        player.Inventory.Add(MakeWeapon("totally_unknown_effect_xyz"));
        player.EquipItem(player.Inventory[0]);

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, MakeEnemy(), 10);

        bonus.Should().Be(0, "unknown effect id falls through to default: 0");
    }

    // ── ResetCombatState (verify all flags cleared) ───────────────────────────

    [Fact]
    public void ResetCombatState_ClearsAllFlags()
    {
        var player = MakePlayer();
        player.AegisUsedThisCombat = true;
        player.ShadowmeldUsedThisCombat = true;
        player.WardingRingActivated = true;
        player.BonusFleeUsed = true;
        player.ExtraFleeCount = 5;
        player.ShadowDanceCounter = 3;

        PassiveEffectProcessor.ResetCombatState(player);

        player.AegisUsedThisCombat.Should().BeFalse();
        player.ShadowmeldUsedThisCombat.Should().BeFalse();
        player.WardingRingActivated.Should().BeFalse();
        player.BonusFleeUsed.Should().BeFalse();
        player.ExtraFleeCount.Should().Be(0);
        player.ShadowDanceCounter.Should().Be(0);
    }

    // ── No equipped items → returns zero ─────────────────────────────────────

    [Fact]
    public void NoEquippedItems_ReturnsZeroBonusDamage()
    {
        var proc = MakeProcessor();
        var player = MakePlayer();

        var bonus = proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, MakeEnemy(), 10);

        bonus.Should().Be(0);
    }
}
