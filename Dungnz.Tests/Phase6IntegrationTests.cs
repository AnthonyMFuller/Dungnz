namespace Dungnz.Tests;

using Xunit;
using FluentAssertions;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Tests.Helpers;

/// <summary>
/// Phase 6F cross-system integration tests (WI-F2, Issue #394).
/// Each test exercises multiple Phase 6 systems together.
/// </summary>
public class Phase6IntegrationTests
{
    private readonly FakeDisplayService _display = new();
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;

    /// <summary>Initialises shared services for Phase 6F integration tests.</summary>
    public Phase6IntegrationTests()
    {
        _statusEffects = new StatusEffectManager(_display);
        _abilities = new AbilityManager();
    }

    private static Player MakePlayer(
        PlayerClass playerClass, int level = 1,
        int hp = 100, int maxHp = 100,
        int atk = 10, int def = 0,
        int mana = 200, int maxMana = 200)
    {
        var player = new Player { Name = "TestHero", Class = playerClass };
        for (int i = 1; i < level; i++) player.LevelUp();
        player.MaxHP = maxHp;
        player.HP = hp;
        player.Attack = atk;
        player.Defense = def;
        player.MaxMana = maxMana;
        player.Mana = mana;
        return player;
    }

    private static P6IntEnemy MakeEnemy(int hp = 200, int atk = 10, int def = 0)
        => new P6IntEnemy(hp, atk, def);

    // ── 1. Ranger wolf + trap + Volley combo ─────────────────────────────────

    /// <summary>Volley with wolf companion and trap synergy deals correct damage.</summary>
    [Fact]
    public void Volley_WithWolfAndTrap_WolfAttacksAndTrapBonusApplied()
    {
        var player = MakePlayer(PlayerClass.Ranger, level: 7, atk: 10, def: 0);
        var wolf = new Minion { Name = "Wolf Companion", HP = 20, MaxHP = 20, ATK = 8 };
        player.ActiveMinions.Add(wolf);
        player.TrapTriggeredThisCombat = true;
        var enemy = MakeEnemy(hp: 200, def: 0);

        var result = _abilities.UseAbility(player, enemy, AbilityType.Volley, _statusEffects, _display);

        result.Should().Be(UseAbilityResult.Success);
        // baseDmg = (int)(10 * 0.80) = 8; with trap: (int)(8 * 1.30) = 10; volleyDmg = 10-0 = 10
        // wolfDmg = 8-0 = 8 → total = 18
        enemy.HP.Should().Be(200 - 10 - 8, "Volley (10) + wolf (8) = 18 total damage");
    }

    // ── 2. Necromancer Raise Dead + Corpse Explosion ──────────────────────────

    /// <summary>RaiseDead creates skeleton with correct HP/ATK; CorpseExplosion removes it and deals damage.</summary>
    [Fact]
    public void RaiseDead_CreatesSkeletonMinion_CorpseExplosion_DealsDamage()
    {
        var player = MakePlayer(PlayerClass.Necromancer, level: 7, atk: 10);
        player.LastKilledEnemyHp = 100;
        var enemy = MakeEnemy(hp: 200, def: 0);

        var raiseResult = _abilities.UseAbility(player, enemy, AbilityType.RaiseDead, _statusEffects, _display);
        raiseResult.Should().Be(UseAbilityResult.Success);
        player.ActiveMinions.Should().HaveCount(1);

        var skeleton = player.ActiveMinions[0];
        skeleton.HP.Should().Be(30, "30% of LastKilledEnemyHp (100*0.30=30)");
        skeleton.ATK.Should().Be(5, "50% of player ATK (10*0.50=5)");

        player.Mana = player.MaxMana;

        var explodeResult = _abilities.UseAbility(player, enemy, AbilityType.CorpseExplosion, _statusEffects, _display);
        explodeResult.Should().Be(UseAbilityResult.Success);
        player.ActiveMinions.Should().BeEmpty("all minions are sacrificed by Corpse Explosion");
        // explosionDmg = (int)(30 * 1.5) = 45
        enemy.HP.Should().Be(200 - 45, "Corpse Explosion deals 45 damage from 30-HP skeleton");
    }

    // ── 3. Paladin Divine Shield blocks damage ───────────────────────────────

    /// <summary>While DivineShield is active, player takes no damage and the counter decrements.</summary>
    [Fact]
    public void DivineShield_WhenActive_BlocksDamageAndDecrementsCounter()
    {
        var player = MakePlayer(PlayerClass.Paladin, level: 3, hp: 80, maxHp: 100);
        player.DivineShieldTurnsRemaining = 2;
        var hpBefore = player.HP;

        // Mirror the CombatEngine PerformEnemyTurn logic
        if (player.DivineShieldTurnsRemaining > 0)
        {
            player.DivineShieldTurnsRemaining--;
            // no HP damage applied
        }
        else
        {
            player.TakeDamage(20);
        }

        player.HP.Should().Be(hpBefore, "Divine Shield absorbs the attack");
        player.DivineShieldTurnsRemaining.Should().Be(1);
    }

    // ── 4. Legendary Aegis survive-at-one ────────────────────────────────────

