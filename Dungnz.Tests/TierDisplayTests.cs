using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Proactive tests for Phase 2.1–2.4 (branch: feature/loot-display-phase2).
///
/// Tests marked "Requires Phase 2.x" will FAIL until Hill's implementation lands.
/// Tests NOT marked with a phase requirement should PASS on master today.
///
/// Phase mapping:
///   2.1 — ColorizeItemName: color-codes item names by ItemTier in ShowLootDrop / ShowInventory
///   2.2 — ShowShop: affordability color coding (commented out — not on IDisplayService yet)
///   2.3 — Tier display in ShowInventory (relies on 2.1 ColorizeItemName)
///   2.4 — ShowCraftRecipe: ingredient availability display (commented out — not on IDisplayService yet)
/// </summary>
[Collection("console-output")]
public class TierDisplayTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    /// <summary>
    /// Expected ANSI code for BrightCyan tier color.
    /// Hill's Phase 2.1 implementation will add ColorCodes.BrightCyan = "\u001b[96m".
    /// Update this constant if a different escape code is chosen.
    /// </summary>
    private const string BrightCyanAnsi = "\u001b[96m";

    public TierDisplayTests()
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
    // Phase 2.1 — ColorizeItemName via ShowLootDrop
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Common items must NOT be wrapped in BrightCyan (Rare color) or Green (Uncommon color)
    /// to avoid misleading the player about item rarity.
    /// PASSES on master today (no tier colorization exists yet).
    /// MUST STILL PASS after Phase 2.1 lands.
    /// </summary>
    [Fact]
    public void ShowLootDrop_CommonItem_DoesNotContainBrightCyan()
    {
        // Arrange
        var item = new Item { Name = "Rusty Dagger", Type = ItemType.Weapon, AttackBonus = 2, Tier = ItemTier.Common };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — BrightCyan must be absent; Common tier stays plain white
        Output.Should().NotContain(BrightCyanAnsi,
            because: "Common items must not use the Rare BrightCyan colour");
    }

    /// <summary>
    /// Common item name must NOT be wrapped in the tier-green color that Uncommon items use.
    /// Tests the specific pattern {green}{itemName} to distinguish tier green from other greens
    /// (e.g. the [E] equipped-tag is also green, so we check the name specifically).
    /// PASSES on master today.
    /// MUST STILL PASS after Phase 2.1 lands.
    /// </summary>
    [Fact]
    public void ShowLootDrop_CommonItem_ItemNameNotPrecededByGreen()
    {
        // Arrange
        var item = new Item { Name = "Iron Shield", Type = ItemType.Armor, DefenseBonus = 3, Tier = ItemTier.Common };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — the item name must not be directly preceded by the Uncommon green code
        Output.Should().NotContain($"{ColorCodes.Green}Iron Shield",
            because: "Common items must not use the Uncommon green colour on their name");
    }

    /// <summary>
    /// Uncommon items must have their name wrapped in Green (Uncommon tier color).
    /// Requires Phase 2.1 — FAILS on master until ColorizeItemName is implemented.
    /// </summary>
    [Fact]
    public void ShowLootDrop_UncommonItem_ItemNamePrecededByGreen()
    {
        // Arrange
        var item = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 8, Tier = ItemTier.Uncommon };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — Uncommon tier must colorize the item name in Green
        Output.Should().Contain($"{ColorCodes.Green}Steel Sword",
            because: "Uncommon items must display their name in Green");
    }

    /// <summary>
    /// Rare items must have their name wrapped in BrightCyan (Rare tier color).
    /// BrightCyan (\u001b[96m) is not currently used by any ConsoleDisplayService method,
    /// so its presence in the output is a clean signal that tier colorization is working.
    /// Requires Phase 2.1 — FAILS on master until ColorizeItemName is implemented.
    /// </summary>
    [Fact]
    public void ShowLootDrop_RareItem_OutputContainsBrightCyan()
    {
        // Arrange
        var item = new Item { Name = "Void Blade", Type = ItemType.Weapon, AttackBonus = 15, Tier = ItemTier.Rare };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — BrightCyan must appear in output for Rare items
        Output.Should().Contain(BrightCyanAnsi,
            because: "Rare items must display their name in BrightCyan");
    }

    /// <summary>
    /// Confirms the Rare item name itself is preceded by BrightCyan — not just that BrightCyan
    /// appears somewhere in the output for unrelated reasons.
    /// Requires Phase 2.1 — FAILS on master until ColorizeItemName is implemented.
    /// </summary>
    [Fact]
    public void ShowLootDrop_RareItem_ItemNamePrecededByBrightCyan()
    {
        // Arrange
        var item = new Item { Name = "Frostmourne", Type = ItemType.Weapon, AttackBonus = 20, Tier = ItemTier.Rare };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — the item name must be directly preceded by BrightCyan
        Output.Should().Contain($"{BrightCyanAnsi}Frostmourne",
            because: "Rare item name must be wrapped in BrightCyan color code");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2.3 — Tier display in ShowInventory
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Common items in inventory must not show BrightCyan on the item name.
    /// PASSES on master today.
    /// MUST STILL PASS after Phase 2.1/2.3 land.
    /// </summary>
    [Fact]
    public void ShowInventory_CommonItem_NoBrightCyanInOutput()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        player.Inventory.Add(new Item { Name = "Worn Boots", Type = ItemType.Armor, DefenseBonus = 1, Tier = ItemTier.Common });

        // Act
        _svc.ShowInventory(player);

        // Assert
        Output.Should().NotContain(BrightCyanAnsi,
            because: "Common items in inventory must not use BrightCyan");
    }

    /// <summary>
    /// Rare items in inventory must show BrightCyan wrapping their name.
    /// Requires Phase 2.1/2.3 — FAILS on master until tier colorization is added to ShowInventory.
    /// </summary>
    [Fact]
    public void ShowInventory_RareItem_OutputContainsBrightCyan()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        player.Inventory.Add(new Item { Name = "Arcane Staff", Type = ItemType.Weapon, AttackBonus = 12, Tier = ItemTier.Rare });

        // Act
        _svc.ShowInventory(player);

        // Assert — Rare items in inventory must show BrightCyan on the item name
        Output.Should().Contain(BrightCyanAnsi,
            because: "Rare items must display their name in BrightCyan in the inventory list");
    }

    /// <summary>
    /// Uncommon items in inventory must show Green wrapping their name.
    /// Requires Phase 2.1/2.3 — FAILS on master until tier colorization is added to ShowInventory.
    /// </summary>
    [Fact]
    public void ShowInventory_UncommonItem_ItemNamePrecededByGreen()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        var item = new Item { Name = "Shadow Cloak", Type = ItemType.Armor, DefenseBonus = 6, Tier = ItemTier.Uncommon };
        player.Inventory.Add(item);

        // Act
        _svc.ShowInventory(player);

        // Assert — Uncommon items in inventory must show Green on the name, not just elsewhere
        Output.Should().Contain($"{ColorCodes.Green}Shadow Cloak",
            because: "Uncommon items must display their name in Green in the inventory list");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FakeDisplayService — tier fields recorded via AllOutput
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FakeDisplayService must record the item name regardless of tier.
    /// Tests that the mock infrastructure works correctly for Rare items — the name
    /// should still appear in AllOutput even after Hill wires up ANSI colors.
    /// PASSES on master today and must continue to pass after Phase 2.1.
    /// </summary>
    [Fact]
    public void FakeDisplayService_ShowLootDrop_RareItem_RecordsItemName()
    {
        // Arrange
        var fake = new FakeDisplayService();
        var item = new Item { Name = "Dragon Scale", Type = ItemType.Armor, DefenseBonus = 10, Tier = ItemTier.Rare };

        // Act
        fake.ShowLootDrop(item, new Player());

        // Assert — item name always present in AllOutput regardless of ANSI wrapping
        fake.AllOutput.Should().ContainMatch("*Dragon Scale*");
    }

    /// <summary>
    /// FakeDisplayService ShowInventory must record the inventory count even for Rare items.
    /// Confirms no regression where tier wiring changes the FakeDisplayService behaviour.
    /// PASSES on master today.
    /// </summary>
    [Fact]
    public void FakeDisplayService_ShowInventory_RareItem_RecordsInventoryCount()
    {
        // Arrange
        var fake = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        player.Inventory.Add(new Item { Name = "Phoenix Feather", Type = ItemType.Accessory, Tier = ItemTier.Rare });

        // Act
        fake.ShowInventory(player);

        // Assert
        fake.AllOutput.Should().ContainMatch("inventory:1");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge cases
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Edge case: item with empty name must not crash ShowLootDrop.
    /// PASSES on master today; must remain stable after Phase 2.1 adds colorization.
    /// </summary>
    [Fact]
    public void ShowLootDrop_EmptyItemName_DoesNotThrow()
    {
        // Arrange — empty name is the closest valid substitute for null given Item.Name is non-nullable
        var item = new Item { Name = string.Empty, Type = ItemType.Weapon, AttackBonus = 5, Tier = ItemTier.Rare };

        // Act / Assert — must not throw
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow(because: "an empty item name must be handled gracefully");
    }

    /// <summary>
    /// Edge case: null item name (forced via null-forgiving operator).
    /// The display service must guard against null names and not throw NullReferenceException.
    /// PASSES on master today; once ColorizeItemName is added it must also guard null.
    /// </summary>
    [Fact]
    public void ShowLootDrop_NullItemName_DoesNotThrow()
    {
        // Arrange
        var item = new Item { Name = null!, Type = ItemType.Consumable, HealAmount = 10, Tier = ItemTier.Common };

        // Act / Assert
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow(because: "a null item name must not crash the display service");
    }

    /// <summary>
    /// Edge case: inventory with zero items must still render without crashing, regardless of tier
    /// colorization changes in Phase 2.1/2.3.
    /// PASSES on master today.
    /// </summary>
    [Fact]
    public void ShowInventory_EmptyInventory_DoesNotThrow()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50 };
        // Inventory intentionally empty

        // Act / Assert
        var act = () => _svc.ShowInventory(player);
        act.Should().NotThrow(because: "an empty inventory must display cleanly");
    }

    /// <summary>
    /// Edge case: item with every tier must not crash ShowLootDrop (parameterized).
    /// PASSES on master. Must still pass after Phase 2.1 adds per-tier color branching.
    /// </summary>
    [Theory]
    [InlineData(ItemTier.Common)]
    [InlineData(ItemTier.Uncommon)]
    [InlineData(ItemTier.Rare)]
    public void ShowLootDrop_AllTiers_DoNotThrow(ItemTier tier)
    {
        // Arrange
        var item = new Item { Name = "Test Item", Type = ItemType.Weapon, AttackBonus = 5, Tier = tier };

        // Act / Assert
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow(because: $"ShowLootDrop must handle ItemTier.{tier} without crashing");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 2.2 — ShowShop
// Requires Phase 2.2: ShowShop added to IDisplayService and ConsoleDisplayService
// ─────────────────────────────────────────────────────────────────────────────
[Collection("console-output")]
public class ShopDisplayTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    public ShopDisplayTests()
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

    // Requires Phase 2.2: ShowShop
    [Fact]
    public void ShowShop_PlayerCanAffordItem_OutputContainsGreen()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50, Gold = 100 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        var shopItems = new List<(Item, int)> { (sword, 50) };

        // Act
        _svc.ShowShop(shopItems, player.Gold);

        // Assert — affordable item name or price shown in green
        Output.Should().Contain(ColorCodes.Green,
            because: "items the player can afford must be highlighted in green");
    }

    // Requires Phase 2.2: ShowShop
    [Fact]
    public void ShowShop_PlayerCannotAffordItem_OutputContainsRedOrYellow()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50, Gold = 5 };
        var armor = new Item { Name = "Mythril Armor", Type = ItemType.Armor, DefenseBonus = 15 };
        var shopItems = new List<(Item, int)> { (armor, 200) };

        // Act
        _svc.ShowShop(shopItems, player.Gold);

        // Assert — unaffordable item price shown in red or yellow as warning
        Output.Should().ContainAny(new[] { ColorCodes.Red, ColorCodes.Yellow });
    }

    // Requires Phase 2.2: ShowShop
    [Fact]
    public void ShowShop_EmptyStock_DoesNotThrow()
    {
        // Arrange
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 50, Gold = 100 };
        var shopItems = new List<(Item, int)>(); // intentionally empty

        // Act / Assert
        var act = () => _svc.ShowShop(shopItems, player.Gold);
        act.Should().NotThrow(because: "an empty shop must display an empty-state message without crashing");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 2.4 — ShowCraftRecipe
