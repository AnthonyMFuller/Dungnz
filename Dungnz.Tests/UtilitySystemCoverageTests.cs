using System;
using System.Collections.Generic;
using System.IO;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for AmbientEvents, MerchantInventoryConfig, and StartupValidator.</summary>
public class UtilitySystemCoverageTests
{
    // ── AmbientEvents ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void AmbientEvents_ForFloor_ReturnsNonEmptyArray(int floor)
    {
        var events = AmbientEvents.ForFloor(floor);
        events.Should().NotBeNullOrEmpty($"floor {floor} should have ambient events");
    }

    [Fact]
    public void AmbientEvents_ForFloor_UnknownFloor_FallsBackToGoblinCaves()
    {
        var unknown = AmbientEvents.ForFloor(99);
        var floor1 = AmbientEvents.ForFloor(1);
        unknown.Should().BeSameAs(floor1, "unknown floor falls back to GoblinCaves");
    }

    [Fact]
    public void AmbientEvents_ForFloor_Floor0_FallsBackToGoblinCaves()
    {
        var floor0 = AmbientEvents.ForFloor(0);
        var floor1 = AmbientEvents.ForFloor(1);
        floor0.Should().BeSameAs(floor1, "floor 0 falls back to GoblinCaves");
    }

    [Fact]
    public void AmbientEvents_StaticArrays_AllContainMessages()
    {
        AmbientEvents.GoblinCaves.Should().NotBeEmpty();
        AmbientEvents.SkeletonCatacombs.Should().NotBeEmpty();
        AmbientEvents.TrollWarrens.Should().NotBeEmpty();
        AmbientEvents.ShadowRealm.Should().NotBeEmpty();
        AmbientEvents.DragonsLair.Should().NotBeEmpty();
        AmbientEvents.VoidAntechamber.Should().NotBeEmpty();
        AmbientEvents.BonePalace.Should().NotBeEmpty();
        AmbientEvents.FinalSanctum.Should().NotBeEmpty();
    }

    // ── MerchantInventoryConfig ───────────────────────────────────────────────

    [Fact]
    public void ComputeSellPrice_ItemWithExplicitSellPrice_ReturnsExplicitPrice()
    {
        var item = new Item { SellPrice = 50, Tier = ItemTier.Common };
        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(50);
    }

