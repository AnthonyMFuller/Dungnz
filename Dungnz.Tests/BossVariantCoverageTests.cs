using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for boss variant classes that previously had low or zero coverage.</summary>
public class BossVariantCoverageTests
{
    // ── GoblinWarchief ────────────────────────────────────────────────────────

    [Fact]
    public void GoblinWarchief_DefaultCtor_HasCorrectStats()
    {
        var b = new GoblinWarchief();
        b.Name.Should().Be("Goblin Warchief");
        b.HP.Should().Be(60);
        b.MaxHP.Should().Be(60);
        b.Attack.Should().Be(10);
        b.Defense.Should().Be(3);
        b.XPValue.Should().Be(60);
        b.FloorNumber.Should().Be(1);
    }

    [Fact]
    public void GoblinWarchief_HasReinforcementsPhase()
    {
        var b = new GoblinWarchief();
        b.Phases.Should().ContainSingle(p => p.AbilityName == "Reinforcements");
    }

    [Fact]
    public void GoblinWarchief_StatsCtor_HasCorrectStats()
    {
        var b = new GoblinWarchief(null, null);
        b.Name.Should().Be("Goblin Warchief");
        b.HP.Should().Be(60);
    }

    // ── PlagueHoundAlpha ──────────────────────────────────────────────────────

    [Fact]
    public void PlagueHoundAlpha_DefaultCtor_HasCorrectStats()
    {
        var b = new PlagueHoundAlpha();
        b.Name.Should().Be("Plague Hound Alpha");
        b.HP.Should().Be(80);
        b.Attack.Should().Be(13);
        b.Defense.Should().Be(4);
        b.XPValue.Should().Be(75);
        b.FloorNumber.Should().Be(2);
    }

    [Fact]
    public void PlagueHoundAlpha_AppliesPoison()
    {
        var b = new PlagueHoundAlpha();
        b.AppliesPoisonOnHit.Should().BeTrue();
    }

    [Fact]
    public void PlagueHoundAlpha_HasBloodfrenzyPhase()
    {
        var b = new PlagueHoundAlpha();
        b.Phases.Should().ContainSingle(p => p.AbilityName == "Bloodfrenzy");
    }

    [Fact]
    public void PlagueHoundAlpha_StatsCtor_HasCorrectStats()
    {
        var b = new PlagueHoundAlpha(null, null);
        b.Name.Should().Be("Plague Hound Alpha");
        b.AppliesPoisonOnHit.Should().BeTrue();
    }

    // ── IronSentinel ──────────────────────────────────────────────────────────

    [Fact]
    public void IronSentinel_DefaultCtor_HasCorrectStats()
    {
        var b = new IronSentinel();
        b.Name.Should().Be("Iron Sentinel");
        b.HP.Should().Be(110);
        b.Attack.Should().Be(14);
        b.Defense.Should().Be(10);
        b.XPValue.Should().Be(90);
        b.FloorNumber.Should().Be(3);
    }

