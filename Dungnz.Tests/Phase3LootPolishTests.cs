using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Proactive tests for Phase 3 Looting UX improvements.
/// Covers: consumable grouping in ShowInventory (3.1), elite loot callouts in ShowLootDrop (3.2),
/// weight warnings in ShowItemPickup (3.3), and new-best "vs equipped" indicators (3.4).
///
/// Tests that exercise ConsoleDisplayService capture Console.Out directly.
/// Tests that only need call-recording use FakeDisplayService.
/// </summary>
[Collection("console-output")]
public class Phase3LootPolishTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    public Phase3LootPolishTests()
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
    // 3.1 — Consumable Grouping in ShowInventory
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowInventory_ThreeIdenticalPotions_ShowsTimesThreeMultiplier()
    {
        // Arrange
        var player = new Player();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        player.Inventory.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 });
        player.Inventory.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 });

        // Act
        _svc.ShowInventory(player);

        // Assert
        Output.Should().Contain("×3", because: "three identical potions must be grouped and show ×3");
    }

    [Fact]
    public void ShowInventory_DifferentNamedItems_StaySeparate()
    {
        // Arrange
        var player = new Player();
        player.Inventory.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 });
        player.Inventory.Add(new Item { Name = "Mana Potion", Type = ItemType.Consumable, ManaRestore = 20 });

        // Act
        _svc.ShowInventory(player);

        // Assert — both names appear, no ×2 multiplier (they have different names)
        Output.Should().Contain("Health Potion");
        Output.Should().Contain("Mana Potion");
        Output.Should().NotContain("×2", because: "items with different names must not be grouped together");
    }

    [Fact]
    public void ShowInventory_SinglePotion_ShowsNoMultiplier()
    {
        // Arrange
        var player = new Player();
        player.Inventory.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 });

        // Act
        _svc.ShowInventory(player);

        // Assert — no × symbol appears for a single item
        Output.Should().NotContain("×", because: "a single item should not show a multiplier badge");
    }

    [Fact]
    public void ShowInventory_EmptyInventory_ShowsNoGroupingArtifacts()
    {
        // Arrange
        var player = new Player(); // empty inventory

        // Act
        _svc.ShowInventory(player);

        // Assert — no stray × characters, no count badges
        Output.Should().NotContain("×", because: "empty inventory must produce no grouping artifacts");
        Output.Should().Contain("empty", because: "empty inventory must show an empty-state message");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3.2 — Elite Loot Callout in ShowLootDrop
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowLootDrop_IsEliteTrue_OutputContainsEliteLootDrop()
    {
        // Arrange
        var item = new Item { Name = "Mythril Blade", Type = ItemType.Weapon, AttackBonus = 8, Tier = ItemTier.Rare };
        var player = new Player();

        // Act
        _svc.ShowLootDrop(item, player, isElite: true);

        // Assert
        Output.Should().Contain("ELITE LOOT DROP",
            because: "elite kills must surface a distinct 'ELITE LOOT DROP' header");
    }

    [Fact]
    public void ShowLootDrop_IsEliteFalse_OutputContainsLootDropButNotElite()
    {
        // Arrange
        var item = new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, Tier = ItemTier.Common };
        var player = new Player();

        // Act
        _svc.ShowLootDrop(item, player, isElite: false);

        // Assert
        Output.Should().Contain("LOOT DROP",
            because: "normal loot drops must still show a LOOT DROP header");
        Output.Should().NotContain("ELITE",
            because: "the word ELITE must not appear for non-elite loot drops");
    }

    [Fact]
    public void ShowLootDrop_UncommonItem_OutputContainsUncommonBadge()
    {
        // Arrange
        var item = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Tier = ItemTier.Uncommon };
        var player = new Player();

        // Act
        _svc.ShowLootDrop(item, player);

        // Assert
        Output.Should().Contain("Uncommon",
            because: "Uncommon tier must be surfaced in the loot drop display");
    }

    [Fact]
    public void ShowLootDrop_RareItem_OutputContainsRareBadge()
    {
        // Arrange
        var item = new Item { Name = "Mythril Blade", Type = ItemType.Weapon, AttackBonus = 8, Tier = ItemTier.Rare };
        var player = new Player();

        // Act
        _svc.ShowLootDrop(item, player);

        // Assert
        Output.Should().Contain("Rare",
            because: "Rare tier must be surfaced in the loot drop display");
    }

    [Fact]
    public void ShowLootDrop_CommonItem_OutputContainsCommonBadge()
    {
        // Arrange
        var item = new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, Tier = ItemTier.Common };
        var player = new Player();

        // Act
        _svc.ShowLootDrop(item, player);

        // Assert
        Output.Should().Contain("Common",
            because: "Common tier must still be labeled so all tiers are represented");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3.3 — Weight Warning in ShowItemPickup
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowItemPickup_At85PercentWeight_ShowsWeightWarning()
    {
        // Arrange — 85% of MaxWeight (50) = 42.5, use 43
        var item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Weight = 1 };
        int maxWeight = InventoryManager.MaxWeight; // 50
        int currentWeight = (int)(maxWeight * 0.85); // 42

        // Act
        _svc.ShowItemPickup(item, slotsCurrent: 5, slotsMax: 20, weightCurrent: currentWeight, weightMax: maxWeight);

        // Assert
        Output.Should().Contain("⚠",
            because: "inventory at 85% weight capacity must trigger a visual warning");
        Output.Should().Contain("nearly full",
            because: "the warning message must tell the player the inventory is nearly full");
    }

    [Fact]
    public void ShowItemPickup_At79PercentWeight_ShowsNoWeightWarning()
    {
        // Arrange — 79% of MaxWeight (50) = 39.5, use 39
        var item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Weight = 1 };
        int maxWeight = InventoryManager.MaxWeight; // 50
        int currentWeight = (int)(maxWeight * 0.79); // 39

        // Act
        _svc.ShowItemPickup(item, slotsCurrent: 3, slotsMax: 20, weightCurrent: currentWeight, weightMax: maxWeight);

        // Assert
        Output.Should().NotContain("⚠",
            because: "inventory below 80% weight must not show the weight warning");
        Output.Should().NotContain("nearly full",
            because: "the nearly-full message must not appear below the warning threshold");
    }

    [Fact]
    public void ShowItemPickup_AtExactly80PercentWeight_ShowsWeightWarning()
    {
        // Arrange — exactly 80% of MaxWeight (50) = 40
        // The boundary condition: > 0.8 means 40 is NOT > 0.8 (40/50 = 0.8 exactly).
        // The current implementation uses weightCurrent > weightMax * 0.8 (strict greater-than),
        // so weight=40 (exactly 80%) does NOT trigger the warning. This test documents that
        // boundary. If Hill changes to >= 0.8 (inclusive), update this test to expect the warning.
        var item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Weight = 1 };
        int maxWeight = InventoryManager.MaxWeight; // 50
        int currentWeight = 40; // exactly 80%

        // Act
        _svc.ShowItemPickup(item, slotsCurrent: 4, slotsMax: 20, weightCurrent: currentWeight, weightMax: maxWeight);

        // Assert — boundary is exclusive (> 0.8), so exact 80% shows no warning
        Output.Should().NotContain("⚠",
            because: "the warning threshold uses strict greater-than (> 80%), so exactly 80% must not trigger it");
    }

    [Fact]
    public void ShowItemPickup_AtJustOver80PercentWeight_ShowsWeightWarning()
    {
        // Arrange — 41/50 = 82% triggers the > 0.8 boundary
        var item = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Weight = 1 };
        int maxWeight = InventoryManager.MaxWeight; // 50
        int currentWeight = 41; // 82%

        // Act
        _svc.ShowItemPickup(item, slotsCurrent: 4, slotsMax: 20, weightCurrent: currentWeight, weightMax: maxWeight);

        // Assert
        Output.Should().Contain("⚠",
            because: "41/50 weight (82%) exceeds the 80% threshold and must show the warning");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3.4 — New Best "vs Equipped" Indicator in ShowLootDrop
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowLootDrop_NewWeaponBetterThanEquipped_ShowsPositiveDelta()
    {
        // Arrange — drop has Attack +5, player has Attack +2 equipped → delta = +3
        var droppedSword = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Tier = ItemTier.Uncommon };
        var equippedSword = new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, IsEquippable = true };
        var player = new Player();
        player.EquippedWeapon = equippedSword;

        // Act
        _svc.ShowLootDrop(droppedSword, player);

        // Assert
        Output.Should().Contain("+3",
            because: "the delta between new weapon (5) and equipped weapon (2) is +3");
        Output.Should().Contain("vs equipped",
            because: "the upgrade indicator must tell the player this is better than their current weapon");
    }

    [Fact]
    public void ShowLootDrop_NewWeaponSameAsEquipped_ShowsNoVsEquipped()
    {
        // Arrange — drop has Attack +5, player has Attack +5 equipped → delta = 0, no improvement
        var droppedSword = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Tier = ItemTier.Uncommon };
        var equippedSword = new Item { Name = "Another Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        var player = new Player();
        player.EquippedWeapon = equippedSword;

        // Act
        _svc.ShowLootDrop(droppedSword, player);

        // Assert
        Output.Should().NotContain("vs equipped",
            because: "no improvement over equipped weapon means no upgrade indicator");
    }

    [Fact]
    public void ShowLootDrop_NewWeaponWeakerThanEquipped_ShowsNoVsEquipped()
    {
        // Arrange — drop has Attack +3, player has Attack +5 equipped → delta = -2, downgrade
        var droppedSword = new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 3, Tier = ItemTier.Common };
        var equippedSword = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        var player = new Player();
        player.EquippedWeapon = equippedSword;

        // Act
        _svc.ShowLootDrop(droppedSword, player);

        // Assert
        Output.Should().NotContain("vs equipped",
            because: "a downgrade must never show the upgrade indicator");
    }

    [Fact]
    public void ShowLootDrop_PlayerHasNoWeaponEquipped_ShowsNoVsEquipped()
    {
        // Arrange — player has no weapon in slot
        var droppedSword = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, Tier = ItemTier.Uncommon };
        var player = new Player(); // EquippedWeapon is null

        // Act
        _svc.ShowLootDrop(droppedSword, player);

        // Assert
        Output.Should().NotContain("vs equipped",
            because: "with nothing equipped there is no comparison to make");
    }
}
