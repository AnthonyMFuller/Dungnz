using System;
using System.IO;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for miscellaneous low-coverage classes: DifficultySettings, MerchantInventoryConfig, EnemyConfig, StartupValidator.</summary>
public class MiscCoverageTests
{
    // ── DifficultySettings ────────────────────────────────────────────────────

    [Fact]
    public void DifficultySettings_Casual_HasWeakerEnemiesAndBetterLoot()
    {
        var settings = DifficultySettings.For(Difficulty.Casual);
        settings.EnemyStatMultiplier.Should().BeApproximately(0.7f, 0.001f);
        settings.LootDropMultiplier.Should().BeApproximately(1.5f, 0.001f);
        settings.GoldMultiplier.Should().BeApproximately(1.5f, 0.001f);
        settings.Permadeath.Should().BeFalse();
    }

    [Fact]
    public void DifficultySettings_Normal_HasBalancedMultipliers()
    {
        var settings = DifficultySettings.For(Difficulty.Normal);
        settings.EnemyStatMultiplier.Should().BeApproximately(1.0f, 0.001f);
        settings.LootDropMultiplier.Should().BeApproximately(1.0f, 0.001f);
        settings.GoldMultiplier.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void DifficultySettings_Hard_HasStrongerEnemiesAndWorseLoot()
    {
        var settings = DifficultySettings.For(Difficulty.Hard);
        settings.EnemyStatMultiplier.Should().BeApproximately(1.3f, 0.001f);
        settings.LootDropMultiplier.Should().BeApproximately(0.7f, 0.001f);
        settings.GoldMultiplier.Should().BeApproximately(0.7f, 0.001f);
    }

    [Fact]
    public void DifficultySettings_AllDifficulties_CanBeConstructed()
    {
        foreach (Difficulty d in Enum.GetValues<Difficulty>())
        {
            var settings = DifficultySettings.For(d);
            settings.Should().NotBeNull();
        }
    }

    // ── MerchantInventoryConfig.ComputeSellPrice ─────────────────────────────

    [Fact]
    public void ComputeSellPrice_ExplicitSellPrice_UsesThatValue()
    {
        var item = new Item { SellPrice = 75, Tier = ItemTier.Common };
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(75);
    }

    [Fact]
    public void ComputeSellPrice_NoSellPrice_ComputesFromTier_Common()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Common, AttackBonus = 0, DefenseBonus = 0, HealAmount = 0 };
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        // ComputePrice for Common = 15 + 0 + 0 = 15; sell = max(1, 15 * 40 / 100) = max(1, 6) = 6
        price.Should().Be(6);
    }

    [Fact]
    public void ComputeSellPrice_NoSellPrice_Uncommon_ComputesCorrectly()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Uncommon, AttackBonus = 5, DefenseBonus = 0, HealAmount = 0 };
        // ComputePrice = 40 + 0 + 5*6 = 40 + 30 = 70; sell = max(1, 70 * 40 / 100) = max(1, 28) = 28
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(28);
    }

    [Fact]
    public void ComputeSellPrice_NoSellPrice_Rare_ComputesCorrectly()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Rare, AttackBonus = 0, DefenseBonus = 0, HealAmount = 0 };
        // ComputePrice = 80 + 0 + 0 = 80; sell = max(1, 80 * 40 / 100) = 32
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(32);
    }

    [Fact]
    public void ComputeSellPrice_NoSellPrice_Epic_ComputesCorrectly()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Epic, AttackBonus = 0, DefenseBonus = 0, HealAmount = 0 };
        // ComputePrice = 150; sell = max(1, 150 * 40 / 100) = 60
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(60);
    }

    [Fact]
    public void ComputeSellPrice_NoSellPrice_Legendary_ComputesCorrectly()
    {
        var item = new Item { SellPrice = 0, Tier = ItemTier.Legendary, AttackBonus = 0, DefenseBonus = 0, HealAmount = 0 };
        // ComputePrice = 400; sell = max(1, 400 * 40 / 100) = 160
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(160);
    }

    [Fact]
    public void ComputeSellPrice_NoSellPrice_UnknownTier_FallbackPrice()
    {
        // Unknown tier falls back to price = 20; sell = max(1, 20 * 40 / 100) = max(1, 8) = 8
        var item = new Item { SellPrice = 0, Tier = (ItemTier)999 };
        var price = MerchantInventoryConfig.ComputeSellPrice(item);
        price.Should().Be(8);
    }

    [Fact]
    public void ComputeSellPrice_MinimumIsOne()
    {
        // Item with all zero stats at common tier: 15 * 40 / 100 = 6, so > 1
        // Can't easily create a case where it would be zero, but we can verify min behavior
        var item = new Item { SellPrice = 0, Tier = ItemTier.Common };
        MerchantInventoryConfig.ComputeSellPrice(item).Should().BeGreaterThanOrEqualTo(1);
    }

    // ── MerchantFloorConfig record ────────────────────────────────────────────

    [Fact]
    public void MerchantFloorConfig_DefaultValues_AreValid()
    {
        var config = new MerchantFloorConfig();
        config.Floor.Should().Be(0);
        config.Guaranteed.Should().BeEmpty();
        config.Pool.Should().BeEmpty();
        config.StockCount.Should().Be(0);
    }

    [Fact]
    public void MerchantFloorConfig_InitProperties_SetCorrectly()
    {
        var config = new MerchantFloorConfig
        {
            Floor = 1,
            Guaranteed = new List<string> { "health-potion", "iron-sword" },
            Pool = new List<string> { "chain-mail", "leather-armor" },
            StockCount = 5
        };
        config.Floor.Should().Be(1);
        config.Guaranteed.Should().HaveCount(2);
        config.Pool.Should().HaveCount(2);
        config.StockCount.Should().Be(5);
    }

    // ── MerchantInventoryData record ──────────────────────────────────────────

    [Fact]
    public void MerchantInventoryData_DefaultValues_EmptyFloors()
    {
        var data = new MerchantInventoryData();
        data.Floors.Should().BeEmpty();
    }

    [Fact]
    public void MerchantInventoryData_InitWithFloors_StoresCorrectly()
    {
        var data = new MerchantInventoryData
        {
            Floors = new List<MerchantFloorConfig>
            {
                new MerchantFloorConfig { Floor = 1, StockCount = 3 },
                new MerchantFloorConfig { Floor = 2, StockCount = 4 }
            }
        };
        data.Floors.Should().HaveCount(2);
        data.Floors[0].Floor.Should().Be(1);
    }

    // ── EnemyConfig ───────────────────────────────────────────────────────────

    [Fact]
    public void EnemyConfig_Load_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = () => EnemyConfig.Load("/nonexistent/path/enemies.json");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void EnemyConfig_Load_EmptyFile_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "{}");  // empty JSON object = 0 entries
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>();
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_InvalidJson_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "{ not valid json ]]]");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>();
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_EntryWithMissingName_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": """", ""MaxHP"": 10, ""Attack"": 5, ""Defense"": 1, ""XPValue"": 5, ""MinGold"": 1, ""MaxGold"": 3 } }");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*Name*");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_EntryWithNegativeHP_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": ""Goblin"", ""MaxHP"": -5, ""Attack"": 5, ""Defense"": 1, ""XPValue"": 5, ""MinGold"": 1, ""MaxGold"": 3 } }");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*MaxHP*");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_EntryWithNegativeAttack_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": ""Goblin"", ""MaxHP"": 10, ""Attack"": -1, ""Defense"": 1, ""XPValue"": 5, ""MinGold"": 1, ""MaxGold"": 3 } }");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*Attack*");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_EntryWithNegativeDefense_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": ""Goblin"", ""MaxHP"": 10, ""Attack"": 5, ""Defense"": -1, ""XPValue"": 5, ""MinGold"": 1, ""MaxGold"": 3 } }");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*Defense*");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_EntryWithNegativeXPValue_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": ""Goblin"", ""MaxHP"": 10, ""Attack"": 5, ""Defense"": 1, ""XPValue"": -1, ""MinGold"": 1, ""MaxGold"": 3 } }");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*XPValue*");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_EntryWithInvalidGoldRange_ThrowsInvalidDataException()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            // MinGold > MaxGold
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": ""Goblin"", ""MaxHP"": 10, ""Attack"": 5, ""Defense"": 1, ""XPValue"": 5, ""MinGold"": 10, ""MaxGold"": 3 } }");
            var act = () => EnemyConfig.Load(tmpFile);
            act.Should().Throw<InvalidDataException>().WithMessage("*gold*");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void EnemyConfig_Load_ValidFile_ReturnsExpectedEntries()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, @"{ ""goblin"": { ""Name"": ""Goblin"", ""MaxHP"": 10, ""Attack"": 5, ""Defense"": 1, ""XPValue"": 5, ""MinGold"": 1, ""MaxGold"": 3 } }");
            var result = EnemyConfig.Load(tmpFile);
            result.Should().ContainKey("goblin");
            result["goblin"].Name.Should().Be("Goblin");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    // ── StartupValidator ──────────────────────────────────────────────────────

    [Fact]
    public void StartupValidator_ValidateOrThrow_MissingFile_ThrowsFileNotFoundException()
    {
        // Change directory temporarily to an empty temp folder
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var act = () => StartupValidator.ValidateOrThrow();
            act.Should().Throw<FileNotFoundException>().WithMessage("*Data/*");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, true);
        }
    }

    // ── CraftingSystem.Load → exercises RecipeConfigData ─────────────────────

    [Fact]
    public void CraftingSystem_Load_WithRealFile_LoadsRecipes()
    {
        // Call CraftingSystem.Load with the real data file to exercise RecipeConfigData
        // Record the current recipe count before and after
        var before = CraftingSystem.Recipes.Count;
        CraftingSystem.Load(); // uses default "Data/crafting-recipes.json" path
        CraftingSystem.Recipes.Count.Should().BeGreaterThan(0, "crafting data file should have at least one recipe");
    }

    [Fact]
    public void CraftingSystem_Load_NonExistentFile_DoesNotThrow()
    {
        // Covers the "if (!File.Exists(path)) return;" early exit path
        var recipesBeforeLoad = CraftingSystem.Recipes.Count;
        var act = () => CraftingSystem.Load("Data/nonexistent_file.json");
        act.Should().NotThrow("load should silently skip missing files");
        CraftingSystem.Recipes.Count.Should().Be(recipesBeforeLoad, "recipes should not change when file is missing");
    }

    [Fact]
    public void RunStats_GetTopRuns_ReturnsListOfRuns()
    {
        // Covers RunStats.GetTopRuns → LoadHistory
        var result = RunStats.GetTopRuns(5);
        result.Should().NotBeNull("GetTopRuns should return a list (possibly empty)");
    }

    [Fact]
    public void Player_ActiveTraps_DefaultIsEmpty()
    {
        // Covers Player.ActiveTraps property initialization
        var player = new Player { Name = "Test", HP = 100, MaxHP = 100 };
        player.ActiveTraps.Should().NotBeNull("ActiveTraps should default to an empty list");
        player.ActiveTraps.Should().BeEmpty();
    }
}

