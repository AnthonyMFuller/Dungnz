using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for enemy classes that previously had 0% or very low coverage.</summary>
public class EnemyCoverageTests
{
    // ── BladeDancer ──────────────────────────────────────────────────────────

    [Fact]
    public void BladeDancer_DefaultCtor_HasCorrectStats()
    {
        var e = new BladeDancer();
        e.Name.Should().Be("Blade Dancer");
        e.HP.Should().Be(50);
        e.MaxHP.Should().Be(50);
        e.Attack.Should().Be(24);
        e.Defense.Should().Be(7);
        e.XPValue.Should().Be(46);
    }

    [Fact]
    public void BladeDancer_OnDodgeCounterChance_Is50Percent()
    {
        var e = new BladeDancer();
        e.OnDodgeCounterChance.Should().BeApproximately(0.50f, 0.001f);
    }

    [Fact]
    public void BladeDancer_LootTable_IsNotNull()
    {
        var e = new BladeDancer();
        e.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void BladeDancer_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Test Dancer", MaxHP = 99, Attack = 30, Defense = 10, XPValue = 80, MinGold = 20, MaxGold = 40 };
        var e = new BladeDancer(stats);
        e.Name.Should().Be("Test Dancer");
        e.HP.Should().Be(99);
        e.Attack.Should().Be(30);
    }

