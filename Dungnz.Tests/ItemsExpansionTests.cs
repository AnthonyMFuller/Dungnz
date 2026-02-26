namespace Dungnz.Tests;

using System;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

/// <summary>WI-D6: Tests for Legendaries, affixes, set bonuses, class restrictions, and passive effects.</summary>
public class ItemsExpansionTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Player MakePlayer(int hp = 100, int atk = 10, int def = 5)
        => new Player { HP = hp, MaxHP = hp, Attack = atk, Defense = def };

    private static Item MakeLegendaryWeapon(string passiveId, int atkBonus = 15)
        => new Item
        {
            Name = "Test Legendary",
            Type = ItemType.Weapon,
            Tier = ItemTier.Legendary,
            AttackBonus = atkBonus,
            IsEquippable = true,
            PassiveEffectId = passiveId
        };

    private static PassiveEffectProcessor MakeProcessor()
    {
        var display = new FakeDisplayService();
        var rng = new Random(42);
        var status = new StatusEffectManager(display);
        return new PassiveEffectProcessor(display, rng, status);
    }

    private static Enemy_Stub MakeEnemy(int hp = 30, int atk = 8, int def = 2)
        => new Enemy_Stub(hp, atk, def, 10);
    // ── D6: Vampiric Strike ──────────────────────────────────────────────────

    [Fact]
    public void VampiricStrike_OnPlayerHit_HealsPlayer20Percent()
    {
        var player = MakePlayer(hp: 50, atk: 10, def: 5);
        player.MaxHP = 100;
        player.HP = 50;
        var weapon = MakeLegendaryWeapon("vampiric_strike");
        player.Inventory.Add(weapon);
        player.EquipItem(weapon);

        var enemy = MakeEnemy();
        var proc = MakeProcessor();

        int damageDealt = 20;
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, enemy, damageDealt);

        var expectedHeal = (int)(damageDealt * 0.20); // 4
        player.HP.Should().Be(50 + expectedHeal);
    }

    [Fact]
    public void VampiricStrike_NoTriggerOnEnemyKill_DoesNotHeal()
    {
        var player = MakePlayer(hp: 50, atk: 10, def: 5);
        player.MaxHP = 100;
        var weapon = MakeLegendaryWeapon("vampiric_strike");
        player.Inventory.Add(weapon);
        player.EquipItem(weapon);

        var enemy = MakeEnemy();
        var proc = MakeProcessor();

        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnEnemyKilled, enemy, 20);

        player.HP.Should().Be(50); // no heal
    }

    // ── D6: Aegis survive-at-one ─────────────────────────────────────────────

    [Fact]
    public void AegisSurviveAtOne_FirstTrigger_SetsHpToOne()
    {
        var player = MakePlayer();
        var armor = new Item
        {
            Name = "Aegis of the Immortal",
            Type = ItemType.Armor,
            Tier = ItemTier.Legendary,
            IsEquippable = true,
            PassiveEffectId = "survive_at_one"
        };
        player.Inventory.Add(armor);
        player.EquipItem(armor);
        player.HP = 0; // simulate death

        var proc = MakeProcessor();
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, null, 0);

        player.HP.Should().Be(1);
        player.AegisUsedThisCombat.Should().BeTrue();
    }

    [Fact]
    public void AegisSurviveAtOne_SecondTriggerSameCombat_DoesNotFire()
    {
        var player = MakePlayer();
        var armor = new Item
        {
            Name = "Aegis of the Immortal",
            Type = ItemType.Armor,
            Tier = ItemTier.Legendary,
            IsEquippable = true,
            PassiveEffectId = "survive_at_one"
        };
        player.Inventory.Add(armor);
        player.EquipItem(armor);
        player.HP = 0;

        var proc = MakeProcessor();

        // First trigger — should fire
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, null, 0);
        player.HP.Should().Be(1);

        // Simulate "death" again
        player.HP = 0;
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, null, 0);

        // Should NOT fire again
        player.HP.Should().Be(0);
    }

    // ── D6: Phoenix revive ───────────────────────────────────────────────────

    [Fact]
    public void PhoenixRevive_FirstTrigger_RevivesAt30Percent()
    {
        var player = MakePlayer(hp: 0);
        player.MaxHP = 100;
        player.HP = 0;
        var amulet = new Item
        {
            Name = "Amulet of the Phoenix",
            Type = ItemType.Accessory,
            Tier = ItemTier.Legendary,
            IsEquippable = true,
            PassiveEffectId = "phoenix_revive"
        };
        player.Inventory.Add(amulet);
        player.EquipItem(amulet);

        var proc = MakeProcessor();
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, null, 0);

        var expected = (int)(100 * 0.30); // 30
        player.HP.Should().Be(expected);
        player.PhoenixUsedThisRun.Should().BeTrue();
    }

    [Fact]
    public void PhoenixRevive_SecondTrigger_DoesNotFireAgain()
    {
        var player = MakePlayer(hp: 0);
        player.MaxHP = 100;
        player.HP = 0;
        var amulet = new Item
        {
            Name = "Amulet of the Phoenix",
            Type = ItemType.Accessory,
            Tier = ItemTier.Legendary,
            IsEquippable = true,
            PassiveEffectId = "phoenix_revive"
        };
        player.Inventory.Add(amulet);
        player.EquipItem(amulet);

        var proc = MakeProcessor();

        // First death — should revive
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, null, 0);
        player.HP.Should().BeGreaterThan(0);

        // Second death in same run — should NOT revive
        player.HP = 0;
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, null, 0);
        player.HP.Should().Be(0);
    }

    [Fact]
    public void PhoenixRevive_ResetCombatState_DoesNotClearPhoenixFlag()
    {
        var player = MakePlayer();
        player.PhoenixUsedThisRun = true;

        PassiveEffectProcessor.ResetCombatState(player);

        // PhoenixUsedThisRun persists across combats — only resets between runs
        player.PhoenixUsedThisRun.Should().BeTrue();
    }

    // ── D6: Affix roll ───────────────────────────────────────────────────────

    [Fact]
    public void AffixRoll_CommonItem_NeverReceivesAffix()
    {
        // Load affixes from file
        AffixRegistry.Load("Data/item-affixes.json");

        var item = new Item { Name = "Iron Sword", Tier = ItemTier.Common, Type = ItemType.Weapon, IsEquippable = true };
        // Even with a seed that always rolls 0.0 (i.e., below 10%)
        var rng = new Random(0);
        for (int i = 0; i < 100; i++)
        {
            var testItem = item.Clone();
            AffixRegistry.ApplyRandomAffix(testItem, rng);
            testItem.Prefix.Should().BeNull("Common items must never receive a prefix");
            testItem.Suffix.Should().BeNull("Common items must never receive a suffix");
        }
    }

    [Fact]
    public void AffixRoll_UncommonItem_ApproximatelyTenPercentChance()
    {
        AffixRegistry.Load("Data/item-affixes.json");

        // Use seed 12345 for deterministic results
        var rng = new Random(12345);
        int prefixCount = 0;
        const int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var item = new Item { Name = "Steel Sword", Tier = ItemTier.Uncommon, Type = ItemType.Weapon, IsEquippable = true };
            AffixRegistry.ApplyRandomAffix(item, rng);
            if (item.Prefix != null) prefixCount++;
        }

        // Expect roughly 10% ± 5% (statistical tolerance)
        var rate = (double)prefixCount / trials;
        rate.Should().BeInRange(0.05, 0.18, "prefix roll rate should be ~10% for Uncommon items");
    }

    // ── D6: SetBonusManager ──────────────────────────────────────────────────

    [Fact]
    public void SetBonusManager_TwoPieces_ReturnsTwoPieceBonus()
    {
        var player = MakePlayer();

        var garb = new Item { Name = "Shadowstalker Garb", Type = ItemType.Armor, IsEquippable = true, Tier = ItemTier.Rare, DefenseBonus = 10, SetId = "shadowstalker" };
        var blades = new Item { Name = "Shadowstalker Blades", Type = ItemType.Weapon, IsEquippable = true, Tier = ItemTier.Rare, AttackBonus = 12, SetId = "shadowstalker" };

        player.Inventory.Add(garb);
        player.EquipItem(garb);
        player.Inventory.Add(blades);
        player.EquipItem(blades);

        var pieces = SetBonusManager.GetEquippedSetPieces(player, "shadowstalker");
        pieces.Should().Be(2);

        var bonuses = SetBonusManager.GetActiveBonuses(player);
        bonuses.Should().Contain(b => b.PiecesRequired == 2 && b.SetId == "shadowstalker");
    }

    [Fact]
    public void SetBonusManager_ThreePieces_ReturnsThreePieceBonus()
    {
        var player = MakePlayer();

        var hood = new Item { Name = "Shadowstalker Hood", Type = ItemType.Accessory, IsEquippable = true, Tier = ItemTier.Rare, DodgeBonus = 0.05f, SetId = "shadowstalker" };
        var garb = new Item { Name = "Shadowstalker Garb", Type = ItemType.Armor, IsEquippable = true, Tier = ItemTier.Rare, DefenseBonus = 10, SetId = "shadowstalker" };
        var blades = new Item { Name = "Shadowstalker Blades", Type = ItemType.Weapon, IsEquippable = true, Tier = ItemTier.Rare, AttackBonus = 12, SetId = "shadowstalker" };

        player.Inventory.Add(hood);
        player.EquipItem(hood);
        player.Inventory.Add(garb);
        player.EquipItem(garb);
        player.Inventory.Add(blades);
        player.EquipItem(blades);

        var pieces = SetBonusManager.GetEquippedSetPieces(player, "shadowstalker");
        pieces.Should().Be(3);

        var bonuses = SetBonusManager.GetActiveBonuses(player);
        bonuses.Should().Contain(b => b.PiecesRequired == 2);
        bonuses.Should().Contain(b => b.PiecesRequired == 3 && b.GrantsShadowDance);
    }

    [Fact]
    public void SetBonusManager_OnePiece_NoBonuses()
    {
        var player = MakePlayer();

        var garb = new Item { Name = "Shadowstalker Garb", Type = ItemType.Armor, IsEquippable = true, Tier = ItemTier.Rare, DefenseBonus = 10, SetId = "shadowstalker" };
        player.Inventory.Add(garb);
        player.EquipItem(garb);

        var bonuses = SetBonusManager.GetActiveBonuses(player);
        bonuses.Should().BeEmpty();
    }

    // ── D6: ClassRestriction ─────────────────────────────────────────────────

    [Fact]
    public void ClassRestriction_WrongClass_EquipFails()
    {
        var player = MakePlayer();
        player.Class = PlayerClass.Mage;

        var sword = new Item
        {
            Name = "Battlemaster's Cleaver",
            Type = ItemType.Weapon,
            IsEquippable = true,
            Tier = ItemTier.Rare,
            AttackBonus = 14,
            ClassRestriction = new[] { "Warrior" }
        };
        player.Inventory.Add(sword);

        var display = new FakeDisplayService();
        var equipment = new EquipmentManager(display);
        equipment.HandleEquip(player, "Battlemaster's Cleaver");

        player.EquippedWeapon.Should().BeNull("Mage should not be able to equip Warrior-only item");
        display.Errors.Should().ContainMatch("*Only*Warrior*");
    }

    [Fact]
    public void ClassRestriction_CorrectClass_EquipSucceeds()
    {
        var player = MakePlayer();
        player.Class = PlayerClass.Warrior;

        var sword = new Item
        {
            Name = "Battlemaster's Cleaver",
            Type = ItemType.Weapon,
            IsEquippable = true,
            Tier = ItemTier.Rare,
            AttackBonus = 14,
            ClassRestriction = new[] { "Warrior" }
        };
        player.Inventory.Add(sword);

        var display = new FakeDisplayService();
        var equipment = new EquipmentManager(display);
        equipment.HandleEquip(player, "Battlemaster's Cleaver");

        player.EquippedWeapon.Should().Be(sword);
    }

    [Fact]
    public void ClassRestriction_NoRestriction_AllClassesCanEquip()
    {
        var player = MakePlayer();
        player.Class = PlayerClass.Rogue;

        var sword = new Item
        {
            Name = "Iron Sword",
            Type = ItemType.Weapon,
            IsEquippable = true,
            Tier = ItemTier.Common,
            AttackBonus = 5,
            ClassRestriction = null
        };
        player.Inventory.Add(sword);

        var display = new FakeDisplayService();
        var equipment = new EquipmentManager(display);
        equipment.HandleEquip(player, "Iron Sword");

        player.EquippedWeapon.Should().Be(sword);
    }

    // ── D6: Belt of Regeneration ─────────────────────────────────────────────

    [Fact]
    public void BeltRegen_OnTurnStart_Heals3HpPerTurn()
    {
        var player = MakePlayer();
        player.HP = 50;
        player.MaxHP = 100;

        var belt = new Item
        {
            Name = "Belt of Regeneration",
            Type = ItemType.Accessory,
            Tier = ItemTier.Rare,
            IsEquippable = true,
            PassiveEffectId = "belt_regen"
        };
        player.Inventory.Add(belt);
        player.EquipItem(belt);

        var proc = MakeProcessor();
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        player.HP.Should().Be(53);
    }

    [Fact]
    public void BeltRegen_DoesNotOverheal()
    {
        var player = MakePlayer();
        player.HP = player.MaxHP; // full health

        var belt = new Item
        {
            Name = "Belt of Regeneration",
            Type = ItemType.Accessory,
            Tier = ItemTier.Rare,
            IsEquippable = true,
            PassiveEffectId = "belt_regen"
        };
        player.Inventory.Add(belt);
        player.EquipItem(belt);

        var proc = MakeProcessor();
        proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        player.HP.Should().Be(player.MaxHP);
    }

    [Fact]
    public void BeltRegen_MultipleTurns_AccumulatesHeal()
    {
        var player = MakePlayer();
        player.HP = 40;
        player.MaxHP = 100;

        var belt = new Item
        {
            Name = "Belt of Regeneration",
            Type = ItemType.Accessory,
            Tier = ItemTier.Rare,
            IsEquippable = true,
            PassiveEffectId = "belt_regen"
        };
        player.Inventory.Add(belt);
        player.EquipItem(belt);

        var proc = MakeProcessor();
        for (int i = 0; i < 5; i++)
            proc.ProcessPassiveEffects(player, PassiveEffectTrigger.OnTurnStart, null, 0);

        player.HP.Should().Be(55); // 40 + (3 * 5)
    }
}
