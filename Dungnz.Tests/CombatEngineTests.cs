using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class CombatEngineTests
{
    private static (Player player, Enemy enemy, FakeDisplayService display) MakeBasic(
        int playerHp = 100, int playerAtk = 10, int playerDef = 5,
        int enemyHp = 1, int enemyAtk = 8, int enemyDef = 2,
        int enemyXp = 15)
    {
        var player = new Player { HP = playerHp, MaxHP = playerHp, Attack = playerAtk, Defense = playerDef };
        var enemy = new Enemy_Stub(enemyHp, enemyAtk, enemyDef, enemyXp);
        var display = new FakeDisplayService();
        return (player, enemy, display);
    }

    [Fact]
    public void PlayerAttacks_EnemyDies_ReturnsWon()
    {
        var (player, enemy, display) = MakeBasic(enemyHp: 1);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
    }

    [Fact]
    public void EnemyAttacksBack_BeforeDying()
    {
        // Enemy HP=10, player does 8 dmg (10-2), enemy survives
        // Then enemy does 3 dmg (8-5), player takes damage
        // Then player finishes off enemy
        var (player, enemy, display) = MakeBasic(enemyHp: 10);
        var input = new FakeInputReader("A", "A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        player.HP.Should().BeLessThan(100); // took some damage
    }

    [Fact]
    public void PlayerDies_ReturnsPlayerDied()
    {
        // Player with tiny HP vs very high attack enemy
        var (player, enemy, display) = MakeBasic(playerHp: 5, playerDef: 0, enemyHp: 9999, enemyAtk: 100, enemyDef: 0);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.PlayerDied);
    }

    [Fact]
    public void FleeSucceeds_ReturnsFlcd()
    {
        var (player, enemy, display) = MakeBasic();
        var input = new FakeInputReader("F");
        // NextDouble() = 0.1 < 0.5 → flee succeeds
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.1));

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Fled);
    }

    [Fact]
    public void FleeFails_PlayerTakesDamage_CombatContinues()
    {
        var (player, enemy, display) = MakeBasic(enemyHp: 1);
        var input = new FakeInputReader("F", "A");
        // First NextDouble() = 0.9 >= 0.5 → flee fails; second call for loot (doesn't matter)
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        var result = engine.RunCombat(player, enemy);

        // Eventually wins after flee fails and then attacks
        result.Should().Be(CombatResult.Won);
        player.HP.Should().BeLessThan(100); // took damage from failed flee
    }

    [Fact]
    public void FleeFails_PlayerDies_ReturnsPlayerDied()
    {
        // Player very low HP, flee fails, enemy kill blow
        var (player, enemy, display) = MakeBasic(playerHp: 1, playerDef: 0, enemyHp: 9999, enemyAtk: 100);
        var input = new FakeInputReader("F");
        // NextDouble() = 0.9 → flee fails → enemy hits for 100 → player dies
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.9));

        var result = engine.RunCombat(player, enemy);

        result.Should().Be(CombatResult.PlayerDied);
    }

    [Fact]
    public void XpAwardedOnWin()
    {
        var (player, enemy, display) = MakeBasic(enemyXp: 30);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.XP.Should().Be(30);
    }

    [Fact]
    public void GoldAwardedOnWin()
    {
        var (player, enemy, display) = MakeBasic();
        // Use a loot table with fixed gold
        ((Enemy_Stub)enemy).LootTable = new LootTable(minGold: 10, maxGold: 10);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.Gold.Should().Be(10);
    }

    [Fact]
    public void ItemAwardedOnWin_WhenLootTableDropsIt()
    {
        var (player, enemy, display) = MakeBasic();
        var sword = new Item { Name = "Test Sword", Type = ItemType.Weapon };
        ((Enemy_Stub)enemy).LootTable = new LootTable(new Random(0), 0, 0);
        ((Enemy_Stub)enemy).LootTable.AddDrop(sword, 1.0);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.Inventory.Should().Contain(sword);
    }

    [Fact]
    public void LevelUp_Triggered_WhenXpCrossesThreshold()
    {
        var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, XP = 90, Level = 1 };
        var enemy = new Enemy_Stub(hp: 1, atk: 8, def: 2, xp: 15);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "1"); // "A" = attack, "1" = trait: +5 MaxHP
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        player.Level.Should().Be(2);
        player.Attack.Should().Be(12); // +2
        player.Defense.Should().Be(6); // +1
        player.MaxHP.Should().Be(115); // +10 level-up + 5 trait bonus
        player.HP.Should().Be(115);    // restored
    }

    [Fact]
    public void DamageFormula_IsMaxOneAttackerMinusDefender()
    {
        // Player Attack=10, Enemy Defense=9 → damage = Max(1, 10-9) = 1
        var (player, enemy, display) = MakeBasic(playerAtk: 10, enemyDef: 9, enemyHp: 1);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("1 damage"));
    }

    [Fact]
    public void MinimumOneDamageAlwaysDealt()
    {
        // Player Attack=1, Enemy Defense=100 → Max(1, 1-100) = 1
        var (player, enemy, display) = MakeBasic(playerAtk: 1, enemyDef: 100, enemyHp: 1);
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("1 damage"));
    }
    // ── Bug #85 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Bug85_ClassDodgeBonus_IncludedInPlayerDodgeRoll()
    {
        var (player, enemy, display) = MakeBasic(playerDef: 0, enemyHp: 999, enemyAtk: 50);
        player.ClassDodgeBonus = 0.99f;
        var input = new FakeInputReader("A", "F");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.01));
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("dodge"));
        player.HP.Should().Be(100);
    }

    [Fact]
    public void Bug85_EquipmentDodgeBonus_IncludedInPlayerDodgeRoll()
    {
        var (player, enemy, display) = MakeBasic(playerDef: 0, enemyHp: 999, enemyAtk: 50);
        player.DodgeBonus = 0.99f;
        var input = new FakeInputReader("A", "F");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.01));
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("dodge"));
        player.HP.Should().Be(100);
    }

    // ── Bug #86 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Bug86_PowerStrikeSkill_IncreasesPlayerDamageBy15Percent()
    {
        // Attack=10, EnemyDef=0 -> base=10; x1.15 -> (int)11.5 = 11
        var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Level = 3 };
        player.Skills.TryUnlock(player, Skill.PowerStrike);
        var enemy = new Enemy_Stub(hp: 1, atk: 0, def: 0, xp: 1);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.95));
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("11 damage"));
    }

    [Fact]
    public void Bug86_BattleHardenedSkill_ReducesIncomingDamageBy5Percent()
    {
        // Enemy attack=10, player def=0 -> normally 10; BattleHardened 5% -> (int)(10*0.95)=9
        var player = new Player { HP = 100, MaxHP = 100, Attack = 1, Defense = 0, Level = 6 };
        player.Skills.TryUnlock(player, Skill.BattleHardened);
        var enemy = new Enemy_Stub(hp: 999, atk: 10, def: 0, xp: 1);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "F");
        var rng = new ControlledRandom(defaultDouble: 0.1, 0.95, 0.95, 0.95, 0.95);
        var engine = new CombatEngine(display, input, rng);
        engine.RunCombat(player, enemy);
        player.HP.Should().Be(91); // 100 - (10-1)
    }

    [Fact]
    public void Bug86_SwiftnessSkill_AddsToPlayerDodgeChance()
    {
        // Player def=0 -> base dodge=0; Swiftness adds 0.05; RNG=0.03 < 0.05 -> dodge
        var player = new Player { HP = 100, MaxHP = 100, Attack = 1, Defense = 0, Level = 5 };
        player.Skills.TryUnlock(player, Skill.Swiftness);
        var enemy = new Enemy_Stub(hp: 999, atk: 50, def: 0, xp: 1);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "F");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.03));
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("dodge"));
        player.HP.Should().Be(100);
    }

    // ── Bug #197 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Bug197_Fortified_AppliesFiftyPercentDefBonus()
    {
        // Player Defense=4, Fortified adds +50% DEF = +2, effective DEF=6
        // Enemy atk=10 → damage = Max(1, 10-6) = 4 → player HP = 96
        var player = new Player { HP = 100, MaxHP = 100, Attack = 1, Defense = 4 };
        var enemy = new Enemy_Stub(hp: 999, atk: 10, def: 0, xp: 1);
        var display = new FakeDisplayService();
        var statusEffects = new StatusEffectManager(display);
        statusEffects.Apply(player, StatusEffect.Fortified, 5);
        var input = new FakeInputReader("A", "F");
        var rng = new ControlledRandom(defaultDouble: 0.1, 0.95, 0.95, 0.95, 0.95);
        var engine = new CombatEngine(display, input, rng, statusEffects: statusEffects);
        engine.RunCombat(player, enemy);
        player.HP.Should().Be(96); // 100 - (10 - (4 + 4/2)) = 100 - 4
    }

    // ── Bug #107 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Bug107_BossChargeActive_ClearedOnPlayerDodge()
    {
        // IsCharging=true -> PerformEnemyTurn sets ChargeActive=true and proceeds.
        // ClassDodgeBonus=0.99 -> player dodges the charged hit. ChargeActive must be false after.
        var player = new Player { HP = 100, MaxHP = 100, Attack = 1, Defense = 0, ClassDodgeBonus = 0.99f };
        var boss = new DungeonBoss(null, null);
        boss.HP = boss.MaxHP = 999;
        boss.Attack = 50;
        boss.IsCharging = true;
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "F");
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.01));
        engine.RunCombat(player, boss);
        boss.ChargeActive.Should().BeFalse();
    }

    // ── Bug #110 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Bug110_BleedOnHit_MessageShown_WhenWeaponHasBleed()
    {
        var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var bleedSword = new Item { Name = "Bleed Sword", Type = ItemType.Weapon, IsEquippable = true, AppliesBleedOnHit = true };
        player.Inventory.Add(bleedSword);
        player.EquipItem(bleedSword);
        var enemy = new Enemy_Stub(hp: 999, atk: 0, def: 0, xp: 1);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "F");
        // Queue: [0.95(enemy no-dodge), 0.95(no crit), 0.05(bleed proc)]
        // default=0.1: player dodge (0.1 < 5/25=0.2), flee success
        var rng = new ControlledRandom(defaultDouble: 0.1, 0.95, 0.95, 0.05);
        var engine = new CombatEngine(display, input, rng);
        engine.RunCombat(player, enemy);
        display.CombatMessages.Should().Contain(m => m.Contains("bleeding"));
    }

    // ── Bug #111 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Bug111_AbilityDamage_TrackedInRunStats()
    {
        // PowerStrike: Max(1, 10*2 - 0) = 20
        var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Level = 1, Mana = 30, MaxMana = 30 };
        var enemy = new Enemy_Stub(hp: 999, atk: 0, def: 0, xp: 1);
        var display = new FakeDisplayService();
        var stats = new RunStats();
        var input = new FakeInputReader("B", "1", "F");
        var rng = new ControlledRandom(defaultDouble: 0.1);
        var engine = new CombatEngine(display, input, rng);
        engine.RunCombat(player, enemy, stats);
        stats.DamageDealt.Should().Be(20);
    }

}

/// <summary>Test stub enemy with configurable stats.</summary>
internal class Enemy_Stub : Enemy
{
    public Enemy_Stub(int hp, int atk, int def, int xp)
    {
        Name = "TestEnemy";
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = xp;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f; // deterministic: never dodges in tests
    }
}
