using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for equipment unequip edge cases (#949):
/// unequipping to full inventory, unequipping last item, equip/unequip stat changes, swapping.
/// </summary>
public class EquipmentUnequipEdgeCaseTests
{
    // ── Unequip stat reversal ─────────────────────────────────────────────────

    [Fact]
    public void UnequipWeapon_ReversesAttackBonus()
    {
        var player = new PlayerBuilder().WithAttack(10).Build();
        var sword = new ItemBuilder().Named("Sword").WithDamage(5).Build();
        player.Inventory.Add(sword);
        player.EquipItem(sword);
        player.Attack.Should().Be(15);

        player.UnequipItem("weapon");

        player.Attack.Should().Be(10);
        player.EquippedWeapon.Should().BeNull();
        player.Inventory.Should().Contain(sword);
    }

    [Fact]
    public void UnequipArmor_ReversesDefenseBonus()
    {
        var player = new PlayerBuilder().WithDefense(5).Build();
        var armor = new ItemBuilder().Named("Plate").WithDefense(8).WithSlot(ArmorSlot.Chest).Build();
        player.Inventory.Add(armor);
        player.EquipItem(armor);
        player.Defense.Should().Be(13);

        player.UnequipItem("chest");

        player.Defense.Should().Be(5);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void UnequipAccessory_ReversesMaxManaBonus()
    {
        var player = new PlayerBuilder().WithMaxMana(30).WithMana(30).Build();
        var ring = new Item { Name = "Ring", Type = ItemType.Accessory, MaxManaBonus = 15, IsEquippable = true };
        player.Inventory.Add(ring);
        player.EquipItem(ring);
        player.MaxMana.Should().Be(45);

        player.UnequipItem("accessory");

        player.MaxMana.Should().Be(30);
    }

    // ── Equip/unequip cycle is idempotent ─────────────────────────────────────

    [Fact]
    public void EquipUnequipCycle_RestoresOriginalStats()
    {
        var player = new PlayerBuilder().WithAttack(10).WithDefense(5).Build();
        var sword = new ItemBuilder().Named("Blade").WithDamage(7).Build();
        player.Inventory.Add(sword);

        player.EquipItem(sword);
        player.UnequipItem("weapon");

        player.Attack.Should().Be(10);
        player.Defense.Should().Be(5);
    }

    // ── Swapping equipment ────────────────────────────────────────────────────

    [Fact]
    public void EquipNewWeapon_SwapsOldToInventory_AppliesNewStats()
    {
        var player = new PlayerBuilder().WithAttack(10).Build();
        var sword1 = new ItemBuilder().Named("Iron Sword").WithId("s1").WithDamage(3).Build();
        var sword2 = new ItemBuilder().Named("Steel Sword").WithId("s2").WithDamage(8).Build();

        player.Inventory.Add(sword1);
        player.EquipItem(sword1);
        player.Attack.Should().Be(13);

        player.Inventory.Add(sword2);
        player.EquipItem(sword2);

        player.Attack.Should().Be(18); // 10 + 8
        player.EquippedWeapon.Should().BeSameAs(sword2);
        player.Inventory.Should().Contain(sword1);
        player.Inventory.Should().NotContain(sword2);
    }

    [Fact]
    public void SwapArmor_CorrectlyUpdatesDefense()
    {
        var player = new PlayerBuilder().WithDefense(5).Build();
        var light = new ItemBuilder().Named("Leather").WithId("a1").WithDefense(3).WithSlot(ArmorSlot.Chest).Build();
        var heavy = new ItemBuilder().Named("Plate").WithId("a2").WithDefense(10).WithSlot(ArmorSlot.Chest).Build();

        player.Inventory.Add(light);
        player.EquipItem(light);
        player.Defense.Should().Be(8);

        player.Inventory.Add(heavy);
        player.EquipItem(heavy);

        player.Defense.Should().Be(15); // 5 + 10
        player.Inventory.Should().Contain(light);
    }

    // ── Unequipping from empty slot ───────────────────────────────────────────

    [Fact]
    public void UnequipEmptySlot_ThrowsInvalidOperationException()
    {
        var player = new Player();

        var act = () => player.UnequipItem("weapon");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UnequipInvalidSlotName_ThrowsArgumentException()
    {
        var player = new Player();

        var act = () => player.UnequipItem("banana");

        act.Should().Throw<ArgumentException>();
    }

    // ── Equip non-equippable item ─────────────────────────────────────────────

    [Fact]
    public void EquipNonEquippableItem_ThrowsArgumentException()
    {
        var player = new Player();
        var potion = new ItemBuilder().Named("Potion").WithHeal(20).Build();
        player.Inventory.Add(potion);

        var act = () => player.EquipItem(potion);

        act.Should().Throw<ArgumentException>();
    }

    // ── Equip item not in inventory ───────────────────────────────────────────

    [Fact]
    public void EquipItemNotInInventory_ThrowsArgumentException()
    {
        var player = new Player();
        var sword = new ItemBuilder().Named("Phantom Blade").WithDamage(5).Build();

        var act = () => player.EquipItem(sword);

        act.Should().Throw<ArgumentException>();
    }

    // ── Multiple armor slots ──────────────────────────────────────────────────

    [Fact]
    public void EquipDifferentArmorSlots_AllCoexist()
    {
        var player = new PlayerBuilder().WithDefense(5).Build();
        var helm = new ItemBuilder().Named("Helm").WithId("helm").WithDefense(2).WithSlot(ArmorSlot.Head).Build();
        var chest = new ItemBuilder().Named("Cuirass").WithId("chest").WithDefense(5).WithSlot(ArmorSlot.Chest).Build();
        var boots = new ItemBuilder().Named("Boots").WithId("boots").WithDefense(1).WithSlot(ArmorSlot.Feet).Build();

        player.Inventory.Add(helm);
        player.EquipItem(helm);
        player.Inventory.Add(chest);
        player.EquipItem(chest);
        player.Inventory.Add(boots);
        player.EquipItem(boots);

        player.Defense.Should().Be(13); // 5 + 2 + 5 + 1
        player.EquippedHead.Should().BeSameAs(helm);
        player.EquippedChest.Should().BeSameAs(chest);
        player.EquippedFeet.Should().BeSameAs(boots);
    }

    [Fact]
    public void UnequipOneArmorSlot_OnlyReversesThatBonus()
    {
        var player = new PlayerBuilder().WithDefense(5).Build();
        var helm = new ItemBuilder().Named("Helm").WithId("helm").WithDefense(2).WithSlot(ArmorSlot.Head).Build();
        var chest = new ItemBuilder().Named("Cuirass").WithId("chest").WithDefense(5).WithSlot(ArmorSlot.Chest).Build();

        player.Inventory.Add(helm);
        player.EquipItem(helm);
        player.Inventory.Add(chest);
        player.EquipItem(chest);
        player.Defense.Should().Be(12);

        player.UnequipItem("head");

        player.Defense.Should().Be(10); // 5 + 5 (chest remains)
        player.EquippedHead.Should().BeNull();
        player.EquippedChest.Should().BeSameAs(chest);
    }
}
