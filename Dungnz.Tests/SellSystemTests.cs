using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for the sell system: ComputeSellPrice formula and the sell flow via GameLoop.
/// </summary>
public class SellSystemTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static (Player player, Room room, FakeDisplayService display, GameLoop loop) MakeSellSetup(
        params string[] inputs)
    {
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var room = new Room { Description = "Market room" };
        room.Merchant = new Merchant { Name = "Old Hag" };

        var display = new FakeDisplayService();
        var combat = new Mock<ICombatEngine>();
        var reader = new FakeInputReader(inputs);
        var loop = new GameLoop(display, combat.Object, reader);

        return (player, room, display, loop);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. ComputeSellPrice formula
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeSellPrice_CommonItem_ReturnsFortyPercentOfBuyPrice()
    {
        // Common buy price = 15 + HealAmount + (AttackBonus + DefenseBonus) * 5
        // With no stats: 15 → sell = 15 * 40 / 100 = 6
        var item = new Item { Name = "Rusty Dagger", Type = ItemType.Weapon, Tier = ItemTier.Common };
        int expected = Math.Max(1, (15) * 40 / 100);

        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(expected);
    }

    [Fact]
    public void ComputeSellPrice_CommonItemWithStats_ReturnsFortyPercentOfBuyPrice()
    {
        // Common buy price = 15 + 0 + (3 + 2) * 5 = 15 + 25 = 40
        var item = new Item
        {
            Name = "Iron Sword",
            Type = ItemType.Weapon,
            Tier = ItemTier.Common,
            AttackBonus = 3,
            DefenseBonus = 2
        };
        int buyPrice = 15 + (3 + 2) * 5; // 40
        int expected = Math.Max(1, buyPrice * 40 / 100); // 16

        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(expected);
    }

    [Fact]
    public void ComputeSellPrice_UncommonItem_ReturnsFortyPercentOfBuyPrice()
    {
        // Uncommon buy price = 40 + HealAmount + (AttackBonus + DefenseBonus) * 6
        // With AttackBonus=2: 40 + 12 = 52 → sell = 52 * 40 / 100 = 20
        var item = new Item
        {
            Name = "Silver Blade",
            Type = ItemType.Weapon,
            Tier = ItemTier.Uncommon,
            AttackBonus = 2
        };
        int buyPrice = 40 + 2 * 6; // 52
        int expected = Math.Max(1, buyPrice * 40 / 100); // 20

        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(expected);
    }

    [Fact]
    public void ComputeSellPrice_RareItem_ReturnsFortyPercentOfBuyPrice()
    {
        // Rare buy price = 80 + HealAmount + (AttackBonus + DefenseBonus) * 8
        // With DefenseBonus=5: 80 + 40 = 120 → sell = 120 * 40 / 100 = 48
        var item = new Item
        {
            Name = "Dragon Scale",
            Type = ItemType.Armor,
            Tier = ItemTier.Rare,
            DefenseBonus = 5
        };
        int buyPrice = 80 + 5 * 8; // 120
        int expected = Math.Max(1, buyPrice * 40 / 100); // 48

        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(expected);
    }

    [Fact]
    public void ComputeSellPrice_ExplicitSellPrice_UsesExplicitValueIgnoresFormula()
    {
        var item = new Item
        {
            Name = "Magic Ring",
            Type = ItemType.Accessory,
            Tier = ItemTier.Rare,
            AttackBonus = 10,
            SellPrice = 75
        };

        // Formula would give: (80 + 10*8) * 40/100 = 640*40/100 = 64, but explicit is 75
        MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(75);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. Sell flow — happy path
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sell_HappyPath_RemovesItemAndIncreasesGold()
    {
        var (player, room, display, loop) = MakeSellSetup("sell", "1", "Y", "quit");
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, Tier = ItemTier.Common };
        player.Inventory.Add(potion);

        int expectedPrice = MerchantInventoryConfig.ComputeSellPrice(potion);

        loop.Run(player, room);

        player.Inventory.Should().NotContain(potion);
        player.Gold.Should().Be(expectedPrice);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. Equipped items excluded
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sell_EquippedWeaponNotInInventory_OnlyUnequippedItemAppears()
    {
        // Weapon is in EquippedWeapon slot (not Inventory); only potion is in Inventory.
        // Selecting "1" should sell the potion, leaving the weapon still equipped.
        var (player, room, display, loop) = MakeSellSetup("sell", "1", "Y", "quit");

        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Tier = ItemTier.Common, IsEquippable = true };
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, Tier = ItemTier.Common };

        player.EquippedWeapon = sword; // in equipment slot, NOT in Inventory
        player.Inventory.Add(potion);

        int expectedGold = MerchantInventoryConfig.ComputeSellPrice(potion);

        loop.Run(player, room);

        player.Inventory.Should().NotContain(potion, "potion should have been sold");
        player.EquippedWeapon.Should().Be(sword, "equipped weapon must not be sold");
        player.Gold.Should().Be(expectedGold);
    }

    [Fact]
    public void Sell_OnlyEquippedWeapon_NoInventoryItems_ShowsNoSellNarration()
    {
        // Only equipped weapon — Inventory is empty → a "NoSell" narration line is shown
        var (player, room, display, loop) = MakeSellSetup("sell", "quit");

        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Tier = ItemTier.Common, IsEquippable = true };
        player.EquippedWeapon = sword;

        loop.Run(player, room);

        display.Messages.Should().Contain(m => MerchantNarration.NoSell.Contains(m),
            "a no-sell narration line should be shown when inventory has nothing sellable");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. Gold-type items not sellable
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sell_OnlyGoldTypeItems_ShowsNoSellNarration()
    {
        var (player, room, display, loop) = MakeSellSetup("sell", "quit");
        var coinBag = new Item { Name = "Coin Bag", Type = ItemType.Gold };
        player.Inventory.Add(coinBag);

        loop.Run(player, room);

        display.Messages.Should().Contain(m => MerchantNarration.NoSell.Contains(m),
            "a no-sell narration line should be shown when only Gold items are in inventory");
        player.Inventory.Should().Contain(coinBag, "gold-type item should not be consumed");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. No merchant in room
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sell_NoMerchantInRoom_ShowsErrorAndInventoryUnchanged()
    {
        var display = new FakeDisplayService();
        var combat = new Mock<ICombatEngine>();
        var reader = new FakeInputReader("sell", "quit");
        var loop = new GameLoop(display, combat.Object, reader);

        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var room = new Room { Description = "Empty room" }; // no merchant
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        player.Inventory.Add(potion);

        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("no merchant"));
        player.Inventory.Should().Contain(potion, "inventory must be unchanged");
        player.Gold.Should().Be(0);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. Cancel sell
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sell_CancelWithN_ItemRemainsInInventoryGoldUnchanged()
    {
        var (player, room, display, loop) = MakeSellSetup("sell", "1", "N", "quit");
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, Tier = ItemTier.Common };
        player.Inventory.Add(potion);

        loop.Run(player, room);

        player.Inventory.Should().Contain(potion, "cancelled sell must not remove item");
        player.Gold.Should().Be(0, "no gold should be awarded on cancel");
        display.Messages.Should().Contain("Changed your mind.");
    }
}