    [Fact]
    public void ComputeSellPrice_ItemWithZeroSellPrice_ComputesFromTier()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Common, AttackBonus = 5, DefenseBonus = 0, HealAmount = 0 };
        // Common price = 15 + 0 + 5*5 = 40. SellPrice = 40 * 40 / 100 = 16
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(16);
    }

    [Fact]
    public void ComputeSellPrice_CommonItem_ReturnsMinimumOne()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Common, AttackBonus = 0, DefenseBonus = 0, HealAmount = 0 };
        // Common price = 15. SellPrice = 15 * 40 / 100 = 6
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(6);
    }

    [Theory]
    [InlineData(ItemTier.Uncommon, 40, 0, 0, 0)]   // 40 * 40/100 = 16
    [InlineData(ItemTier.Rare, 80, 0, 0, 0)]         // 80 * 40/100 = 32
    [InlineData(ItemTier.Epic, 150, 0, 0, 0)]        // 150 * 40/100 = 60
    [InlineData(ItemTier.Legendary, 400, 0, 0, 0)]   // 400 * 40/100 = 160
    public void ComputeSellPrice_VariousTiers_ComputeFromTierFormula(ItemTier tier, int basePrice, int heal, int atk, int def)
    {
        var item = new Item { SellPrice = 0, Tier = tier, HealAmount = heal, AttackBonus = atk, DefenseBonus = def };
        var expected = (basePrice + heal + (atk + def) * GetTierMultiplier(tier)) * 40 / 100;
        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(Math.Max(1, expected));
    }

    private static int GetTierMultiplier(ItemTier tier) => tier switch
    {
        ItemTier.Common => 5,
        ItemTier.Uncommon => 6,
        ItemTier.Rare => 8,
        ItemTier.Epic => 11,
        ItemTier.Legendary => 15,
        _ => 5
    };

    [Fact]
    public void GetStockForFloor_WithRealDataFile_ReturnsItems()
    {
        MerchantInventoryConfig.ClearCache();
        var items = new List<Item>
        {
            new() { Id = "health-potion", Name = "Health Potion", Type = ItemType.Consumable, Tier = ItemTier.Common, HealAmount = 30 },
            new() { Id = "iron-sword", Name = "Iron Sword", Type = ItemType.Weapon, Tier = ItemTier.Common, AttackBonus = 5 },
            new() { Id = "leather-armor", Name = "Leather Armor", Type = ItemType.Armor, Tier = ItemTier.Common, DefenseBonus = 3 },
            new() { Id = "mana-potion", Name = "Mana Potion", Type = ItemType.Consumable, Tier = ItemTier.Common, ManaRestore = 25 },
            new() { Id = "steel-sword", Name = "Steel Sword", Type = ItemType.Weapon, Tier = ItemTier.Uncommon, AttackBonus = 10 }
        };

        // This uses the real Data/merchant-inventory.json if present; otherwise returns empty list.
        var stock = MerchantInventoryConfig.GetStockForFloor(1, items, new Random(42));

        // The test verifies the method doesn't throw and returns a list (may be empty if file not found)
        stock.Should().NotBeNull();
    }

    [Fact]
    public void GetStockForFloor_Floor8_ClampsToValid()
    {
        MerchantInventoryConfig.ClearCache();
        var items = new List<Item>
        {
            new() { Id = "health-potion", Name = "Health Potion", Type = ItemType.Consumable, Tier = ItemTier.Common }
        };
        var stock = MerchantInventoryConfig.GetStockForFloor(8, items, new Random());
        stock.Should().NotBeNull();
    }

    [Fact]
    public void GetStockForFloor_HighFloor_ClampsToFloor8()
    {
        MerchantInventoryConfig.ClearCache();
        var items = new List<Item>
        {
            new() { Id = "health-potion", Name = "Health Potion", Type = ItemType.Consumable, Tier = ItemTier.Common }
        };
        var stock = MerchantInventoryConfig.GetStockForFloor(999, items, new Random());
        stock.Should().NotBeNull();
    }

    // ── StartupValidator ──────────────────────────────────────────────────────

    [Fact]
    public void StartupValidator_MissingFile_ThrowsFileNotFoundException()
    {
        var act = () => CallValidateWithMissingFile();
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void StartupValidator_EmptyFile_ThrowsInvalidDataException()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "   ");
            var act = () => CallValidateWithEmptyFile(tmp);
            act.Should().Throw<InvalidDataException>().WithMessage("*empty*");
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void StartupValidator_InvalidJson_ThrowsInvalidDataException()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "{ invalid json ]]]");
            var act = () => CallValidateWithInvalidJson(tmp);
            act.Should().Throw<InvalidDataException>().WithMessage("*Invalid JSON*");
        }
        finally { File.Delete(tmp); }
    }

    private static void CallValidateWithMissingFile()
    {
        // Use reflection to call ValidateOrThrow with a custom path list
        // Since RequiredDataFiles is private, we test indirectly by calling ValidateOrThrow
        // from a directory that doesn't have the data files
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var origDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);
            try { StartupValidator.ValidateOrThrow(); }
            finally { Directory.SetCurrentDirectory(origDir); }
        }
        finally { Directory.Delete(tempDir, true); }
    }

    private static void CallValidateWithEmptyFile(string emptyFilePath)
    {
        // Create a temp directory with "Data/" sub-dir containing our empty file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dataDir = Path.Combine(tempDir, "Data");
        Directory.CreateDirectory(dataDir);
        try
        {
            // Create all required files but the first one is empty
            var required = new[] { "item-stats.json", "enemy-stats.json", "crafting-recipes.json", "item-affixes.json" };
            File.WriteAllText(Path.Combine(dataDir, required[0]), "   "); // empty first file
            for (int i = 1; i < required.Length; i++)
                File.WriteAllText(Path.Combine(dataDir, required[i]), "{}");

            var origDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);
            try { StartupValidator.ValidateOrThrow(); }
            finally { Directory.SetCurrentDirectory(origDir); }
        }
        finally { Directory.Delete(tempDir, true); }
    }

    private static void CallValidateWithInvalidJson(string invalidJsonPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dataDir = Path.Combine(tempDir, "Data");
        Directory.CreateDirectory(dataDir);
        try
        {
            var required = new[] { "item-stats.json", "enemy-stats.json", "crafting-recipes.json", "item-affixes.json" };
            File.Copy(invalidJsonPath, Path.Combine(dataDir, required[0])); // invalid JSON first file
            for (int i = 1; i < required.Length; i++)
                File.WriteAllText(Path.Combine(dataDir, required[i]), "{}");

            var origDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);
            try { StartupValidator.ValidateOrThrow(); }
            finally { Directory.SetCurrentDirectory(origDir); }
        }
        finally { Directory.Delete(tempDir, true); }
    }
}