// Requires Phase 2.4: ShowCraftRecipe added to IDisplayService and ConsoleDisplayService
// ─────────────────────────────────────────────────────────────────────────────
[Collection("console-output")]
public class CraftRecipeDisplayTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    public CraftRecipeDisplayTests()
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

    // Requires Phase 2.4: ShowCraftRecipe
    [Fact]
    public void ShowCraftRecipe_PlayerHasAllIngredients_OutputContainsCheckmark()
    {
        // Arrange
        var resultItem = new Item { Name = "Iron Sword", Type = ItemType.Weapon };
        var ingredients = new List<(string, bool)>
        {
            ("Iron Ore", true)
        };

        // Act
        _svc.ShowCraftRecipe("Iron Sword", resultItem, ingredients);

        // Assert — ✅ checkmark next to each ingredient the player has
        Output.Should().Contain("✅",
            because: "ingredients the player has must show the ✅ checkmark");
    }

    // Requires Phase 2.4: ShowCraftRecipe
    [Fact]
    public void ShowCraftRecipe_PlayerMissingIngredient_OutputContainsCross()
    {
        // Arrange
        var resultItem = new Item { Name = "Dragon Armor", Type = ItemType.Armor };
        var ingredients = new List<(string, bool)>
        {
            ("Dragon Scale", false)
        };

        // Act
        _svc.ShowCraftRecipe("Dragon Armor", resultItem, ingredients);

        // Assert — ❌ cross next to each missing ingredient
        Output.Should().Contain("❌",
            because: "missing ingredients must show the ❌ cross");
    }
}
