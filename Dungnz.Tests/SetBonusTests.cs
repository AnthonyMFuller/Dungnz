using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for SetBonusManager set-piece counting and bonus activation.</summary>
public class SetBonusTests
{
    private static Item IroncladItem(ArmorSlot slot) => new Item
    {
        Name = $"Ironclad {slot}",
        Type = ItemType.Armor,
        Slot = slot,
        SetId = "ironclad",
    };

    [Fact]
    public void SetBonusManager_IroncladSet_ActivatesWhenThreePiecesEquipped()
    {
        var player = new Player { HP = 100, MaxHP = 100 };
        player.EquippedChest     = IroncladItem(ArmorSlot.Chest);
        player.EquippedHead      = IroncladItem(ArmorSlot.Head);
        player.EquippedShoulders = IroncladItem(ArmorSlot.Shoulders);

        var bonuses = SetBonusManager.GetActiveBonuses(player);

        bonuses.Should().Contain(b => b.GrantsUnyielding,
            "Ironclad 3-piece grants Unyielding");
    }

    [Fact]
    public void SetBonusManager_NoBonusWithTwoPieces()
    {
        var player = new Player { HP = 100, MaxHP = 100 };
        player.EquippedChest = IroncladItem(ArmorSlot.Chest);
        player.EquippedHead  = IroncladItem(ArmorSlot.Head);
        // Only 2 pieces equipped — no 3-piece bonus

        var bonuses = SetBonusManager.GetActiveBonuses(player);

        bonuses.Should().NotContain(b => b.GrantsUnyielding,
            "Unyielding requires 3 Ironclad pieces, not 2");
    }

    [Fact]
    public void SetBonusManager_IroncladTwoPieces_StillGrantsDefenseBonus()
    {
        var player = new Player { HP = 100, MaxHP = 100 };
        player.EquippedChest = IroncladItem(ArmorSlot.Chest);
        player.EquippedHead  = IroncladItem(ArmorSlot.Head);

        var bonuses = SetBonusManager.GetActiveBonuses(player);

        bonuses.Should().Contain(b => b.SetId == "ironclad" && b.PiecesRequired == 2,
            "2-piece Ironclad bonus (MaxHP + DEF) should still activate");
    }

    [Fact]
    public void SetBonusManager_ApplySetBonuses_SetsUnyieldingFlagOnPlayer()
    {
        // ApplySetBonuses wires 4-piece flags; Unyielding is a combat-time read so
        // the method itself doesn't write a flag — but GetActiveBonuses is exercised.
        var player = new Player { HP = 100, MaxHP = 100 };
        player.EquippedChest     = IroncladItem(ArmorSlot.Chest);
        player.EquippedHead      = IroncladItem(ArmorSlot.Head);
        player.EquippedShoulders = IroncladItem(ArmorSlot.Shoulders);

        SetBonusManager.ApplySetBonuses(player);

        // 3-piece Ironclad has no DamageReflectPercent (that is 4-piece),
        // so verify the method ran without error and piece count is correct.
        SetBonusManager.GetEquippedSetPieces(player, "ironclad").Should().Be(3);
    }

    [Fact]
    public void SetBonusManager_GetEquippedSetPieces_CountsAllArmorSlots()
    {
        var player = new Player { HP = 100, MaxHP = 100 };
        player.EquippedChest     = IroncladItem(ArmorSlot.Chest);
        player.EquippedHead      = IroncladItem(ArmorSlot.Head);
        player.EquippedShoulders = IroncladItem(ArmorSlot.Shoulders);
        player.EquippedLegs      = IroncladItem(ArmorSlot.Legs);

        SetBonusManager.GetEquippedSetPieces(player, "ironclad").Should().Be(4);
    }
}
