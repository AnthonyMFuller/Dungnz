using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for static narration/content classes that previously had 0% coverage.</summary>
public class StaticContentCoverageTests
{
    // ── FloorTransitionNarration ─────────────────────────────────────────────

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void FloorTransitionNarration_GetSequence_ValidFloor_Returns5Lines(int floor)
    {
        var lines = FloorTransitionNarration.GetSequence(floor);
        lines.Should().HaveCount(5, $"floor {floor} transition should have exactly 5 lines");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public void FloorTransitionNarration_GetSequence_OutOfRange_ReturnsEmpty(int floor)
    {
        var lines = FloorTransitionNarration.GetSequence(floor);
        lines.Should().BeEmpty($"floor {floor} has no transition sequence");
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor2_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor2.Should().HaveCount(5);
        FloorTransitionNarration.ToFloor2[0].Should().NotBeEmpty();
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor3_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor3.Should().HaveCount(5);
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor4_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor4.Should().HaveCount(5);
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor5_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor5.Should().HaveCount(5);
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor6_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor6.Should().HaveCount(5);
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor7_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor7.Should().HaveCount(5);
    }

    [Fact]
    public void FloorTransitionNarration_ToFloor8_IsNotEmpty()
    {
        FloorTransitionNarration.ToFloor8.Should().HaveCount(5);
    }

    // ── ShrineNarration ──────────────────────────────────────────────────────

    [Fact]
    public void ShrineNarration_Presence_HasMultipleLines()
    {
        ShrineNarration.Presence.Should().NotBeEmpty();
        ShrineNarration.Presence.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShrineNarration_UseShrine_HasMultipleLines()
    {
        ShrineNarration.UseShrine.Should().NotBeEmpty();
    }

    [Fact]
    public void ShrineNarration_GrantHeal_HasMultipleLines()
    {
        ShrineNarration.GrantHeal.Should().NotBeEmpty();
    }

    [Fact]
    public void ShrineNarration_GrantPower_HasMultipleLines()
    {
        ShrineNarration.GrantPower.Should().NotBeEmpty();
    }

    [Fact]
    public void ShrineNarration_AllArrays_ContainNonEmptyStrings()
    {
        ShrineNarration.Presence.Should().AllSatisfy(s => s.Should().NotBeNullOrEmpty());
        ShrineNarration.UseShrine.Should().AllSatisfy(s => s.Should().NotBeNullOrEmpty());
        ShrineNarration.GrantHeal.Should().AllSatisfy(s => s.Should().NotBeNullOrEmpty());
        ShrineNarration.GrantPower.Should().AllSatisfy(s => s.Should().NotBeNullOrEmpty());
    }

    // ── AbilityFlavorText ────────────────────────────────────────────────────

    [Theory]
    [InlineData(AbilityType.ShieldBash)]
    [InlineData(AbilityType.BattleCry)]
    [InlineData(AbilityType.Fortify)]
    [InlineData(AbilityType.RecklessBlow)]
    [InlineData(AbilityType.LastStand)]
    [InlineData(AbilityType.ArcaneBolt)]
    [InlineData(AbilityType.FrostNova)]
    [InlineData(AbilityType.ArcaneSacrifice)]
    [InlineData(AbilityType.Meteor)]
    [InlineData(AbilityType.QuickStrike)]
    [InlineData(AbilityType.Backstab)]
    [InlineData(AbilityType.Evade)]
    [InlineData(AbilityType.Flurry)]
    [InlineData(AbilityType.Assassinate)]
    public void AbilityFlavorText_Get_KnownAbility_ReturnsNonEmptyString(AbilityType type)
    {
        var text = AbilityFlavorText.Get(type);
        text.Should().NotBeNullOrEmpty($"{type} should have flavor text");
    }

    [Fact]
    public void AbilityFlavorText_Get_ManaShield_StandardText()
    {
        var text = AbilityFlavorText.Get(AbilityType.ManaShield, enhanced: false);
        text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AbilityFlavorText_Get_ManaShield_Enhanced_ReturnsDifferentText()
    {
        var standard = AbilityFlavorText.Get(AbilityType.ManaShield, enhanced: false);
        var enhanced = AbilityFlavorText.Get(AbilityType.ManaShield, enhanced: true);
        standard.Should().NotBe(enhanced);
    }

    [Fact]
    public void AbilityFlavorText_Get_Backstab_Enhanced_ReturnsDifferentText()
    {
        var standard = AbilityFlavorText.Get(AbilityType.Backstab, enhanced: false);
        var enhanced = AbilityFlavorText.Get(AbilityType.Backstab, enhanced: true);
        standard.Should().NotBe(enhanced);
    }

    [Fact]
    public void AbilityFlavorText_Get_UnknownAbility_ReturnsEmpty()
    {
        // Cast to a value that doesn't exist in the enum switch
        var text = AbilityFlavorText.Get((AbilityType)9999);
        text.Should().BeEmpty();
    }

    // ── DungeonVariant ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, "Goblin Caves")]
    [InlineData(2, "Skeleton Catacombs")]
    [InlineData(3, "Troll Warrens")]
    [InlineData(4, "Shadow Realm")]
    [InlineData(5, "Dragon's Lair")]
    [InlineData(6, "Void Antechamber")]
    [InlineData(7, "Bone Palace")]
    [InlineData(8, "Final Sanctum")]
    public void DungeonVariant_ForFloor_KnownFloor_HasCorrectName(int floor, string expectedName)
    {
        var variant = DungeonVariant.ForFloor(floor);
        variant.Name.Should().Be(expectedName);
    }

    [Fact]
    public void DungeonVariant_ForFloor_UnknownFloor_FallsBackToGenericName()
    {
        var variant = DungeonVariant.ForFloor(99);
        variant.Name.Should().Be("Floor 99");
    }

    [Fact]
    public void DungeonVariant_ForFloor_UnknownFloor_HasDefaultMessages()
    {
        var variant = DungeonVariant.ForFloor(99);
        variant.EntryMessage.Should().Be("You descend into unknown depths.");
        variant.ExitMessage.Should().BeEmpty();
    }

    [Fact]
    public void DungeonVariant_ForFloor_Floor1_HasEntryMessage()
    {
        var variant = DungeonVariant.ForFloor(1);
        variant.EntryMessage.Should().NotBeEmpty();
        variant.ExitMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void DungeonVariant_ForFloor_Floor8_EmptyExitMessage()
    {
        var variant = DungeonVariant.ForFloor(8);
        variant.Name.Should().Be("Final Sanctum");
        variant.ExitMessage.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void DungeonVariant_ForFloor_Floors1Through7_HaveNonEmptyEntryAndExit(int floor)
    {
        var variant = DungeonVariant.ForFloor(floor);
        variant.EntryMessage.Should().NotBeEmpty($"floor {floor} should have an entry message");
        variant.ExitMessage.Should().NotBeEmpty($"floor {floor} should have an exit message");
    }
}
