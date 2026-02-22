using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Proactive tests for Phase 1 looting UI improvements.
/// Covers ShowLootDrop (type icons + primary stats), ShowGoldPickup, ShowItemPickup,
/// and FakeDisplayService call recording. ItemTier tests are commented out pending Phase 2.0.
/// </summary>
[Collection("console-output")]
public class LootDisplayTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    public LootDisplayTests()
    {
        _originalOut = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
        _svc = new ConsoleDisplayService();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    private string Output => _output.ToString();

    // ─────────────────────────────────────────────────────────────────────────
    // ShowLootDrop — type icons
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ItemType.Weapon,     "⚔")]
    [InlineData(ItemType.Armor,      "🛡")]
    [InlineData(ItemType.Consumable, "🧪")]
    [InlineData(ItemType.Accessory,  "💍")]
    public void ShowLootDrop_EachItemType_ShowsCorrectIcon(ItemType type, string expectedIcon)
    {
        var item = new Item { Name = "Test Item", Type = type };
        _svc.ShowLootDrop(item);
        Output.Should().Contain(expectedIcon);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowLootDrop — primary stat labels
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowLootDrop_Weapon_ShowsAttackBonus()
    {
        var item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 7 };
        _svc.ShowLootDrop(item);
        Output.Should().Contain("Attack +7");
    }

    [Fact]
    public void ShowLootDrop_Armor_ShowsDefenseBonus()
    {
        var item = new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 4 };
        _svc.ShowLootDrop(item);
        Output.Should().Contain("Defense +4");
    }

    [Fact]
    public void ShowLootDrop_Consumable_ShowsHealAmount()
    {
        var item = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        _svc.ShowLootDrop(item);
        Output.Should().Contain("Heals 20 HP");
    }

    [Fact]
    public void ShowLootDrop_AlwaysShowsItemName()
    {
        var item = new Item { Name = "Blessed Blade", Type = ItemType.Weapon, AttackBonus = 5 };
        _svc.ShowLootDrop(item);
        Output.Should().Contain("Blessed Blade");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowLootDrop — edge cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowLootDrop_ZeroStats_ShowsTypeName()
    {
        // Edge case: item with no meaningful stats — PrimaryStatLabel falls back to type name
        var item = new Item
        {
            Name = "Strange Relic", Type = ItemType.Accessory,
            AttackBonus = 0, DefenseBonus = 0, HealAmount = 0, Weight = 3
        };
        _svc.ShowLootDrop(item);
        Output.Should().Contain("Accessory");
        Output.Should().Contain("3");  // weight is always shown
    }

    [Fact]
    public void ShowLootDrop_ItemWeight_IsAlwaysShown()
    {
        var item = new Item { Name = "Heavy Axe", Type = ItemType.Weapon, AttackBonus = 10, Weight = 5 };
        _svc.ShowLootDrop(item);
        Output.Should().Contain("5");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowGoldPickup — amount and running total
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowGoldPickup_ShowsPickedUpAmount()
    {
        _svc.ShowGoldPickup(15, 50);
        Output.Should().Contain("15");
    }

    [Fact]
    public void ShowGoldPickup_ShowsRunningTotal()
    {
        _svc.ShowGoldPickup(15, 50);
        Output.Should().Contain("50");
    }

    [Fact]
    public void ShowGoldPickup_ZeroAmount_ShowsZeroAndTotal()
    {
        // Edge case: 0-gold pickup — display must still be valid
        _svc.ShowGoldPickup(0, 100);
        Output.Should().Contain("0");
        Output.Should().Contain("100");
    }

    [Fact]
    public void ShowGoldPickup_ShowsGoldSymbolOrLabel()
    {
        // Must identify itself as gold (emoji or label)
        _svc.ShowGoldPickup(25, 75);
        Output.Should().ContainAny("gold", "💰", "g");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowItemPickup — slot and weight display
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowItemPickup_ShowsSlotCount()
    {
        var item = new Item { Name = "New Sword", Type = ItemType.Weapon, Weight = 2 };
        _svc.ShowItemPickup(item, slotsCurrent: 5, slotsMax: 20, weightCurrent: 10, weightMax: 50);
        Output.Should().Contain("5/20");
    }

    [Fact]
    public void ShowItemPickup_ShowsCarryWeight()
    {
        var item = new Item { Name = "Shield", Type = ItemType.Armor, Weight = 3 };
        _svc.ShowItemPickup(item, slotsCurrent: 3, slotsMax: 20, weightCurrent: 8, weightMax: 50);
        Output.Should().Contain("8/50");
    }

    [Fact]
    public void ShowItemPickup_ShowsItemName()
    {
        var item = new Item { Name = "Crystal Orb", Type = ItemType.Accessory, Weight = 1 };
        _svc.ShowItemPickup(item, slotsCurrent: 2, slotsMax: 20, weightCurrent: 3, weightMax: 50);
        Output.Should().Contain("Crystal Orb");
    }

    [Fact]
    public void ShowItemPickup_AtSlotCapacity_UsesRedWarningColor()
    {
        // Edge case: inventory exactly full — ratio = 20/20 = 1.0 > 0.95 → Red ANSI
        var item = new Item { Name = "Last Item", Type = ItemType.Consumable, Weight = 0 };
        _svc.ShowItemPickup(item, slotsCurrent: 20, slotsMax: 20, weightCurrent: 1, weightMax: 50);
        Output.Should().Contain(ColorCodes.Red, because: "full slot count must be highlighted in red");
    }

    [Fact]
    public void ShowItemPickup_AtWeightCapacity_UsesRedWarningColor()
    {
        // Edge case: carry weight exactly at limit — ratio = 50/50 = 1.0 > 0.95 → Red ANSI
        var item = new Item { Name = "Heavy Sword", Type = ItemType.Weapon, Weight = 1 };
        _svc.ShowItemPickup(item, slotsCurrent: 1, slotsMax: 20, weightCurrent: 50, weightMax: 50);
        Output.Should().Contain(ColorCodes.Red, because: "full carry weight must be highlighted in red");
    }

    [Fact]
    public void ShowItemPickup_BelowCapacity_UsesGreenColor()
    {
        // Normal case: low usage — should use green (not red/yellow)
        var item = new Item { Name = "Small Dagger", Type = ItemType.Weapon, Weight = 1 };
        _svc.ShowItemPickup(item, slotsCurrent: 2, slotsMax: 20, weightCurrent: 3, weightMax: 50);
        Output.Should().Contain(ColorCodes.Green);
        Output.Should().NotContain(ColorCodes.Red);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FakeDisplayService — verifies mock records calls for integration tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FakeDisplayService_ShowLootDrop_RecordsItemName()
    {
        var fake = new FakeDisplayService();
        var item = new Item { Name = "Test Blade", Type = ItemType.Weapon, AttackBonus = 3 };
        fake.ShowLootDrop(item);
        fake.AllOutput.Should().ContainMatch("*Test Blade*");
    }

    [Fact]
    public void FakeDisplayService_ShowGoldPickup_RecordsAmountAndTotal()
    {
        var fake = new FakeDisplayService();
        fake.ShowGoldPickup(10, 40);
        fake.AllOutput.Should().ContainMatch("*10*40*");
    }

    [Fact]
    public void FakeDisplayService_ShowItemPickup_RecordsItemNameAndSlots()
    {
        var fake = new FakeDisplayService();
        var item = new Item { Name = "Shield", Type = ItemType.Armor, Weight = 2 };
        fake.ShowItemPickup(item, slotsCurrent: 3, slotsMax: 20, weightCurrent: 5, weightMax: 50);
        fake.AllOutput.Should().ContainMatch("*Shield*");
        fake.AllOutput.Should().ContainMatch("*3/20*");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ItemTier assignment tests
// Requires ItemTier — Phase 2.0
// These tests will NOT compile until the ItemTier enum is added to Item.cs
// and the Item model exposes a Tier property. Uncomment when Phase 2.0 lands.
// ─────────────────────────────────────────────────────────────────────────────
/*
public class ItemTierTests
{
    // Requires ItemTier — Phase 2.0
    [Fact]
    public void Item_DefaultTier_IsCommon()
    {
        var item = new Item { Name = "Plain Dagger", Type = ItemType.Weapon };
        item.Tier.Should().Be(ItemTier.Common);
    }

    // Requires ItemTier — Phase 2.0
    [Fact]
    public void Item_TierCanBeSetToRare()
    {
        var item = new Item { Name = "Silver Blade", Type = ItemType.Weapon, Tier = ItemTier.Rare };
        item.Tier.Should().Be(ItemTier.Rare);
    }

    // Requires ItemTier — Phase 2.0
    [Fact]
    public void Item_TierCanBeSetToEpic()
    {
        var item = new Item { Name = "Void Blade", Type = ItemType.Weapon, Tier = ItemTier.Epic };
        item.Tier.Should().Be(ItemTier.Epic);
    }

    // Requires ItemTier — Phase 2.0
    [Fact]
    public void Item_TierCanBeSetToLegendary()
    {
        var item = new Item { Name = "Dragonslayer", Type = ItemType.Weapon, Tier = ItemTier.Legendary };
        item.Tier.Should().Be(ItemTier.Legendary);
    }

    // Requires ItemTier — Phase 2.0
    [Theory]
    [InlineData(ItemTier.Common,    "Common")]
    [InlineData(ItemTier.Uncommon,  "Uncommon")]
    [InlineData(ItemTier.Rare,      "Rare")]
    [InlineData(ItemTier.Epic,      "Epic")]
    [InlineData(ItemTier.Legendary, "Legendary")]
    public void ShowLootDrop_AllTiers_ShowTierName(ItemTier tier, string tierName)
    {
        var output = new StringWriter();
        Console.SetOut(output);
        var svc = new ConsoleDisplayService();
        var item = new Item { Name = "Test Blade", Type = ItemType.Weapon, AttackBonus = 5, Tier = tier };
        svc.ShowLootDrop(item);
        output.ToString().Should().Contain(tierName);
    }

    // Requires ItemTier — Phase 2.0
    [Fact]
    public void ShowLootDrop_LegendaryItem_ShowsDistinctIndicator()
    {
        var output = new StringWriter();
        Console.SetOut(output);
        var svc = new ConsoleDisplayService();
        var item = new Item { Name = "Frostmourne", Type = ItemType.Weapon, AttackBonus = 15, Tier = ItemTier.Legendary };
        svc.ShowLootDrop(item);
        output.ToString().Should().ContainAny("Legendary", "★★★", "🟨");
    }
}
*/
