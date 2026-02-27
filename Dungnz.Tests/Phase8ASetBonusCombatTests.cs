using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Phase 8-A2: Tests verifying that 4-piece set bonus flags are read and applied during combat.
/// </summary>
public class Phase8ASetBonusCombatTests
{
    // Test 1: DamageReflectPercent (Ironclad 4-piece)
    [Fact]
    public void DamageReflect_PlayerTakes100Damage_EnemyLoses10HpFromReflect()
    {
        // Enemy Attack=100, Player Defense=0: enemy deals 100 per hit.
        // DamageReflectPercent=0.1 -> int(Round(100*0.1))=10 HP reflected each hit.
        // Enemy HP=20: round 1 player deals 1->HP=19, reflect 10->HP=9.
        // Round 2: player deals 1->HP=8, reflect 10->HP=-2 -> enemy dies.
        var player = new Player
        {
            HP = 1000, MaxHP = 1000, Attack = 1, Defense = 0,
            DamageReflectPercent = 0.1f, Mana = 0, MaxMana = 100
        };
        var enemy = new Enemy_Stub(20, 100, 0, 10);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "A", "A");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.5));

        engine.RunCombat(player, enemy);

        // Reflect message appears in combat messages ("[Ironclad] Reflected X damage!")
        display.CombatMessages.Should().Contain(m => m.Contains("Reflected") && m.Contains("damage"));
    }

    // Test 2: SetBonusAppliesBleed (Shadowstep 4-piece)
    [Fact]
    public void SetBonusAppliesBleed_PlayerAttacksEnemy_BleedApplied()
    {
        // SetBonusAppliesBleed guarantees Bleed on every hit when enemy.HP > 0.
        // Enemy HP=5, player Attack=1: enemy dies in ~5 rounds; bleed message on round 1.
        var player = new Player
        {
            HP = 1000, MaxHP = 1000, Attack = 1, Defense = 0,
            SetBonusAppliesBleed = true, Mana = 0, MaxMana = 100
        };
        var enemy = new Enemy_Stub(5, 1, 0, 10);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "A", "A", "A", "A", "A");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.5));

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("is bleeding!"));
    }

    // Test 3: ManaDiscount (Arcane Ascendant 4-piece)
    [Fact]
    public void ManaDiscount_AbilityCosts8Mana_OnlyDeducts7WithDiscount1()
    {
        // ShieldBash normally costs 8 mana. With ManaDiscount=1, effective cost = 7.
        var display = new TestDisplayService();
        var statusEffects = new StatusEffectManager(display);
        var abilities = new AbilityManager();
        var player = new Player
        {
            Name = "Hero", Class = PlayerClass.Warrior,
            Mana = 100, MaxMana = 100, ManaDiscount = 1
        };
        var enemy = new Enemy_Stub(100, 5, 0, 10);

        abilities.UseAbility(player, enemy, AbilityType.ShieldBash, statusEffects, display);

        player.Mana.Should().Be(100 - 7); // 8 base cost - 1 discount = 7 deducted
    }

    // Test 4: IsStunImmune on Player (Sentinel 4-piece)
    [Fact]
    public void IsStunImmune_StunAppliedToPlayer_StunNotApplied()
    {
        // With IsStunImmune=true, StatusEffectManager must silently ignore Stun on the player.
        var display = new TestDisplayService();
        var mgr = new StatusEffectManager(display);
        var player = new Player { Name = "Hero", IsStunImmune = true };

        mgr.Apply(player, StatusEffect.Stun, 2);

        mgr.HasEffect(player, StatusEffect.Stun).Should().BeFalse();
    }
}