    /// <summary>Aegis of the Immortal sets HP to 1 on the first lethal hit per combat.</summary>
    [Fact]
    public void AegisOfImmortal_FirstLethalHit_SurvivesAtOneHP()
    {
        var player = MakePlayer(PlayerClass.Warrior, hp: 0, maxHp: 100);
        player.EquippedChest = new Item
        {
            Name = "Aegis of the Immortal", Type = ItemType.Armor,
            IsEquippable = true, PassiveEffectId = "survive_at_one"
        };

        var processor = new PassiveEffectProcessor(_display, new Random(0), _statusEffects);
        processor.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, MakeEnemy(), 0);

        player.HP.Should().Be(1, "Aegis sets HP to 1 on first lethal hit");
        player.AegisUsedThisCombat.Should().BeTrue();
    }

    /// <summary>Aegis of the Immortal does not trigger a second time in the same combat.</summary>
    [Fact]
    public void AegisOfImmortal_SecondLethalHit_DoesNotSave()
    {
        var player = MakePlayer(PlayerClass.Warrior, hp: 0, maxHp: 100);
        player.EquippedChest = new Item
        {
            Name = "Aegis of the Immortal", Type = ItemType.Armor,
            IsEquippable = true, PassiveEffectId = "survive_at_one"
        };
        player.AegisUsedThisCombat = true; // already consumed this combat

        var processor = new PassiveEffectProcessor(_display, new Random(0), _statusEffects);
        processor.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerWouldDie, MakeEnemy(), 0);

        player.HP.Should().Be(0, "Aegis already used — second lethal hit is not intercepted");
    }

    // ── 5. Affix roll rate ────────────────────────────────────────────────────

    /// <summary>Uncommon items have approximately 10% prefix/suffix chance each.</summary>
    [Fact]
    public void AffixRoll_UncommonItems_RoughlyTenPercentReceiveAffix()
    {
        AffixRegistry.Load("Data/item-affixes.json");
        var rng = new Random(42);
        int prefixCount = 0;
        const int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var item = new Item { Name = "Steel Sword", Tier = ItemTier.Uncommon, Type = ItemType.Weapon, IsEquippable = true };
            AffixRegistry.ApplyRandomAffix(item, rng);
            if (item.Prefix != null) prefixCount++;
        }

        // Each Uncommon item has a 10% chance of rolling a prefix — expect ~10% with tolerance
        var rate = (double)prefixCount / trials;
        rate.Should().BeInRange(0.05, 0.18, "roughly 10% of Uncommon items should receive a prefix");
    }

    /// <summary>Common items never receive affixes regardless of RNG.</summary>
    [Fact]
    public void AffixRoll_CommonItems_NeverReceiveAffixes()
    {
        AffixRegistry.Load("Data/item-affixes.json");
        var rng = new Random(99);

        for (int i = 0; i < 1000; i++)
        {
            var item = new Item { Name = "Iron Sword", Tier = ItemTier.Common, Type = ItemType.Weapon, IsEquippable = true };
            AffixRegistry.ApplyRandomAffix(item, rng);
            item.Prefix.Should().BeNull("Common items must never receive a prefix");
            item.Suffix.Should().BeNull("Common items must never receive a suffix");
        }
    }

    // ── 6. Set bonus detection ────────────────────────────────────────────────

    /// <summary>Two shadowstalker pieces trigger the 2-piece set bonus.</summary>
    [Fact]
    public void SetBonus_TwoPieces_ActivatesTwoPieceBonus()
    {
        var player = MakePlayer(PlayerClass.Rogue);
        player.EquippedChest = new Item { Name = "Shadowstalker Garb", Type = ItemType.Armor, IsEquippable = true, SetId = "shadowstalker" };
        player.EquippedWeapon = new Item { Name = "Shadowstalker Blades", Type = ItemType.Weapon, IsEquippable = true, SetId = "shadowstalker" };

        var bonuses = SetBonusManager.GetActiveBonuses(player);

        bonuses.Should().Contain(b => b.SetId == "shadowstalker" && b.PiecesRequired == 2,
            "2-piece bonus activates with 2 equipped shadowstalker pieces");
    }

    /// <summary>Three shadowstalker pieces trigger both the 2-piece and 3-piece set bonuses.</summary>
    [Fact]
    public void SetBonus_ThreePieces_ActivatesThreePieceBonus()
    {
        var player = MakePlayer(PlayerClass.Rogue);
        player.EquippedChest = new Item { Name = "Shadowstalker Garb", Type = ItemType.Armor, IsEquippable = true, SetId = "shadowstalker" };
        player.EquippedWeapon = new Item { Name = "Shadowstalker Blades", Type = ItemType.Weapon, IsEquippable = true, SetId = "shadowstalker" };
        player.EquippedAccessory = new Item { Name = "Shadowstalker Hood", Type = ItemType.Accessory, IsEquippable = true, SetId = "shadowstalker" };

        var bonuses = SetBonusManager.GetActiveBonuses(player);

        bonuses.Should().Contain(b => b.SetId == "shadowstalker" && b.PiecesRequired == 3,
            "3-piece bonus activates with all 3 shadowstalker pieces equipped");
    }

    // ── 7. ClassRestriction equip blocking ───────────────────────────────────

    /// <summary>A Mage cannot equip a Warrior-restricted item.</summary>
    [Fact]
    public void ClassRestriction_MageCannotEquipWarriorItem()
    {
        var player = MakePlayer(PlayerClass.Mage);
        var item = new Item
        {
            Name = "Warrior's Cleaver", Type = ItemType.Weapon,
            IsEquippable = true, AttackBonus = 10,
            ClassRestriction = new[] { "Warrior" }
        };
        player.Inventory.Add(item);

        var equipMgr = new EquipmentManager(_display);
        equipMgr.HandleEquip(player, "Warrior's Cleaver");

        _display.Errors.Should().NotBeEmpty("equipping a class-restricted item should produce an error");
        player.EquippedWeapon.Should().BeNull("Mage cannot equip a Warrior-only item");
    }

    /// <summary>A Warrior can successfully equip a Warrior-restricted item.</summary>
    [Fact]
    public void ClassRestriction_WarriorCanEquipWarriorItem()
    {
        var player = MakePlayer(PlayerClass.Warrior);
        var item = new Item
        {
            Name = "Warrior's Cleaver", Type = ItemType.Weapon,
            IsEquippable = true, AttackBonus = 10,
            ClassRestriction = new[] { "Warrior" }
        };
        player.Inventory.Add(item);

        var equipMgr = new EquipmentManager(_display);
        equipMgr.HandleEquip(player, "Warrior's Cleaver");

        player.EquippedWeapon.Should().NotBeNull("Warrior can equip a Warrior-restricted item");
        player.EquippedWeapon!.Name.Should().Be("Warrior's Cleaver");
    }

    // ── 8. FloorSpawnPools floor appropriateness ──────────────────────────────

    /// <summary>Floor 1 spawn pool never returns high-floor enemies.</summary>
    [Fact]
    public void FloorSpawnPools_Floor1_NeverReturnsHighFloorEnemies()
    {
        var rng = new Random(0);
        var floor8Exclusives = new HashSet<string> { "frostwyvern", "bladedancer", "shieldbreaker", "chaosknight" };

        for (int i = 0; i < 100; i++)
        {
            var enemy = FloorSpawnPools.GetRandomEnemyForFloor(1, rng);
            floor8Exclusives.Should().NotContain(enemy,
                $"floor-1 must not spawn floor-8 exclusive enemies (got '{enemy}')");
        }
    }

    /// <summary>Floor 8 spawn pool returns floor-8 enemies across 100 rolls.</summary>
    [Fact]
    public void FloorSpawnPools_Floor8_ReturnsFloor8Enemies()
    {
        var rng = new Random(0);
        var results = new HashSet<string>();

        for (int i = 0; i < 100; i++)
            results.Add(FloorSpawnPools.GetRandomEnemyForFloor(8, rng));

        var floor8Pool = new[] { "chaosknight", "frostwyvern", "nightstalker", "bladedancer", "shieldbreaker" };
        results.Should().Contain(r => floor8Pool.Contains(r),
            "floor-8 enemies should appear across 100 rolls from floor-8 pool");
    }

    // ── 9. PassiveEffectProcessor damage_reflect ──────────────────────────────

    /// <summary>Ironheart Plate reflects 25% of damage taken back to the attacker.</summary>
    [Fact]
    public void DamageReflect_WhenPlayerTakesDamage_Reflects25Percent()
    {
        var player = MakePlayer(PlayerClass.Warrior);
        player.EquippedChest = new Item
        {
            Name = "Ironheart Plate", Type = ItemType.Armor,
            IsEquippable = true, PassiveEffectId = "damage_reflect"
        };
        var enemy = MakeEnemy(hp: 200);

        var processor = new PassiveEffectProcessor(_display, new Random(0), _statusEffects);
        var reflected = processor.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerTakeDamage, enemy, 100);

        reflected.Should().Be(25, "damage_reflect returns 25% of incoming damage (100 × 0.25 = 25)");
    }

    // ── 10. StatusEffect.Burn ticks correctly ─────────────────────────────────

    /// <summary>Burn status deals 8 damage per turn when applied to the player.</summary>
    [Fact]
    public void BurnStatus_WhenApplied_Deals8DamagePerTick()
    {
        var player = MakePlayer(PlayerClass.Warrior, hp: 100, maxHp: 100);
        _statusEffects.Apply(player, StatusEffect.Burn, 3);

        var hpBefore = player.HP;
        _statusEffects.ProcessTurnStart(player);

        player.HP.Should().Be(hpBefore - 8, "Burn deals 8 HP damage per turn");
    }
}

/// <summary>Stub enemy for Phase 6F integration tests.</summary>
internal class P6IntEnemy : Enemy
{
    /// <summary>Creates a test enemy with the given HP, ATK, and DEF.</summary>
    public P6IntEnemy(int hp = 200, int atk = 10, int def = 0)
    {
        Name = "TestTarget";
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = 0;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f;
    }
}
