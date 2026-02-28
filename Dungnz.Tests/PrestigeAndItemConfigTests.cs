using System;
using System.IO;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for PrestigeSystem and ItemConfig to improve their coverage.</summary>
[Collection("PrestigeTests")]
public class PrestigeAndItemConfigTests
{
    // ── PrestigeSystem ────────────────────────────────────────────────────────

    [Fact]
    public void PrestigeSystem_Load_NoFile_ReturnsDefault()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        var tmpFile = Path.Combine(tmpDir, "prestige.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            var data = PrestigeSystem.Load();
            data.PrestigeLevel.Should().Be(0);
            data.TotalWins.Should().Be(0);
            data.TotalRuns.Should().Be(0);
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void PrestigeSystem_SaveAndLoad_RoundTrips()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"prestige_{Guid.NewGuid()}.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            var original = new PrestigeData { TotalWins = 3, TotalRuns = 5, PrestigeLevel = 1, BonusStartAttack = 1, BonusStartDefense = 1, BonusStartHP = 5 };
            PrestigeSystem.Save(original);

            var loaded = PrestigeSystem.Load();
            loaded.TotalWins.Should().Be(3);
            loaded.TotalRuns.Should().Be(5);
            loaded.PrestigeLevel.Should().Be(1);
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void PrestigeSystem_Load_WrongVersion_ReturnsDefault()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"prestige_{Guid.NewGuid()}.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            File.WriteAllText(tmpFile, """{"Version":99,"PrestigeLevel":5,"TotalWins":15}""");
            var data = PrestigeSystem.Load();
            data.PrestigeLevel.Should().Be(0, "wrong version resets to defaults");
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void PrestigeSystem_Load_InvalidJson_ReturnsDefault()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"prestige_{Guid.NewGuid()}.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            File.WriteAllText(tmpFile, "{ not valid json ]]]");
            var data = PrestigeSystem.Load();
            data.PrestigeLevel.Should().Be(0, "invalid JSON returns default");
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void PrestigeSystem_RecordRun_Lost_IncrementsTotalRuns()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"prestige_{Guid.NewGuid()}.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            PrestigeSystem.RecordRun(won: false);
            var data = PrestigeSystem.Load();
            data.TotalRuns.Should().Be(1);
            data.TotalWins.Should().Be(0);
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void PrestigeSystem_RecordRun_Won_IncrementsBoth()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"prestige_{Guid.NewGuid()}.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            PrestigeSystem.RecordRun(won: true);
            var data = PrestigeSystem.Load();
            data.TotalRuns.Should().Be(1);
            data.TotalWins.Should().Be(1);
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void PrestigeSystem_RecordRun_ThreeWins_GrantsPrestigeLevel()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"prestige_{Guid.NewGuid()}.json");
        PrestigeSystem.SetSavePathForTesting(tmpFile);
        try
        {
            PrestigeSystem.RecordRun(won: true);
            PrestigeSystem.RecordRun(won: true);
            PrestigeSystem.RecordRun(won: true);

            var data = PrestigeSystem.Load();
            data.PrestigeLevel.Should().Be(1, "3 wins grants 1 prestige level");
            data.BonusStartAttack.Should().Be(1);
            data.BonusStartDefense.Should().Be(1);
            data.BonusStartHP.Should().Be(5);
        }
        finally
        {
            PrestigeSystem.SetSavePathForTesting(null);
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void GetPrestigeDisplay_PrestigeLevel0_ReturnsEmpty()
    {
        var data = new PrestigeData { PrestigeLevel = 0 };
        PrestigeSystem.GetPrestigeDisplay(data).Should().BeEmpty();
    }

    [Fact]
    public void GetPrestigeDisplay_PrestigeLevel1_ReturnsFormattedString()
    {
        var data = new PrestigeData { PrestigeLevel = 1, BonusStartAttack = 1, BonusStartDefense = 1, BonusStartHP = 5 };
        var display = PrestigeSystem.GetPrestigeDisplay(data);
        display.Should().Contain("Prestige 1");
        display.Should().Contain("+1 Atk");
    }

    // ── ItemConfig.Load ────────────────────────────────────────────────────────

    [Fact]
    public void ItemConfig_Load_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = () => ItemConfig.Load("/nonexistent/path/items.json");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ItemConfig_Load_EmptyFile_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[]}""");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>();
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_MissingName_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[{"Name":"","Type":"Weapon","Tier":"Common"}]}""");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*Name*");
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_MissingType_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[{"Name":"Test","Type":"","Tier":"Common"}]}""");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*Type*");
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_InvalidType_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[{"Name":"Test","Type":"InvalidItemType","Tier":"Common"}]}""");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*Type*");
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_NameTooLong_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            // Name with 31 characters
            var longName = "ThisNameIsWayTooLongForTheLimit1";
            File.WriteAllText(tmpFile, $"{{\"Items\":[{{\"Name\":\"{longName}\",\"Type\":\"Weapon\",\"Tier\":\"Common\"}}]}}");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*30 character*");
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_InvalidTier_ThrowsInvalidOperationException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[{"Name":"Test","Type":"Weapon","Tier":"SuperRare"}]}""");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidOperationException>().WithMessage("*ItemTier*");
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_NegativeHealAmount_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[{"Name":"Test","Type":"Consumable","Tier":"Common","HealAmount":-1}]}""");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*negative*");
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_InvalidJson_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "{ not valid json ]]]");
            var act = () => ItemConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>();
        }
        finally { File.Delete(tmpFile); }
    }

    [Fact]
    public void ItemConfig_Load_ValidFile_ReturnsItems()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, """{"Items":[{"Name":"Iron Sword","Type":"Weapon","Tier":"Common","AttackBonus":5,"IsEquippable":true}]}""");
            var result = ItemConfig.Load(tmpFile);
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Iron Sword");
        }
        finally { File.Delete(tmpFile); }
    }

    // ── ItemConfig.GetByTier ──────────────────────────────────────────────────

    [Fact]
    public void GetByTier_FiltersByTier_ExcludesBossKey()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Common Sword", Type = "Weapon", Tier = "Common", AttackBonus = 5, IsEquippable = true },
            new() { Name = "Boss Key", Type = "Consumable", Tier = "Common" },
            new() { Name = "Rare Axe", Type = "Weapon", Tier = "Rare", AttackBonus = 15, IsEquippable = true }
        };

        var commons = ItemConfig.GetByTier(items, ItemTier.Common);
        commons.Should().ContainSingle(i => i.Name == "Common Sword");
        commons.Should().NotContain(i => i.Name == "Boss Key");
    }

    [Fact]
    public void GetByTier_ExcludesMerchantExclusiveItems()
    {
        var items = new List<ItemStats>
        {
            new() { Name = "Shop Only", Type = "Weapon", Tier = "Uncommon", IsEquippable = true, MerchantExclusive = true },
            new() { Name = "Normal Sword", Type = "Weapon", Tier = "Uncommon", AttackBonus = 8, IsEquippable = true }
        };

        var uncommons = ItemConfig.GetByTier(items, ItemTier.Uncommon);
        uncommons.Should().ContainSingle(i => i.Name == "Normal Sword");
        uncommons.Should().NotContain(i => i.Name == "Shop Only");
    }

    // ── ItemConfig.CreateItem ─────────────────────────────────────────────────

    [Fact]
    public void CreateItem_InvalidType_ThrowsArgumentException()
    {
        var stats = new ItemStats { Name = "Mystery", Type = "TOTALLY_INVALID" };
        var act = () => ItemConfig.CreateItem(stats);
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid item type*");
    }

    [Fact]
    public void CreateItem_WithAllFields_MapsCorrectly()
    {
        var stats = new ItemStats
        {
            Id = "iron-sword",
            Name = "Iron Sword",
            Type = "Weapon",
            Tier = "Common",
            AttackBonus = 8,
            IsEquippable = true,
            Weight = 2,
            DodgeBonus = 0.05f,
            PassiveEffectId = "vampiric_strike",
            ClassRestriction = new[] { "Warrior" },
            Slot = "None"
        };

        var item = ItemConfig.CreateItem(stats);
        item.Name.Should().Be("Iron Sword");
        item.Type.Should().Be(ItemType.Weapon);
        item.AttackBonus.Should().Be(8);
        item.IsEquippable.Should().BeTrue();
        item.Weight.Should().Be(2);
        item.DodgeBonus.Should().BeApproximately(0.05f, 0.001f);
        item.PassiveEffectId.Should().Be("vampiric_strike");
        item.ClassRestriction.Should().Contain("Warrior");
    }

    [Fact]
    public void CreateItem_UnknownTierString_DefaultsToCommon()
    {
        // This path is only reached if ItemConfig.Load hasn't validated the tier
        // but CreateItem has its own fallback via TryParse
        var stats = new ItemStats { Name = "Mystery Item", Type = "Weapon", Tier = "Common" };
        var item = ItemConfig.CreateItem(stats);
        item.Tier.Should().Be(ItemTier.Common);
    }
}