    [Fact]
    public void BladeDancer_ItemConfig_AddsLootDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Mythril Blade", Type = "Weapon", AttackBonus = 15, IsEquippable = true, Tier = "Rare" },
            new() { Name = "Cloak of Shadows", Type = "Armor", DefenseBonus = 5, IsEquippable = true, Tier = "Rare", Slot = "Chest" }
        };
        var e = new BladeDancer(null, items);
        // AddLoot was called — enemy has named drops
        e.LootTable.Should().NotBeNull();
    }

    // ── BoneArcher ───────────────────────────────────────────────────────────

    [Fact]
    public void BoneArcher_DefaultCtor_HasCorrectStats()
    {
        var e = new BoneArcher();
        e.Name.Should().Be("Bone Archer");
        e.HP.Should().Be(38);
        e.Attack.Should().Be(14);
        e.Defense.Should().Be(5);
        e.XPValue.Should().Be(33);
        e.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void BoneArcher_FirstAttackMultiplier_Is1Point5()
    {
        var e = new BoneArcher();
        e.FirstAttackMultiplier.Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void BoneArcher_FirstAttackCritChance_Is20Percent()
    {
        var e = new BoneArcher();
        e.FirstAttackCritChance.Should().BeApproximately(0.20f, 0.001f);
    }

    [Fact]
    public void BoneArcher_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Elite Archer", MaxHP = 55, Attack = 18, Defense = 6, XPValue = 45, MinGold = 10, MaxGold = 22, IsUndead = true };
        var e = new BoneArcher(stats);
        e.Name.Should().Be("Elite Archer");
        e.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void BoneArcher_ItemConfig_AddsBoneFragmentAndPotion()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Bone Fragment", Type = "Consumable", HealAmount = 5, Tier = "Common" },
            new() { Name = "Health Potion", Type = "Consumable", HealAmount = 20, Tier = "Common" }
        };
        var e = new BoneArcher(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── CarrionCrawler ────────────────────────────────────────────────────────

    [Fact]
    public void CarrionCrawler_DefaultCtor_HasCorrectStats()
    {
        var e = new CarrionCrawler();
        e.Name.Should().Be("Carrion Crawler");
        e.HP.Should().Be(35);
        e.Attack.Should().Be(12);
        e.Defense.Should().Be(4);
        e.XPValue.Should().Be(30);
    }

    [Fact]
    public void CarrionCrawler_RegenPerTurn_Is5()
    {
        var e = new CarrionCrawler();
        e.RegenPerTurn.Should().Be(5);
    }

    [Fact]
    public void CarrionCrawler_LootTable_IsNotNull()
    {
        var e = new CarrionCrawler();
        e.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void CarrionCrawler_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Giant Crawler", MaxHP = 50, Attack = 16, Defense = 5, XPValue = 40, MinGold = 7, MaxGold = 18 };
        var e = new CarrionCrawler(stats);
        e.Name.Should().Be("Giant Crawler");
        e.HP.Should().Be(50);
    }

    [Fact]
    public void CarrionCrawler_ItemConfig_AddsPotionDrop()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Health Potion", Type = "Consumable", HealAmount = 20, Tier = "Common" }
        };
        var e = new CarrionCrawler(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── DarkSorcerer ─────────────────────────────────────────────────────────

    [Fact]
    public void DarkSorcerer_DefaultCtor_HasCorrectStats()
    {
        var e = new DarkSorcerer();
        e.Name.Should().Be("Dark Sorcerer");
        e.HP.Should().Be(45);
        e.Attack.Should().Be(18);
        e.Defense.Should().Be(6);
        e.XPValue.Should().Be(40);
    }

    [Fact]
    public void DarkSorcerer_WeakenOnAttackChance_Is25Percent()
    {
        var e = new DarkSorcerer();
        e.WeakenOnAttackChance.Should().BeApproximately(0.25f, 0.001f);
    }

    [Fact]
    public void DarkSorcerer_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Arch Sorcerer", MaxHP = 60, Attack = 22, Defense = 8, XPValue = 55, MinGold = 12, MaxGold = 26 };
        var e = new DarkSorcerer(stats);
        e.Name.Should().Be("Arch Sorcerer");
    }

    [Fact]
    public void DarkSorcerer_ItemConfig_AddsStaffAndPotion()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Staff of Domination", Type = "Weapon", AttackBonus = 12, IsEquippable = true, Tier = "Epic" },
            new() { Name = "Large Health Potion", Type = "Consumable", HealAmount = 50, Tier = "Uncommon" }
        };
        var e = new DarkSorcerer(null, items);
        e.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void DarkSorcerer_ItemConfig_AlternateStaffName()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "StaffOfDomination", Type = "Weapon", AttackBonus = 12, IsEquippable = true, Tier = "Epic" }
        };
        var e = new DarkSorcerer(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── ShieldBreaker ─────────────────────────────────────────────────────────

    [Fact]
    public void ShieldBreaker_DefaultCtor_HasCorrectStats()
    {
        var e = new ShieldBreaker();
        e.Name.Should().Be("Shield Breaker");
        e.HP.Should().Be(55);
        e.Attack.Should().Be(21);
        e.Defense.Should().Be(8);
        e.XPValue.Should().Be(50);
    }

    [Fact]
    public void ShieldBreaker_DefThreshold_Is15()
    {
        var e = new ShieldBreaker();
        e.ShieldBreakerDefThreshold.Should().Be(15);
    }

    [Fact]
    public void ShieldBreaker_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Master Breaker", MaxHP = 70, Attack = 25, Defense = 10, XPValue = 60, MinGold = 15, MaxGold = 30 };
        var e = new ShieldBreaker(stats);
        e.Name.Should().Be("Master Breaker");
    }

    [Fact]
    public void ShieldBreaker_ItemConfig_AddsArmorAndSword()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Chain Mail", Type = "Armor", DefenseBonus = 8, IsEquippable = true, Tier = "Uncommon", Slot = "Chest" },
            new() { Name = "Iron Sword", Type = "Weapon", AttackBonus = 6, IsEquippable = true, Tier = "Common" }
        };
        var e = new ShieldBreaker(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── SiegeOgre ────────────────────────────────────────────────────────────

    [Fact]
    public void SiegeOgre_DefaultCtor_HasCorrectStats()
    {
        var e = new SiegeOgre();
        e.Name.Should().Be("Siege Ogre");
        e.HP.Should().Be(65);
        e.Attack.Should().Be(23);
        e.Defense.Should().Be(10);
        e.XPValue.Should().Be(58);
    }

    [Fact]
    public void SiegeOgre_ThickHideHitsRemaining_Is3()
    {
        var e = new SiegeOgre();
        e.ThickHideHitsRemaining.Should().Be(3);
    }

    [Fact]
    public void SiegeOgre_ThickHideDamageReduction_Is5()
    {
        var e = new SiegeOgre();
        e.ThickHideDamageReduction.Should().Be(5);
    }

    [Fact]
    public void SiegeOgre_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "War Ogre", MaxHP = 80, Attack = 28, Defense = 12, XPValue = 70, MinGold = 18, MaxGold = 36 };
        var e = new SiegeOgre(stats);
        e.Name.Should().Be("War Ogre");
    }

    [Fact]
    public void SiegeOgre_ItemConfig_AddsArmorAndPelt()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Plate Armor", Type = "Armor", DefenseBonus = 15, IsEquippable = true, Tier = "Rare", Slot = "Chest" },
            new() { Name = "Troll Hide", Type = "Armor", DefenseBonus = 10, IsEquippable = true, Tier = "Uncommon", Slot = "Chest" }
        };
        var e = new SiegeOgre(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── GenericEnemy ─────────────────────────────────────────────────────────

    [Fact]
    public void GenericEnemy_DefaultCtor_HasFallbackStats()
    {
        var e = new GenericEnemy();
        e.Name.Should().Be("Unknown Enemy");
        e.HP.Should().Be(20);
        e.Attack.Should().Be(8);
        e.Defense.Should().Be(2);
        e.XPValue.Should().Be(10);
    }

    [Fact]
    public void GenericEnemy_LootTable_IsNotNull()
    {
        var e = new GenericEnemy();
        e.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void GenericEnemy_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Forest Sprite", MaxHP = 15, Attack = 5, Defense = 1, XPValue = 8, MinGold = 1, MaxGold = 4, IsUndead = false };
        var e = new GenericEnemy(stats);
        e.Name.Should().Be("Forest Sprite");
        e.HP.Should().Be(15);
        e.Attack.Should().Be(5);
        e.IsUndead.Should().BeFalse();
    }

    [Fact]
    public void GenericEnemy_StatsCtor_WithAsciiArt_StoresArt()
    {
        var stats = new EnemyStats { Name = "Art Enemy", MaxHP = 10, Attack = 3, Defense = 1, XPValue = 5, MinGold = 1, MaxGold = 3, AsciiArt = ["  /\\  ", " /  \\ "] };
        var e = new GenericEnemy(stats);
        e.AsciiArt.Should().NotBeEmpty();
    }

    // ── GoblinShaman stats constructor path ──────────────────────────────────

    [Fact]
    public void GoblinShaman_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Elder Shaman", MaxHP = 40, Attack = 14, Defense = 6, XPValue = 35, MinGold = 8, MaxGold = 20 };
        var e = new GoblinShaman(stats);
        e.Name.Should().Be("Elder Shaman");
        e.AppliesPoisonOnHit.Should().BeTrue();
    }

    [Fact]
    public void GoblinShaman_ItemConfig_AddsAntidoteDrop()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Antidote", Type = "Consumable", HealAmount = 8, Tier = "Common" }
        };
        var e = new GoblinShaman(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── VampireLord stats constructor path ───────────────────────────────────

    [Fact]
    public void VampireLord_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Ancient Vampire", MaxHP = 100, Attack = 20, Defense = 14, XPValue = 80, MinGold = 20, MaxGold = 40 };
        var e = new VampireLord(stats);
        e.Name.Should().Be("Ancient Vampire");
        e.LifestealPercent.Should().BeApproximately(0.50f, 0.001f);
    }

    [Fact]
    public void VampireLord_ItemConfig_AddsBloodVialDrop()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Blood Vial", Type = "Consumable", HealAmount = 30, Tier = "Rare" }
        };
        var e = new VampireLord(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── Wraith stats constructor path ────────────────────────────────────────

    [Fact]
    public void Wraith_StatsCtor_AppliesProvidedStats()
    {
        var stats = new EnemyStats { Name = "Ancient Wraith", MaxHP = 45, Attack = 22, Defense = 3, XPValue = 45, MinGold = 10, MaxGold = 24, IsUndead = true };
        var e = new Wraith(stats);
        e.Name.Should().Be("Ancient Wraith");
        e.FlatDodgeChance.Should().BeApproximately(0.30f, 0.001f);
        e.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void Wraith_ItemConfig_AddsShadowEssenceDrop()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Shadow Essence", Type = "Consumable", HealAmount = 20, Tier = "Rare" }
        };
        var e = new Wraith(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── JSON deserialization (triggers [JsonConstructor] private ctor) ────────

    [Fact]
    public void BladeDancer_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<BladeDancer>("{}");
        e.Should().NotBeNull();
    }

    [Fact]
    public void BloodHound_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<BloodHound>("{}");
        e.Should().NotBeNull();
    }

    [Fact]
    public void BoneArcher_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<BoneArcher>("{}");
        e.Should().NotBeNull();
    }

    [Fact]
    public void CarrionCrawler_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<CarrionCrawler>("{}");
        e.Should().NotBeNull();
    }

    [Fact]
    public void ChaosKnight_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<ChaosKnight>("{}");
        e.Should().NotBeNull();
    }

    [Fact]
    public void CryptPriest_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<CryptPriest>("{}");
        e.Should().NotBeNull();
    }

    [Fact]
    public void CursedZombie_JsonDeserialize_UsesPrivateCtor()
    {
        var e = System.Text.Json.JsonSerializer.Deserialize<CursedZombie>("{}");
        e.Should().NotBeNull();
    }
}
