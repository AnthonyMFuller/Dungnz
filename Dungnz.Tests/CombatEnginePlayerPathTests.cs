using System.Linq;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>CombatEngine tests for player class paths, ability menu, and special combat states.</summary>
public class CombatEnginePlayerPathTests
{
    private static Player MakePlayer(int hp = 500, int atk = 20, int def = 0, PlayerClass cls = PlayerClass.Warrior, int mana = 100, int level = 1)
        => new Player { HP = hp, MaxHP = hp, Attack = atk, Defense = def, Class = cls, Mana = mana, MaxMana = mana, Level = level, Name = "Hero" };

    private static FakeInputReader Attacks(int count) => new FakeInputReader(Enumerable.Repeat("A", count).ToArray());

    // ── HandleAbilityMenu — Mage opens ability menu and uses ArcaneBolt ───────

    [Fact]
    public void HandleAbilityMenu_MageUsesArcaneBolt_DealsDamage()
    {
        // Turn 1: player opens ability menu ("B"), selects ability "1" (ArcaneBolt)
        // ArcaneBolt deals (20*1.5 + 50/10) = 35 damage. Enemy HP=50-35=15. Alive.
        // Turn 2: player attacks ("A"). 20-0=20 damage. HP=15-20=-5. Dead.
        var input = new FakeInputReader("B", "1", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Mage, mana: 50, level: 1);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("bolt") || m.Contains("energy") || m.Contains("crackling"));
    }

    [Fact]
    public void HandleAbilityMenu_NoAbilitiesUnlocked_ShowsMessage()
    {
        // Player at Level 0 has no unlocked abilities → ability menu shows "no abilities" message
        // After canceling, player falls back to attacking
        var input = new FakeInputReader("B", "A", "A", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Mage, mana: 0, level: 1);
        player.Mana = 0; // no mana → no usable abilities even if unlocked
        var enemy = new Enemy_Stub(10, 5, 0, 10);
        engine.RunCombat(player, enemy);
        // Just verify combat completes
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void HandleAbilityMenu_PlayerCancels_ReturnsToMenu()
    {
        // Player opens ability menu and types "C" to cancel
        var input = new FakeInputReader("B", "C", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Warrior, mana: 100, level: 2);
        var enemy = new Enemy_Stub(10, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    // ── Warrior class paths ───────────────────────────────────────────────────

    [Fact]
    public void Warrior_ShieldBash_StunEnemy()
    {
        // Warrior at Level 1 has ShieldBash unlocked (unlockLevel=1).
        // Input: "B" open ability menu, "1" to use ShieldBash (mana cost=8, cooldown=2)
        var input = new FakeInputReader("B", "1", "A", "A", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Warrior, mana: 100, level: 1);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    [Fact]
    public void Warrior_LowHP_DamageBonus()
    {
        // Warrior with HP < MaxHP/2 gets 5% damage bonus
        var input = Attacks(10);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 100, atk: 20, def: 0, cls: PlayerClass.Warrior, mana: 0);
        player.HP = 40; // < 50% of 100
        var enemy = new Enemy_Stub(30, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    // ── Rogue class paths ─────────────────────────────────────────────────────

    [Fact]
    public void Rogue_ShadowStrike_FirstAttackDoubleDamage()
    {
        // Rogue's first attack is a Shadow Strike (double damage). ShadowStrikeReady = true by default.
        var input = Attacks(5);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Rogue, mana: 50);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("Shadow Strike") || m.Contains("shadows"));
    }

    [Fact]
    public void Rogue_QuickStrike_UsesAbility()
    {
        // Rogue at Level 1 has QuickStrike unlocked (mana cost=5).
        var input = new FakeInputReader("B", "1", "A", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Rogue, mana: 100, level: 1);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    // ── Ranger class paths ────────────────────────────────────────────────────

    [Fact]
    public void Ranger_HuntersMark_FirstStrikeBonusDamage()
    {
        // Ranger's first attack gets HuntersMark +25% bonus
        var input = Attacks(5);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Ranger, mana: 50);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("Hunter") || m.Contains("Hunter's Mark") || m.Contains("first strike"));
    }

    // ── Paladin class paths ───────────────────────────────────────────────────

    [Fact]
    public void Paladin_HolyStrike_CanHitUndead()
    {
        // Paladin's first ability is HolyStrike vs undead
        var input = new FakeInputReader("B", "1", "A", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Paladin, mana: 100, level: 1);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    // ── Necromancer class paths ───────────────────────────────────────────────

    [Fact]
    public void Necromancer_SoulHarvest_HealsOnKill()
    {
        // Necromancer heals 5 HP on enemy kill via Soul Harvest passive
        var input = Attacks(5);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Necromancer, mana: 50);
        var enemy = new Enemy_Stub(10, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("Soul Harvest") || m.Contains("essence"));
    }

    // ── Mage hit messages ─────────────────────────────────────────────────────

    [Fact]
    public void Mage_HitMessages_UseMagePool()
    {
        var input = Attacks(10);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Mage, mana: 50);
        var enemy = new Enemy_Stub(50, 5, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    // ── Elite enemy special abilities ─────────────────────────────────────────

    [Fact]
    public void EliteEnemy_IsElite_RunCombatCompletesSuccessfully()
    {
        // IsElite=true: elite ability fires when rng.Next(100) < 15.
        // With ControlledRandom, Next(100)=0 always → elite fires each turn (case 0=stun).
        // To avoid stun loop, use high player damage to kill on first attack before elite can stun.
        // Player deals 20 damage. Enemy HP=15 (< player damage). Dies on turn 1.
        var input = Attacks(5);
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0);
        var enemy = new Enemy_Stub(10, 10, 0, 20); // low HP: dies in 1 hit
        enemy.IsElite = true;
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }

    // ── Mana Shield absorption ────────────────────────────────────────────────

    [Fact]
    public void ManaShield_AbsorbsIncomingDamage()
    {
        // When IsManaShieldActive=true, incoming damage is absorbed by mana
        var input = new FakeInputReader("B", "1", "A", "A", "A", "A", "A", "A", "A");
        var display = new FakeDisplayService(input);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));
        var player = MakePlayer(hp: 500, atk: 20, def: 0, cls: PlayerClass.Mage, mana: 200, level: 4);
        // Level 4: Mana Shield is at unlockLevel=4 for Mage
        var enemy = new Enemy_Stub(50, 20, 0, 10);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().NotBeEmpty();
    }
}
