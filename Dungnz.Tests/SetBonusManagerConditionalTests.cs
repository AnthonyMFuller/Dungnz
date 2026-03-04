using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #1002 — Tests for SetBonusManager: ArcaneSurge, ShadowDance, UnyieldingFortress
/// set bonuses. Verifies 2-piece and 3-piece bonuses apply correct stat modifications,
/// and equipping/unequipping pieces changes active bonuses.
/// </summary>
public class SetBonusManagerConditionalTests
{
    private static Item MakeSetItem(string setId, ArmorSlot slot) => new()
    {
        Name = $"{setId} {slot}",
        Type = ItemType.Armor,
        Slot = slot,
        SetId = setId,
    };

    // ── ArcaneSurge ──────────────────────────────────────────────────────────

    [Fact]
    public void ArcaneSurge_Active_WhenThreePiecesAndHighMana()
    {
        var player = new Player { Mana = 28, MaxMana = 30 };
        player.EquippedChest = MakeSetItem("arcanist", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("arcanist", ArmorSlot.Head);
        player.EquippedLegs = MakeSetItem("arcanist", ArmorSlot.Legs);

        SetBonusManager.IsArcaneSurgeActive(player).Should().BeTrue(
            "3-piece arcanist with >80% mana should activate Arcane Surge");
    }

    [Fact]
    public void ArcaneSurge_Inactive_WhenLowMana()
    {
        var player = new Player { Mana = 5, MaxMana = 30 };
        player.EquippedChest = MakeSetItem("arcanist", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("arcanist", ArmorSlot.Head);
        player.EquippedLegs = MakeSetItem("arcanist", ArmorSlot.Legs);

        SetBonusManager.IsArcaneSurgeActive(player).Should().BeFalse(
            "mana below 80% should not activate Arcane Surge");
    }

    [Fact]
    public void ArcaneSurge_Inactive_WithOnlyTwoPieces()
    {
        var player = new Player { Mana = 30, MaxMana = 30 };
        player.EquippedChest = MakeSetItem("arcanist", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("arcanist", ArmorSlot.Head);

        SetBonusManager.IsArcaneSurgeActive(player).Should().BeFalse(
            "only 2 arcanist pieces should not activate 3-piece bonus");
    }

    // ── ShadowDance ──────────────────────────────────────────────────────────

    [Fact]
    public void ShadowDance_Active_WithThreePieces()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("shadowstalker", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("shadowstalker", ArmorSlot.Head);
        player.EquippedLegs = MakeSetItem("shadowstalker", ArmorSlot.Legs);

        SetBonusManager.IsShadowDanceActive(player).Should().BeTrue();
    }

    [Fact]
    public void ShadowDance_Inactive_WithTwoPieces()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("shadowstalker", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("shadowstalker", ArmorSlot.Head);

        SetBonusManager.IsShadowDanceActive(player).Should().BeFalse();
    }

    // ── Unyielding ───────────────────────────────────────────────────────────

    [Fact]
    public void Unyielding_Active_WithThreePiecesAndLowHP()
    {
        var player = new Player { HP = 10, MaxHP = 100 };
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("ironclad", ArmorSlot.Head);
        player.EquippedShoulders = MakeSetItem("ironclad", ArmorSlot.Shoulders);

        SetBonusManager.IsUnyieldingActive(player).Should().BeTrue(
            "3-piece ironclad at <25% HP should activate Unyielding");
    }

    [Fact]
    public void Unyielding_Inactive_WhenHPAboveThreshold()
    {
        var player = new Player { HP = 80, MaxHP = 100 };
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("ironclad", ArmorSlot.Head);
        player.EquippedShoulders = MakeSetItem("ironclad", ArmorSlot.Shoulders);

        SetBonusManager.IsUnyieldingActive(player).Should().BeFalse(
            "HP above 25% should not activate Unyielding");
    }

    // ── Apply/Unequip ────────────────────────────────────────────────────────

    [Fact]
    public void ApplySetBonuses_Ironclad2Piece_GrantsDefAndHP()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("ironclad", ArmorSlot.Head);

        SetBonusManager.ApplySetBonuses(player);

        player.SetBonusDefense.Should().Be(3);
        player.SetBonusMaxHP.Should().Be(10);
    }

    [Fact]
    public void ApplySetBonuses_Arcanist2Piece_GrantsMana()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("arcanist", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("arcanist", ArmorSlot.Head);

        SetBonusManager.ApplySetBonuses(player);

        player.SetBonusMaxMana.Should().Be(20);
    }

    [Fact]
    public void ApplySetBonuses_Ironclad4Piece_GrantsReflect()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("ironclad", ArmorSlot.Head);
        player.EquippedShoulders = MakeSetItem("ironclad", ArmorSlot.Shoulders);
        player.EquippedLegs = MakeSetItem("ironclad", ArmorSlot.Legs);

        SetBonusManager.ApplySetBonuses(player);

        player.DamageReflectPercent.Should().BeApproximately(0.10f, 0.001f);
    }

    [Fact]
    public void Unequip_RemovesSetBonus()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("ironclad", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("ironclad", ArmorSlot.Head);
        SetBonusManager.ApplySetBonuses(player);
        player.SetBonusDefense.Should().Be(3);

        player.EquippedHead = null;
        SetBonusManager.ApplySetBonuses(player);

        player.SetBonusDefense.Should().Be(0, "below 2-piece threshold removes bonus");
    }

    [Fact]
    public void Sentinel4Piece_GrantsStunImmunity()
    {
        var player = new Player();
        player.EquippedChest = MakeSetItem("sentinel", ArmorSlot.Chest);
        player.EquippedHead = MakeSetItem("sentinel", ArmorSlot.Head);
        player.EquippedLegs = MakeSetItem("sentinel", ArmorSlot.Legs);
        player.EquippedFeet = MakeSetItem("sentinel", ArmorSlot.Feet);

        SetBonusManager.ApplySetBonuses(player);

        player.IsStunImmune.Should().BeTrue();
    }

    [Fact]
    public void GetActiveBonusDescription_NoBonuses_ReturnsEmpty()
    {
        var player = new Player();
        SetBonusManager.GetActiveBonusDescription(player).Should().BeEmpty();
    }
}
