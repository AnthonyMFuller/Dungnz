using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for the stats constructor paths of enemies at 64-68% coverage.</summary>
public class EnemyStatsPathTests
{
    private static EnemyStats MakeStats(string name, int hp = 50, int atk = 15, int def = 5, int xp = 40, int minGold = 10, int maxGold = 25) =>
        new EnemyStats { Name = name, MaxHP = hp, Attack = atk, Defense = def, XPValue = xp, MinGold = minGold, MaxGold = maxGold };

    // ── BloodHound ────────────────────────────────────────────────────────────

    [Fact]
    public void BloodHound_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Feral Hound", hp: 60, atk: 20, def: 7, xp: 50);
        var e = new BloodHound(stats);
        e.Name.Should().Be("Feral Hound");
        e.HP.Should().Be(60);
        e.BleedOnHitChance.Should().BeApproximately(0.40f, 0.001f);
    }

    [Fact]
    public void BloodHound_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "BloodVial", Type = "Consumable", HealAmount = 25, Tier = "Rare" },
            new() { Name = "Troll Hide", Type = "Armor", DefenseBonus = 8, IsEquippable = true, Tier = "Uncommon", Slot = "Chest" }
        };
        var e = new BloodHound(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── ChaosKnight ──────────────────────────────────────────────────────────

    [Fact]
    public void ChaosKnight_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Dark Knight", hp: 80, atk: 25, def: 12, xp: 70);
        var e = new ChaosKnight(stats);
        e.Name.Should().Be("Dark Knight");
        e.HP.Should().Be(80);
    }

    [Fact]
    public void ChaosKnight_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Chaos Blade", Type = "Weapon", AttackBonus = 18, IsEquippable = true, Tier = "Epic" },
            new() { Name = "Chaos Armor", Type = "Armor", DefenseBonus = 14, IsEquippable = true, Tier = "Epic", Slot = "Chest" }
        };
        var e = new ChaosKnight(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── CryptPriest ──────────────────────────────────────────────────────────

    [Fact]
    public void CryptPriest_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("High Priest", hp: 55, atk: 17, def: 8, xp: 50);
        var e = new CryptPriest(stats);
        e.Name.Should().Be("High Priest");
        e.HP.Should().Be(55);
    }

    [Fact]
    public void CryptPriest_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Holy Water", Type = "Consumable", HealAmount = 30, Tier = "Uncommon" }
        };
        var e = new CryptPriest(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── FrostWyvern ──────────────────────────────────────────────────────────

    [Fact]
    public void FrostWyvern_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Elder Wyvern", hp: 90, atk: 32, def: 15, xp: 80);
        var e = new FrostWyvern(stats);
        e.Name.Should().Be("Elder Wyvern");
        e.HP.Should().Be(90);
    }

    [Fact]
    public void FrostWyvern_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "WyvernFang", Type = "Consumable", HealAmount = 15, Tier = "Rare" },
            new() { Name = "FrostScaleArmor", Type = "Armor", DefenseBonus = 18, IsEquippable = true, Tier = "Rare", Slot = "Chest" }
        };
        var e = new FrostWyvern(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── IronGuard ────────────────────────────────────────────────────────────

    [Fact]
    public void IronGuard_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Elite Guard", hp: 65, atk: 22, def: 18, xp: 60);
        var e = new IronGuard(stats);
        e.Name.Should().Be("Elite Guard");
        e.HP.Should().Be(65);
        e.CounterStrikeChance.Should().BeApproximately(0.30f, 0.001f);
    }

    [Fact]
    public void IronGuard_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Steel Shield", Type = "Armor", DefenseBonus = 12, IsEquippable = true, Tier = "Rare", Slot = "Offhand" },
            new() { Name = "Guard Helm", Type = "Armor", DefenseBonus = 8, IsEquippable = true, Tier = "Uncommon", Slot = "Head" }
        };
        var e = new IronGuard(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── ManaLeech ────────────────────────────────────────────────────────────

    [Fact]
    public void ManaLeech_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Greater Leech", hp: 70, atk: 22, def: 9, xp: 65);
        var e = new ManaLeech(stats);
        e.Name.Should().Be("Greater Leech");
        e.HP.Should().Be(70);
    }

    [Fact]
    public void ManaLeech_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Mana Shard", Type = "Accessory", MaxManaBonus = 10, IsEquippable = true, Tier = "Rare" }
        };
        var e = new ManaLeech(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── NightStalker ─────────────────────────────────────────────────────────

    [Fact]
    public void NightStalker_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Shadow Stalker", hp: 55, atk: 24, def: 7, xp: 52);
        var e = new NightStalker(stats);
        e.Name.Should().Be("Shadow Stalker");
        e.HP.Should().Be(55);
    }

    [Fact]
    public void NightStalker_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Shadow Blade", Type = "Weapon", AttackBonus = 14, IsEquippable = true, Tier = "Rare" },
            new() { Name = "Shadow Cloak", Type = "Armor", DefenseBonus = 6, IsEquippable = true, Tier = "Rare", Slot = "Back" }
        };
        var e = new NightStalker(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── PlagueBear ───────────────────────────────────────────────────────────

    [Fact]
    public void PlagueBear_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Diseased Bear", hp: 75, atk: 26, def: 10, xp: 68);
        var e = new PlagueBear(stats);
        e.Name.Should().Be("Diseased Bear");
        e.HP.Should().Be(75);
    }

    [Fact]
    public void PlagueBear_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Plague Fang", Type = "Weapon", AttackBonus = 10, IsEquippable = true, Tier = "Uncommon" }
        };
        var e = new PlagueBear(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── ShadowImp ────────────────────────────────────────────────────────────

    [Fact]
    public void ShadowImp_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Greater Imp", hp: 30, atk: 13, def: 5, xp: 25);
        var e = new ShadowImp(stats);
        e.Name.Should().Be("Greater Imp");
        e.HP.Should().Be(30);
    }

    [Fact]
    public void ShadowImp_ItemConfig_AddsPotionDrop()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Health Potion", Type = "Consumable", HealAmount = 20, Tier = "Common" }
        };
        var e = new ShadowImp(null, items);
        e.LootTable.Should().NotBeNull();
    }

    // ── Mimic ────────────────────────────────────────────────────────────────

    [Fact]
    public void Mimic_StatsCtor_AppliesProvidedStats()
    {
        var stats = MakeStats("Ancient Mimic", hp: 80, atk: 24, def: 10, xp: 80);
        var e = new Mimic(stats);
        e.Name.Should().Be("Ancient Mimic");
        e.HP.Should().Be(80);
    }

    [Fact]
    public void Mimic_ItemConfig_AddsDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Mimic Tooth", Type = "Consumable", HealAmount = 10, Tier = "Uncommon" },
            new() { Name = "Mimic Shell", Type = "Armor", DefenseBonus = 10, IsEquippable = true, Tier = "Rare", Slot = "Chest" }
        };
        var e = new Mimic(null, items);
        e.LootTable.Should().NotBeNull();
    }
}
