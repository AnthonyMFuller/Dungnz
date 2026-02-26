using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Phase 8-A4: Unit tests for the Phase 7 armor slot system.</summary>
public class ArmorSlotTests
{
    private static Player MakePlayer() => new() { Name = "Tester" };

    private static Item MakeArmor(string id, ArmorSlot slot, float dodge = 0, bool poisonImmune = false, string? setId = null) =>
        new Item { Id = id, Name = id, Type = ItemType.Armor, IsEquippable = true, Slot = slot,
                   DodgeBonus = dodge, PoisonImmunity = poisonImmune, SetId = setId };

    // ── EquipItem routing ────────────────────────────────────────────────────

    [Fact] public void EquipItem_SlotHead_SetsEquippedHead()
    { var p = MakePlayer(); var i = MakeArmor("helm", ArmorSlot.Head); p.Inventory.Add(i); p.EquipItem(i); p.EquippedHead.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotShoulders_SetsEquippedShoulders()
    { var p = MakePlayer(); var i = MakeArmor("pauldrons", ArmorSlot.Shoulders); p.Inventory.Add(i); p.EquipItem(i); p.EquippedShoulders.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotChest_SetsEquippedChest()
    { var p = MakePlayer(); var i = MakeArmor("cuirass", ArmorSlot.Chest); p.Inventory.Add(i); p.EquipItem(i); p.EquippedChest.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotHands_SetsEquippedHands()
    { var p = MakePlayer(); var i = MakeArmor("gauntlets", ArmorSlot.Hands); p.Inventory.Add(i); p.EquipItem(i); p.EquippedHands.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotLegs_SetsEquippedLegs()
    { var p = MakePlayer(); var i = MakeArmor("greaves", ArmorSlot.Legs); p.Inventory.Add(i); p.EquipItem(i); p.EquippedLegs.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotFeet_SetsEquippedFeet()
    { var p = MakePlayer(); var i = MakeArmor("boots", ArmorSlot.Feet); p.Inventory.Add(i); p.EquipItem(i); p.EquippedFeet.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotBack_SetsEquippedBack()
    { var p = MakePlayer(); var i = MakeArmor("cloak", ArmorSlot.Back); p.Inventory.Add(i); p.EquipItem(i); p.EquippedBack.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotOffHand_SetsEquippedOffHand()
    { var p = MakePlayer(); var i = MakeArmor("shield", ArmorSlot.OffHand); p.Inventory.Add(i); p.EquipItem(i); p.EquippedOffHand.Should().BeSameAs(i); }

    [Fact] public void EquipItem_SlotNone_FallsBackToEquippedChest()
    { var p = MakePlayer(); var i = MakeArmor("robe", ArmorSlot.None); p.Inventory.Add(i); p.EquipItem(i); p.EquippedChest.Should().BeSameAs(i); }

    // ── UnequipItem by slot name ─────────────────────────────────────────────

    [Fact]
    public void UnequipItem_Head_RemovesEquippedHeadAndReturnsToInventory()
    {
        var player = MakePlayer();
        var item = MakeArmor("helm", ArmorSlot.Head);
        player.Inventory.Add(item);
        player.EquipItem(item);
        var returned = player.UnequipItem("head");
        returned.Should().BeSameAs(item);
        player.EquippedHead.Should().BeNull();
        player.Inventory.Should().Contain(item);
    }

    [Fact] public void UnequipItem_Shoulders_RemovesEquippedShoulders()
    { var p = MakePlayer(); var i = MakeArmor("pauldrons", ArmorSlot.Shoulders); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("shoulders"); p.EquippedShoulders.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_Chest_RemovesEquippedChest()
    { var p = MakePlayer(); var i = MakeArmor("cuirass", ArmorSlot.Chest); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("chest"); p.EquippedChest.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_Hands_RemovesEquippedHands()
    { var p = MakePlayer(); var i = MakeArmor("gauntlets", ArmorSlot.Hands); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("hands"); p.EquippedHands.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_Legs_RemovesEquippedLegs()
    { var p = MakePlayer(); var i = MakeArmor("greaves", ArmorSlot.Legs); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("legs"); p.EquippedLegs.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_Feet_RemovesEquippedFeet()
    { var p = MakePlayer(); var i = MakeArmor("boots", ArmorSlot.Feet); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("feet"); p.EquippedFeet.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_Back_RemovesEquippedBack()
    { var p = MakePlayer(); var i = MakeArmor("cloak", ArmorSlot.Back); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("back"); p.EquippedBack.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_OffHand_RemovesEquippedOffHand()
    { var p = MakePlayer(); var i = MakeArmor("shield", ArmorSlot.OffHand); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("offhand"); p.EquippedOffHand.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_ArmorLegacyAlias_RemovesEquippedChest()
    { var p = MakePlayer(); var i = MakeArmor("robe", ArmorSlot.Chest); p.Inventory.Add(i); p.EquipItem(i); p.UnequipItem("armor"); p.EquippedChest.Should().BeNull(); p.Inventory.Should().Contain(i); }

    [Fact] public void UnequipItem_HeadWhenEmpty_ThrowsInvalidOperationException()
    { var act = () => MakePlayer().UnequipItem("head"); act.Should().Throw<InvalidOperationException>(); }

    // ── AllEquippedArmor ─────────────────────────────────────────────────────

    [Fact] public void AllEquippedArmor_WhenNoneEquipped_ReturnsEmpty()
    { MakePlayer().AllEquippedArmor.Should().BeEmpty(); }

    [Fact]
    public void AllEquippedArmor_HeadAndChestEquipped_ReturnsExactlyTwoItems()
    {
        var p = MakePlayer();
        var helm = MakeArmor("helm", ArmorSlot.Head);
        var chest = MakeArmor("cuirass", ArmorSlot.Chest);
        p.Inventory.Add(helm); p.Inventory.Add(chest);
        p.EquipItem(helm); p.EquipItem(chest);
        p.AllEquippedArmor.Should().HaveCount(2).And.Contain(helm).And.Contain(chest);
    }

    [Fact] public void AllEquippedArmor_DoesNotIncludeNullSlots()
    { var p = MakePlayer(); var i = MakeArmor("boots", ArmorSlot.Feet); p.Inventory.Add(i); p.EquipItem(i); p.AllEquippedArmor.Should().NotContainNulls(); }

    // ── Stat aggregation ─────────────────────────────────────────────────────

    [Fact]
    public void DodgeBonus_SumsAcrossMultipleEquippedArmorPieces()
    {
        var p = MakePlayer();
        var h = MakeArmor("helm", ArmorSlot.Head, dodge: 0.05f);
        var b = MakeArmor("boots", ArmorSlot.Feet, dodge: 0.03f);
        p.Inventory.Add(h); p.Inventory.Add(b);
        p.EquipItem(h); p.EquipItem(b);
        p.DodgeBonus.Should().BeApproximately(0.08f, precision: 0.0001f);
    }

    [Fact]
    public void PoisonImmune_IsTrueIfAnyEquippedArmorHasPoisonImmunity()
    {
        var p = MakePlayer();
        var h = MakeArmor("helm", ArmorSlot.Head, poisonImmune: false);
        var c = MakeArmor("cuirass", ArmorSlot.Chest, poisonImmune: true);
        p.Inventory.Add(h); p.Inventory.Add(c);
        p.EquipItem(h); p.EquipItem(c);
        p.PoisonImmune.Should().BeTrue();
    }

    [Fact]
    public void PoisonImmune_IsFalseIfNoArmorHasPoisonImmunity()
    {
        var p = MakePlayer();
        var h = MakeArmor("helm", ArmorSlot.Head, poisonImmune: false);
        var c = MakeArmor("cuirass", ArmorSlot.Chest, poisonImmune: false);
        p.Inventory.Add(h); p.Inventory.Add(c);
        p.EquipItem(h); p.EquipItem(c);
        p.PoisonImmune.Should().BeFalse();
    }

    // ── GetArmorSlotItem ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(ArmorSlot.Head,      "head-item")]
    [InlineData(ArmorSlot.Shoulders, "shoulders-item")]
    [InlineData(ArmorSlot.Chest,     "chest-item")]
    [InlineData(ArmorSlot.Hands,     "hands-item")]
    [InlineData(ArmorSlot.Legs,      "legs-item")]
    [InlineData(ArmorSlot.Feet,      "feet-item")]
    [InlineData(ArmorSlot.Back,      "back-item")]
    [InlineData(ArmorSlot.OffHand,   "offhand-item")]
    public void GetArmorSlotItem_ReturnsCorrectItemForSlot(ArmorSlot slot, string id)
    {
        var p = MakePlayer(); var item = MakeArmor(id, slot);
        p.Inventory.Add(item); p.EquipItem(item);
        p.GetArmorSlotItem(slot).Should().BeSameAs(item);
    }

    [Fact] public void GetArmorSlotItem_ReturnsNullForEmptySlot()
    { MakePlayer().GetArmorSlotItem(ArmorSlot.Shoulders).Should().BeNull(); }

    // ── SetBonusManager ──────────────────────────────────────────────────────

    [Fact]
    public void SetBonusManager_OnePieceIronclad_DoesNotTriggerTwoPieceBonus()
    {
        var p = MakePlayer();
        var c = MakeArmor("ironclad-chest", ArmorSlot.Chest, setId: "ironclad");
        p.Inventory.Add(c); p.EquipItem(c);
        SetBonusManager.GetActiveBonuses(p).Should().NotContain(b => b.PiecesRequired == 2 && b.SetId == "ironclad");
    }

    [Fact]
    public void SetBonusManager_TwoPiecesIronclad_TriggersTwoPieceDefenseBonus()
    {
        var p = MakePlayer();
        var chest  = MakeArmor("ironclad-chest", ArmorSlot.Chest, setId: "ironclad");
        var weapon = new Item { Id = "ironclad-sword", Name = "ironclad-sword", Type = ItemType.Weapon, IsEquippable = true, SetId = "ironclad" };
        p.Inventory.Add(chest); p.Inventory.Add(weapon);
        p.EquipItem(chest); p.EquipItem(weapon);
        SetBonusManager.GetActiveBonuses(p).Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 2 && b.DefenseBonus > 0);
    }

    [Fact]
    public void SetBonusManager_ThreePiecesIronclad_GrantsUnyielding()
    {
        var p = MakePlayer();
        var chest     = MakeArmor("ironclad-chest", ArmorSlot.Chest, setId: "ironclad");
        var weapon    = new Item { Id = "ironclad-sword", Name = "ironclad-sword", Type = ItemType.Weapon, IsEquippable = true, SetId = "ironclad" };
        var accessory = new Item { Id = "ironclad-ring", Name = "ironclad-ring", Type = ItemType.Accessory, IsEquippable = true, SetId = "ironclad" };
        p.Inventory.Add(chest); p.Inventory.Add(weapon); p.Inventory.Add(accessory);
        p.EquipItem(chest); p.EquipItem(weapon); p.EquipItem(accessory);
        SetBonusManager.GetActiveBonuses(p).Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 3 && b.GrantsUnyielding);
    }
}
