using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Phase 8-A4: Unit tests for the Phase 7 armor slot system.</summary>
public class ArmorSlotTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Player MakePlayer() => new() { Name = "Tester" };

    private static Item MakeArmor(string name, ArmorSlot slot, int defBonus = 5) =>
        new() { Name = name, Type = ItemType.Armor, IsEquippable = true, Slot = slot, DefenseBonus = defBonus };

    // ════════════════════════════════════════════════════════════════════════
    // EquipItem routing
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EquipItem_SlotHead_GoesToEquippedHead()
    {
        var player = MakePlayer();
        var helm = MakeArmor("Test Helm", ArmorSlot.Head);
        player.Inventory.Add(helm);
        player.EquipItem(helm);
        player.EquippedHead.Should().BeSameAs(helm);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotShoulders_GoesToEquippedShoulders()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Pauldrons", ArmorSlot.Shoulders);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedShoulders.Should().BeSameAs(item);
        player.EquippedHead.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotChest_GoesToEquippedChest()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Cuirass", ArmorSlot.Chest);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedChest.Should().BeSameAs(item);
        player.EquippedHead.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotHands_GoesToEquippedHands()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Gauntlets", ArmorSlot.Hands);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedHands.Should().BeSameAs(item);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotLegs_GoesToEquippedLegs()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Greaves", ArmorSlot.Legs);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedLegs.Should().BeSameAs(item);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotFeet_GoesToEquippedFeet()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Boots", ArmorSlot.Feet);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedFeet.Should().BeSameAs(item);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotBack_GoesToEquippedBack()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Cloak", ArmorSlot.Back);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedBack.Should().BeSameAs(item);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotOffHand_GoesToEquippedOffHand()
    {
        var player = MakePlayer();
        var item = MakeArmor("Test Shield", ArmorSlot.OffHand);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedOffHand.Should().BeSameAs(item);
        player.EquippedChest.Should().BeNull();
    }

    [Fact]
    public void EquipItem_SlotNone_DefaultsToChest()
    {
        var player = MakePlayer();
        var item = MakeArmor("Generic Armor", ArmorSlot.None);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.EquippedChest.Should().BeSameAs(item);
    }

    [Fact]
    public void EquipItem_OccupiedSlot_ReturnsOldItemToInventory()
    {
        var player = MakePlayer();
        var first = MakeArmor("Old Helm", ArmorSlot.Head, defBonus: 3);
        var second = MakeArmor("New Helm", ArmorSlot.Head, defBonus: 7);
        player.Inventory.Add(first);
        player.EquipItem(first);
        player.Inventory.Add(second);
        player.EquipItem(second);
        player.EquippedHead.Should().BeSameAs(second);
        player.Inventory.Should().Contain(first);
        player.Inventory.Should().NotContain(second);
    }

    // ════════════════════════════════════════════════════════════════════════
    // UnequipItem by slot name
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnequipItem_Head_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Helm", ArmorSlot.Head);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("head");
        player.EquippedHead.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_Shoulders_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Pauldrons", ArmorSlot.Shoulders);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("shoulders");
        player.EquippedShoulders.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_Chest_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Cuirass", ArmorSlot.Chest);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("chest");
        player.EquippedChest.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_Hands_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Gauntlets", ArmorSlot.Hands);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("hands");
        player.EquippedHands.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_Legs_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Greaves", ArmorSlot.Legs);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("legs");
        player.EquippedLegs.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_Feet_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Boots", ArmorSlot.Feet);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("feet");
        player.EquippedFeet.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_Back_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Cloak", ArmorSlot.Back);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("back");
        player.EquippedBack.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_OffHand_ReturnsItemToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("Shield", ArmorSlot.OffHand);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("offhand");
        player.EquippedOffHand.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_ArmorAlias_MapsToChest()
    {
        var player = MakePlayer();
        var item = MakeArmor("Chest Plate", ArmorSlot.Chest);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.UnequipItem("armor"); // legacy alias
        player.EquippedChest.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact]
    public void UnequipItem_InvalidSlot_ThrowsArgumentException()
    {
        var player = MakePlayer();
        var act = () => player.UnequipItem("banana");
        act.Should().Throw<ArgumentException>();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AllEquippedArmor
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AllEquippedArmor_ThreeSlotsOccupied_YieldsExactlyThreeItems()
    {
        var player = MakePlayer();
        var helm  = MakeArmor("Helm",  ArmorSlot.Head);
        var chest = MakeArmor("Chest", ArmorSlot.Chest);
        var boots = MakeArmor("Boots", ArmorSlot.Feet);
        player.Inventory.AddRange([helm, chest, boots]);
        player.EquipItem(helm);
        player.EquipItem(chest);
        player.EquipItem(boots);
        player.AllEquippedArmor.Should().HaveCount(3).And.Contain(helm).And.Contain(chest).And.Contain(boots);
    }

    [Fact]
    public void AllEquippedArmor_NoSlotsOccupied_IsEmpty()
    {
        var player = MakePlayer();
        player.AllEquippedArmor.Should().BeEmpty();
    }

    [Fact]
    public void AllEquippedArmor_DoesNotIncludeWeaponOrAccessory()
    {
        var player = MakePlayer();
        var weapon = new Item { Name = "Sword", Type = ItemType.Weapon, IsEquippable = true, AttackBonus = 5 };
        var ring   = new Item { Name = "Ring",  Type = ItemType.Accessory, IsEquippable = true };
        player.Inventory.AddRange([weapon, ring]);
        player.EquipItem(weapon);
        player.EquipItem(ring);
        player.AllEquippedArmor.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Stat aggregation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void DodgeBonus_SumsAcrossMultipleArmorSlots()
    {
        var player = MakePlayer();
        var headItem = new Item { Name = "Dodge Helm", Type = ItemType.Armor, IsEquippable = true, Slot = ArmorSlot.Head, DodgeBonus = 0.1f };
        var feetItem = new Item { Name = "Dodge Boots", Type = ItemType.Armor, IsEquippable = true, Slot = ArmorSlot.Feet, DodgeBonus = 0.2f };
        player.Inventory.AddRange([headItem, feetItem]);
        player.EquipItem(headItem);
        player.EquipItem(feetItem);
        player.DodgeBonus.Should().BeApproximately(0.3f, precision: 0.001f);
    }

    [Fact]
    public void PoisonImmune_TrueWhenItemEquipped_FalseAfterUnequip()
    {
        var player = MakePlayer();
        var pauldrons = new Item
        {
            Name = "Venom Guard",
            Type = ItemType.Armor,
            IsEquippable = true,
            Slot = ArmorSlot.Shoulders,
            PoisonImmunity = true
        };
        player.Inventory.Add(pauldrons);
        player.EquipItem(pauldrons);
        player.PoisonImmune.Should().BeTrue();
        player.UnequipItem("shoulders");
        player.PoisonImmune.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // SetBonusManager triggers
    // ════════════════════════════════════════════════════════════════════════

    private static Item MakeIroncladWeapon() =>
        new() { Name = "Ironclad Sword", Type = ItemType.Weapon, IsEquippable = true, AttackBonus = 5, SetId = "ironclad" };

    private static Item MakeIroncladChest() =>
        new() { Name = "Ironclad Cuirass", Type = ItemType.Armor, IsEquippable = true, Slot = ArmorSlot.Chest, DefenseBonus = 5, SetId = "ironclad" };

    private static Item MakeIroncladAccessory() =>
        new() { Name = "Ironclad Medallion", Type = ItemType.Accessory, IsEquippable = true, SetId = "ironclad" };

    [Fact]
    public void SetBonus_OnePieceIronclad_TwoPieceBonusNotActive()
    {
        var player = MakePlayer();
        var chest = MakeIroncladChest();
        player.Inventory.Add(chest);
        player.EquipItem(chest);

        var active = SetBonusManager.GetActiveBonuses(player);
        active.Should().NotContain(b => b.SetId == "ironclad" && b.PiecesRequired == 2);
    }

    [Fact]
    public void SetBonus_TwoPiecesIronclad_TwoPieceBonusIsActive()
    {
        var player = MakePlayer();
        var weapon = MakeIroncladWeapon();
        var chest  = MakeIroncladChest();
        player.Inventory.AddRange([weapon, chest]);
        player.EquipItem(weapon);
        player.EquipItem(chest);

        var active = SetBonusManager.GetActiveBonuses(player);
        active.Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 2 && b.DefenseBonus > 0);
    }

    [Fact]
    public void SetBonus_ThreePiecesIronclad_ThreePieceBonusIsActive()
    {
        var player = MakePlayer();
        var weapon    = MakeIroncladWeapon();
        var chest     = MakeIroncladChest();
        var accessory = MakeIroncladAccessory();
        player.Inventory.AddRange([weapon, chest, accessory]);
        player.EquipItem(weapon);
        player.EquipItem(chest);
        player.EquipItem(accessory);

        var active = SetBonusManager.GetActiveBonuses(player);
        active.Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 3 && b.GrantsUnyielding);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GetArmorSlotItem
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ArmorSlot.Head,      "Head Item")]
    [InlineData(ArmorSlot.Shoulders, "Shoulders Item")]
    [InlineData(ArmorSlot.Chest,     "Chest Item")]
    [InlineData(ArmorSlot.Hands,     "Hands Item")]
    [InlineData(ArmorSlot.Legs,      "Legs Item")]
    [InlineData(ArmorSlot.Feet,      "Feet Item")]
    [InlineData(ArmorSlot.Back,      "Back Item")]
    [InlineData(ArmorSlot.OffHand,   "OffHand Item")]
    public void GetArmorSlotItem_ReturnsEquippedItemForEachSlot(ArmorSlot slot, string name)
    {
        var player = MakePlayer();
        var item = MakeArmor(name, slot);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.GetArmorSlotItem(slot).Should().BeSameAs(item);
    }

    [Fact]
    public void GetArmorSlotItem_SlotNone_MapsToChest()
    {
        var player = MakePlayer();
        var item = MakeArmor("Generic Plate", ArmorSlot.None);
        player.Inventory.Add(item);
        player.EquipItem(item);
        player.GetArmorSlotItem(ArmorSlot.None).Should().BeSameAs(item);
        player.GetArmorSlotItem(ArmorSlot.Chest).Should().BeSameAs(item);
    }
}
