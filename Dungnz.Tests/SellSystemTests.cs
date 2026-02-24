using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dungnz.Tests;

public class SellSystemTests
{
    private static string ItemStatsPath =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "item-stats.json");

    // ── SellPrice from JSON ──────────────────────────────────────────────────

    [Fact]
    public void SellPrice_FromJson_UsesExplicitValue()
    {
        var stats = ItemConfig.Load(ItemStatsPath);
        var healthPotionStats = stats.First(s => s.Id == "health-potion");

        var item = ItemConfig.CreateItem(healthPotionStats);

        item.SellPrice.Should().Be(healthPotionStats.SellPrice);
        item.SellPrice.Should().Be(5);
    }

    // ── SellPrice fallback (~40% of buy price) ───────────────────────────────

    [Fact]
    public void SellPrice_FallbackCompute_IsFortyPercentOfBuyPrice()
    {
        var item = new Item { Name = "Test Sword", Type = ItemType.Weapon, Tier = ItemTier.Common, AttackBonus = 5 };

        var sellPrice = MerchantInventoryConfig.ComputeSellPrice(item);
        var buyPrice = MerchantInventoryConfig.ComputePrice(item);

        sellPrice.Should().Be(Math.Max(1, buyPrice * 40 / 100));
    }

    // ── ComputeSellPrice minimum guarantee ──────────────────────────────────

    [Fact]
    public void ComputeSellPrice_ReturnsAtLeastOneGold()
    {
        var item = new Item { Name = "Worthless Trinket", Type = ItemType.Consumable, Tier = ItemTier.Common };

        var sellPrice = MerchantInventoryConfig.ComputeSellPrice(item);

        sellPrice.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── GameLoop sell helpers ────────────────────────────────────────────────

    private static (Player player, Room room, FakeDisplayService display, GameLoop loop) MakeSellSetup(
        Item item, params string[] inputs)
    {
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        player.Inventory.Add(item);

        var merchant = new Merchant();
        var room = new Room { Description = "Shop room", Merchant = merchant };

        var display = new FakeDisplayService();
        var combat = new Mock<ICombatEngine>().Object;
        var reader = new FakeInputReader(inputs);
        var loop = new GameLoop(display, combat, reader);

        return (player, room, display, loop);
    }

    // ── Sell removes item from inventory ────────────────────────────────────

    [Fact]
    public void Sell_RemovesItemFromInventory()
    {
        var item = new Item { Name = "Iron Dagger", Type = ItemType.Weapon, Tier = ItemTier.Common, SellPrice = 10 };
        var (player, room, _, loop) = MakeSellSetup(item, "sell", "1");

        loop.Run(player, room);

        player.Inventory.Should().NotContain(item);
    }

    // ── Sell adds correct gold to player ────────────────────────────────────

    [Fact]
    public void Sell_AddsCorrectGoldToPlayer()
    {
        var item = new Item { Name = "Iron Dagger", Type = ItemType.Weapon, Tier = ItemTier.Common, SellPrice = 15 };
        var (player, room, _, loop) = MakeSellSetup(item, "sell", "1");

        loop.Run(player, room);

        player.Gold.Should().Be(15);
    }

    // ── AfterSale narration pool ─────────────────────────────────────────────

    [Fact]
    public void AfterSale_NarrationPool_IsNotEmpty()
    {
        MerchantNarration.AfterSale.Should().NotBeEmpty();
    }
}
