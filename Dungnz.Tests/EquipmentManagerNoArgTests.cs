using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for EQUIP with no argument — interactive menu path (issue #654).</summary>
public class EquipmentManagerNoArgTests
{
    private static EquipmentManager MakeManager(FakeDisplayService display)
        => new EquipmentManager(display);

    private static Player MakePlayer()
        => new Player { Name = "Tester", Attack = 10, Defense = 5, HP = 100, MaxHP = 100 };

    private static Item MakeSword() => new Item
    {
        Name = "Iron Sword",
        Type = ItemType.Weapon,
        AttackBonus = 5,
        IsEquippable = true
    };

    private static Item MakeChestplate() => new Item
    {
        Name = "Steel Chestplate",
        Type = ItemType.Armor,
        DefenseBonus = 8,
        IsEquippable = true,
        Slot = ArmorSlot.Chest
    };

    private static Item MakePotion() => new Item
    {
        Name = "Health Potion",
        Type = ItemType.Consumable,
        IsEquippable = false
    };

    [Fact]
    public void HandleEquip_NoArgument_WithEquippableItems_ShowsMenuAndEquips()
    {
        // Arrange — pass "1" so FakeDisplayService picks the first equippable item
        var input = new FakeInputReader("1");
        var display = new FakeDisplayService(input);
        var manager = MakeManager(display);
        var player = MakePlayer();
        var sword = MakeSword();
        player.Inventory.Add(sword);

        // Act
        manager.HandleEquip(player, "");

        // Assert
        display.AllOutput.Should().Contain("equip_menu", "the interactive equip menu should be shown");
        player.EquippedWeapon.Should().BeSameAs(sword, "selecting item 1 from the menu should equip the sword");
    }

    [Fact]
    public void HandleEquip_NoArgument_NoEquippableItems_ShowsError()
    {
        // Arrange — inventory contains only non-equippable items
        var display = new FakeDisplayService();
        var manager = MakeManager(display);
        var player = MakePlayer();
        player.Inventory.Add(MakePotion());
        player.Inventory.Add(new Item { Name = "Gold Coin", Type = ItemType.Gold, IsEquippable = false });

        // Act
        manager.HandleEquip(player, "");

        // Assert
        display.Errors.Should().ContainSingle(
            e => e.Contains("no equippable", StringComparison.OrdinalIgnoreCase),
            "an error should tell the player there are no equippable items");
        display.AllOutput.Should().NotContain("equip_menu", "the menu should not appear when there is nothing to equip");
    }

    [Fact]
    public void HandleEquip_NoArgument_CancelSelection_DoesNotEquip()
    {
        // Arrange — no FakeInputReader provided, so ShowEquipMenuAndSelect returns null (cancel)
        var display = new FakeDisplayService();
        var manager = MakeManager(display);
        var player = MakePlayer();
        var sword = MakeSword();
        player.Inventory.Add(sword);

        // Act
        manager.HandleEquip(player, "");

        // Assert
        display.AllOutput.Should().Contain("equip_menu", "the menu should still be shown before the player cancels");
        player.EquippedWeapon.Should().BeNull("cancelling the menu should not equip anything");
    }

    [Fact]
    public void HandleEquip_NoArgument_WhitespaceArgument_TreatedSameAsEmpty()
    {
        // Arrange — "  " (whitespace) should trigger the same interactive-menu path as ""
        var input = new FakeInputReader("1");
        var display = new FakeDisplayService(input);
        var manager = MakeManager(display);
        var player = MakePlayer();
        var chestplate = MakeChestplate();
        player.Inventory.Add(chestplate);

        // Act
        manager.HandleEquip(player, "  ");

        // Assert
        display.AllOutput.Should().Contain("equip_menu", "whitespace should be treated as empty and show the menu");
        player.EquippedChest.Should().BeSameAs(chestplate, "the menu should equip the selected item");
    }
}
