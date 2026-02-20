using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class InventoryManagerTests
{
    private static (Player player, Room room, FakeDisplayService display, InventoryManager manager) Make()
    {
        var player = new Player { HP = 50, MaxHP = 100 };
        var room = new Room();
        var display = new FakeDisplayService();
        var manager = new InventoryManager(display);
        return (player, room, display, manager);
    }

    [Fact]
    public void TakeItem_ItemExists_MovesToInventory()
    {
        var (player, room, display, manager) = Make();
        var item = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        room.Items.Add(item);

        var result = manager.TakeItem(player, room, "potion");

        result.Should().BeTrue();
        player.Inventory.Should().Contain(item);
        room.Items.Should().NotContain(item);
    }

    [Fact]
    public void TakeItem_ItemNotFound_ReturnsFalse()
    {
        var (player, room, display, manager) = Make();

        var result = manager.TakeItem(player, room, "sword");

        result.Should().BeFalse();
        display.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void UseItem_Consumable_HealPlayer()
    {
        var (player, room, display, manager) = Make();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        player.Inventory.Add(potion);

        var result = manager.UseItem(player, "potion");

        result.Should().Be(UseResult.Used);
        player.HP.Should().Be(80);
        player.Inventory.Should().NotContain(potion);
    }

    [Fact]
    public void UseItem_Consumable_ClampsToMaxHP()
    {
        var (player, room, display, manager) = Make();
        player.HP = 95;
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        player.Inventory.Add(potion);

        manager.UseItem(player, "potion");

        player.HP.Should().Be(100);
    }

    [Fact]
    public void UseItem_Weapon_IncreasesAttack()
    {
        var (player, room, display, manager) = Make();
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        player.Inventory.Add(sword);

        var result = manager.UseItem(player, "sword");

        result.Should().Be(UseResult.Used);
        player.Attack.Should().Be(15);
        player.Inventory.Should().NotContain(sword);
    }

    [Fact]
    public void UseItem_Armor_IncreasesDefense()
    {
        var (player, room, display, manager) = Make();
        var armor = new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 3 };
        player.Inventory.Add(armor);

        var result = manager.UseItem(player, "armor");

        result.Should().Be(UseResult.Used);
        player.Defense.Should().Be(8);
        player.Inventory.Should().NotContain(armor);
    }

    [Fact]
    public void UseItem_NotInInventory_ReturnsNotFound()
    {
        var (player, room, display, manager) = Make();

        var result = manager.UseItem(player, "nonexistent");

        result.Should().Be(UseResult.NotFound);
    }

    [Fact]
    public void UseItem_OtherType_ReturnsNotUsable()
    {
        var (player, room, display, manager) = Make();
        // Create item with type that doesn't map to Consumable/Weapon/Armor
        var misc = new Item { Name = "Strange Rock", Type = (ItemType)99 };
        player.Inventory.Add(misc);

        var result = manager.UseItem(player, "rock");

        result.Should().Be(UseResult.NotUsable);
    }
}