    [Fact]
    public void IronSentinel_ProtectionDR_Is50Percent()
    {
        var b = new IronSentinel();
        b.ProtectionDR.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void IronSentinel_HasStunningBlowPhase()
    {
        var b = new IronSentinel();
        b.Phases.Should().ContainSingle(p => p.AbilityName == "StunningBlow");
    }

    [Fact]
    public void IronSentinel_StatsCtor_HasCorrectStats()
    {
        var b = new IronSentinel(null, null);
        b.Name.Should().Be("Iron Sentinel");
        b.HP.Should().Be(110);
    }

    // ── BoneArchon ────────────────────────────────────────────────────────────

    [Fact]
    public void BoneArchon_DefaultCtor_HasCorrectStats()
    {
        var b = new BoneArchon();
        b.Name.Should().Be("Bone Archon");
        b.HP.Should().Be(130);
        b.Attack.Should().Be(16);
        b.Defense.Should().Be(6);
        b.XPValue.Should().Be(110);
        b.IsUndead.Should().BeTrue();
        b.FloorNumber.Should().Be(4);
    }

    [Fact]
    public void BoneArchon_HasWeakenAuraPhase()
    {
        var b = new BoneArchon();
        b.Phases.Should().ContainSingle(p => p.AbilityName == "WeakenAura");
    }

    [Fact]
    public void BoneArchon_StatsCtor_HasCorrectStats()
    {
        var b = new BoneArchon(null, null);
        b.Name.Should().Be("Bone Archon");
        b.IsUndead.Should().BeTrue();
    }

    // ── CrimsonVampire ────────────────────────────────────────────────────────

    [Fact]
    public void CrimsonVampire_DefaultCtor_HasCorrectStats()
    {
        var b = new CrimsonVampire();
        b.Name.Should().Be("Crimson Vampire");
        b.HP.Should().Be(150);
        b.Attack.Should().Be(18);
        b.Defense.Should().Be(7);
        b.XPValue.Should().Be(130);
        b.FloorNumber.Should().Be(5);
    }

    [Fact]
    public void CrimsonVampire_LifestealPercent_Is30Percent()
    {
        var b = new CrimsonVampire();
        b.LifestealPercent.Should().BeApproximately(0.30f, 0.001f);
    }

    [Fact]
    public void CrimsonVampire_HasBloodDrainPhase()
    {
        var b = new CrimsonVampire();
        b.Phases.Should().ContainSingle(p => p.AbilityName == "BloodDrain");
    }

    [Fact]
    public void CrimsonVampire_StatsCtor_HasCorrectStats()
    {
        var b = new CrimsonVampire(null, null);
        b.Name.Should().Be("Crimson Vampire");
        b.LifestealPercent.Should().BeApproximately(0.30f, 0.001f);
    }

    // ── LichKing ─────────────────────────────────────────────────────────────

    [Fact]
    public void LichKing_DefaultCtor_HasCorrectStats()
    {
        var b = new LichKing();
        b.Name.Should().Be("Lich King");
        b.HP.Should().Be(120);
        b.MaxHP.Should().Be(120);
        b.Attack.Should().Be(18);
        b.Defense.Should().Be(5);
        b.XPValue.Should().Be(100);
        b.AppliesPoisonOnHit.Should().BeTrue();
        b.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void LichKing_StatsCtor_HasCorrectStats()
    {
        var b = new LichKing(null, null);
        b.Name.Should().Be("Lich King");
        b.IsUndead.Should().BeTrue();
        b.AppliesPoisonOnHit.Should().BeTrue();
    }

    // ── StoneTitan ────────────────────────────────────────────────────────────

    [Fact]
    public void StoneTitan_DefaultCtor_HasCorrectStats()
    {
        var b = new StoneTitan();
        b.Name.Should().Be("Stone Titan");
        b.HP.Should().Be(200);
        b.Attack.Should().Be(22);
        b.Defense.Should().Be(15);
        b.XPValue.Should().Be(100);
    }

    [Fact]
    public void StoneTitan_StatsCtor_HasCorrectStats()
    {
        var b = new StoneTitan(null, null);
        b.Name.Should().Be("Stone Titan");
        b.HP.Should().Be(200);
    }

    // ── ShadowWraith ──────────────────────────────────────────────────────────

    [Fact]
    public void ShadowWraith_DefaultCtor_HasCorrectStats()
    {
        var b = new ShadowWraith();
        b.Name.Should().Be("Shadow Wraith");
        b.HP.Should().Be(90);
        b.Attack.Should().Be(25);
        b.Defense.Should().Be(3);
        b.XPValue.Should().Be(100);
        b.FlatDodgeChance.Should().BeApproximately(0.25f, 0.001f);
    }

    [Fact]
    public void ShadowWraith_StatsCtor_HasCorrectStats()
    {
        var b = new ShadowWraith(null, null);
        b.Name.Should().Be("Shadow Wraith");
        b.FlatDodgeChance.Should().BeApproximately(0.25f, 0.001f);
    }

    // ── VampireBoss ───────────────────────────────────────────────────────────

    [Fact]
    public void VampireBoss_DefaultCtor_HasCorrectStats()
    {
        var b = new VampireBoss();
        b.Name.Should().Be("Vampire Lord");
        b.HP.Should().Be(110);
        b.Attack.Should().Be(20);
        b.Defense.Should().Be(8);
        b.XPValue.Should().Be(100);
        b.LifestealPercent.Should().BeApproximately(0.30f, 0.001f);
    }

    [Fact]
    public void VampireBoss_StatsCtor_HasCorrectStats()
    {
        var b = new VampireBoss(null, null);
        b.Name.Should().Be("Vampire Lord");
        b.LifestealPercent.Should().BeApproximately(0.30f, 0.001f);
    }

    // ── InfernalDragon ────────────────────────────────────────────────────────

    [Fact]
    public void InfernalDragon_DefaultCtor_HasCorrectStats()
    {
        var b = new InfernalDragon();
        b.Name.Should().Be("Infernal Dragon");
        b.HP.Should().Be(250);
        b.Attack.Should().Be(54);
        b.Defense.Should().Be(16);
        b.XPValue.Should().Be(220);
        b.FlameBreathCooldown.Should().Be(2);
    }

    [Fact]
    public void InfernalDragon_HasFlameBreathPhase()
    {
        var b = new InfernalDragon();
        b.Phases.Should().ContainSingle(p => p.AbilityName == "FlameBreath");
    }

    [Fact]
    public void InfernalDragon_StatsCtor_HasCorrectStats()
    {
        var b = new InfernalDragon(null, null);
        b.Name.Should().Be("Infernal Dragon");
        b.FlameBreathCooldown.Should().Be(2);
    }

    [Fact]
    public void InfernalDragon_ItemConfig_AddsLootDrops()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "InfernalGreatsword", Type = "Weapon", AttackBonus = 30, IsEquippable = true, Tier = "Legendary" },
            new() { Name = "Dragon Scale", Type = "Armor", DefenseBonus = 20, IsEquippable = true, Tier = "Legendary", Slot = "Chest" }
        };
        var b = new InfernalDragon(null, items);
        b.LootTable.Should().NotBeNull();
    }

    // ── AbyssalLeviathan stats constructor ───────────────────────────────────

    [Fact]
    public void AbyssalLeviathan_StatsCtor_HasCorrectStats()
    {
        var b = new AbyssalLeviathan(null, null);
        b.Name.Should().Be("Abyssal Leviathan");
        b.HP.Should().Be(220);
    }

    [Fact]
    public void AbyssalLeviathan_StatsCtor_ItemConfig_AddsLoot()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Dragon Scale", Type = "Armor", DefenseBonus = 20, IsEquippable = true, Tier = "Legendary", Slot = "Chest" },
            new() { Name = "Soul Gem", Type = "Accessory", StatModifier = 10, IsEquippable = true, Tier = "Legendary" }
        };
        var b = new AbyssalLeviathan(null, items);
        b.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void AbyssalLeviathan_StatsCtor_ItemConfig_AlternateNameSoulGem()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "SoulGem", Type = "Accessory", StatModifier = 10, IsEquippable = true, Tier = "Legendary" }
        };
        var b = new AbyssalLeviathan(null, items);
        b.LootTable.Should().NotBeNull();
    }

    // ── ArchlichSovereign stats constructor ───────────────────────────────────

    [Fact]
    public void ArchlichSovereign_StatsCtor_HasCorrectStats()
    {
        var b = new ArchlichSovereign(null, null);
        b.Name.Should().Be("Archlich Sovereign");
        b.IsUndead.Should().BeTrue();
    }

    [Fact]
    public void ArchlichSovereign_StatsCtor_ItemConfig_AddsLoot()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "LichsCrown", Type = "Armor", DefenseBonus = 15, IsEquippable = true, Tier = "Legendary", Slot = "Head" },
            new() { Name = "Soul Gem", Type = "Accessory", StatModifier = 10, IsEquippable = true, Tier = "Legendary" }
        };
        var b = new ArchlichSovereign(null, items);
        b.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void ArchlichSovereign_StatsCtor_ItemConfig_AlternateNameSoulGem()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "SoulGem", Type = "Accessory", StatModifier = 10, IsEquippable = true, Tier = "Legendary" }
        };
        var b = new ArchlichSovereign(null, items);
        b.LootTable.Should().NotBeNull();
    }
}
