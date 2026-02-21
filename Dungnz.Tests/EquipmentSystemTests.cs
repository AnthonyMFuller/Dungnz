using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class EquipmentSystemTests
{
    [Fact]
    public void EquipWeapon_IncreasesPlayerAttack()
    {
        var player = new Player { Name = "Tester", Attack = 10 };
        var sword = new Item { Name = "Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        player.Inventory.Add(sword);
        player.EquipItem(sword);
        player.Attack.Should().Be(15);
        player.EquippedWeapon.Should().BeSameAs(sword);
    }

    [Fact]
    public void UnequipWeapon_RestoresPlayerAttack()
    {
        var player = new Player { Name = "Tester", Attack = 10 };
        var sword = new Item { Name = "Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        player.Inventory.Add(sword);
        player.EquipItem(sword);
        player.UnequipItem("weapon");
        player.Attack.Should().Be(10);
        player.EquippedWeapon.Should().BeNull();
    }

    [Fact]
    public void EquipArmor_IncreasesPlayerDefense()
    {
        var player = new Player { Name = "Tester", Defense = 5 };
        var armor = new Item { Name = "Armor", Type = ItemType.Armor, DefenseBonus = 10, IsEquippable = true };
        player.Inventory.Add(armor);
        player.EquipItem(armor);
        player.Defense.Should().Be(15);
    }

    [Fact]
    public void EquipNewWeapon_ReplacesOldOne()
    {
        var player = new Player { Name = "Tester", Attack = 10 };
        var sword1 = new Item { Name = "Sword1", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        var sword2 = new Item { Name = "Sword2", Type = ItemType.Weapon, AttackBonus = 8, IsEquippable = true };
        player.Inventory.Add(sword1);
        player.EquipItem(sword1);
        player.Inventory.Add(sword2);
        player.EquipItem(sword2);
        player.Attack.Should().Be(18); // 10 + 8
        player.EquippedWeapon.Should().BeSameAs(sword2);
        player.Inventory.Should().Contain(sword1); // old weapon in inventory
    }

    [Fact]
    public void EquipAccessory_MaxManaBonus_Applied()
    {
        var player = new Player { Name = "Tester", MaxMana = 30, Mana = 30 };
        var ring = new Item { Name = "Ring", Type = ItemType.Accessory, MaxManaBonus = 15, IsEquippable = true };
        player.Inventory.Add(ring);
        player.EquipItem(ring);
        player.MaxMana.Should().Be(45);
    }

    [Fact]
    public void EquipManager_HandleEquip_EquipsItem()
    {
        var display = new FakeDisplayService();
        var manager = new EquipmentManager(display);
        var player = new Player { Name = "Tester", Attack = 10 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        player.Inventory.Add(sword);

        manager.HandleEquip(player, "sword");

        player.EquippedWeapon.Should().BeSameAs(sword);
        player.Attack.Should().Be(15);
    }

    [Fact]
    public void EquipManager_HandleEquip_NotInInventory_ShowsError()
    {
        var display = new FakeDisplayService();
        var manager = new EquipmentManager(display);
        var player = new Player { Name = "Tester" };

        manager.HandleEquip(player, "sword");

        display.Errors.Should().Contain(e => e.Contains("sword"));
    }
}
