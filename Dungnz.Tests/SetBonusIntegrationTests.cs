using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class SetBonusIntegrationTests
{
    private static Item IC(ArmorSlot s) => new() { Name = $"IC{s}", Type = ItemType.Armor, Slot = s, SetId = "ironclad", IsEquippable = true };
    private static Item SS(ArmorSlot s) => new() { Name = $"SS{s}", Type = ItemType.Armor, Slot = s, SetId = "shadowstalker", IsEquippable = true };
    private static Item ARC(ArmorSlot s) => new() { Name = $"ARC{s}", Type = ItemType.Armor, Slot = s, SetId = "arcanist", IsEquippable = true };

    [Fact]
    public void Ironclad_OnePiece_NoBonusActivated()
    {
        var p = new Player { Name = "Hero", MaxHP = 100 };
        p.EquippedChest = IC(ArmorSlot.Chest);
        SetBonusManager.GetActiveBonuses(p).Should().BeEmpty();
    }

    [Fact]
    public void Ironclad_TwoPieces_TwoPieceBonusActivated()
    {
        var p = new Player { Name = "Hero", MaxHP = 100 };
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head);
        var bonuses = SetBonusManager.GetActiveBonuses(p);
        bonuses.Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 2);
        bonuses.Should().NotContain(b => b.SetId == "ironclad" && b.PiecesRequired == 3);
    }

    [Fact]
    public void Ironclad_ThreePieces_BothTwoAndThreePieceBonusesActive()
    {
        var p = new Player { Name = "Hero", MaxHP = 100 };
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head); p.EquippedShoulders = IC(ArmorSlot.Shoulders);
        var bonuses = SetBonusManager.GetActiveBonuses(p);
        bonuses.Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 2);
        bonuses.Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 3);
    }

    [Fact]
    public void Ironclad_UnequipToOnePiece_TwoPieceBonusRemoved()
    {
        var p = new Player { Name = "Hero", MaxHP = 100 };
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head);
        SetBonusManager.GetActiveBonuses(p).Should().Contain(b => b.PiecesRequired == 2, "precondition");
        p.EquippedHead = null;
        SetBonusManager.GetActiveBonuses(p).Should().BeEmpty();
    }

    [Fact]
    public void Ironclad_ApplySetBonuses_TwoPiece_SetsPlayerSetBonusDefense()
    {
        var p = new Player { Name = "Hero", MaxHP = 100, Defense = 0 };
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head);
        SetBonusManager.ApplySetBonuses(p);
        p.SetBonusDefense.Should().Be(3);
        p.SetBonusMaxHP.Should().Be(10);
    }

    [Fact]
    public void Ironclad_Unyielding_ActiveWhenHPBelow25Percent()
    {
        var p = new Player { Name = "Hero", MaxHP = 100 };
        p.SetHPDirect(20);
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head); p.EquippedShoulders = IC(ArmorSlot.Shoulders);
        SetBonusManager.IsUnyieldingActive(p).Should().BeTrue();
    }

    [Fact]
    public void Ironclad_Unyielding_InactiveWhenHPAbove25Percent()
    {
        var p = new Player { Name = "Hero", MaxHP = 100 };
        p.SetHPDirect(50);
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head); p.EquippedShoulders = IC(ArmorSlot.Shoulders);
        SetBonusManager.IsUnyieldingActive(p).Should().BeFalse();
    }

    [Fact]
    public void ShadowStalker_ThreePieces_ShadowDanceActive()
    {
        var p = new Player { Name = "Rogue" };
        p.EquippedChest = SS(ArmorSlot.Chest); p.EquippedHead = SS(ArmorSlot.Head); p.EquippedShoulders = SS(ArmorSlot.Shoulders);
        SetBonusManager.IsShadowDanceActive(p).Should().BeTrue();
    }

    [Fact]
    public void ShadowStalker_TwoPieces_ShadowDanceInactive()
    {
        var p = new Player { Name = "Rogue" };
        p.EquippedChest = SS(ArmorSlot.Chest); p.EquippedHead = SS(ArmorSlot.Head);
        SetBonusManager.IsShadowDanceActive(p).Should().BeFalse();
    }

    [Fact]
    public void Arcanist_ThreePieces_HighMana_ArcaneSurgeActive()
    {
        var p = new Player { Name = "Mage", Mana = 90, MaxMana = 100 };
        p.EquippedChest = ARC(ArmorSlot.Chest); p.EquippedHead = ARC(ArmorSlot.Head); p.EquippedShoulders = ARC(ArmorSlot.Shoulders);
        SetBonusManager.IsArcaneSurgeActive(p).Should().BeTrue();
    }

    [Fact]
    public void Arcanist_ThreePieces_LowMana_ArcaneSurgeInactive()
    {
        var p = new Player { Name = "Mage", Mana = 50, MaxMana = 100 };
        p.EquippedChest = ARC(ArmorSlot.Chest); p.EquippedHead = ARC(ArmorSlot.Head); p.EquippedShoulders = ARC(ArmorSlot.Shoulders);
        SetBonusManager.IsArcaneSurgeActive(p).Should().BeFalse();
    }

    [Fact]
    public void MixedSets_OneEachFromTwoSets_NoBonuses()
    {
        var p = new Player { Name = "Hero" };
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = SS(ArmorSlot.Head);
        SetBonusManager.GetActiveBonuses(p).Should().BeEmpty();
    }

    [Fact]
    public void GetEquippedSetPieces_FourPiecesAcrossSlots_CountsCorrectly()
    {
        var p = new Player { Name = "Tank" };
        p.EquippedChest = IC(ArmorSlot.Chest); p.EquippedHead = IC(ArmorSlot.Head);
        p.EquippedShoulders = IC(ArmorSlot.Shoulders); p.EquippedLegs = IC(ArmorSlot.Legs);
        SetBonusManager.GetEquippedSetPieces(p, "ironclad").Should().Be(4);
    }
}
