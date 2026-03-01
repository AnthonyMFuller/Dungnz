using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Regression tests for fuzzy Levenshtein matching in HandleEquip (issue #653).</summary>
public class EquipmentManagerFuzzyTests
{
    private static EquipmentManager MakeManager(FakeDisplayService display)
        => new EquipmentManager(display);

    private static Player MakePlayer()
        => new Player { Name = "Tester", Attack = 10, Defense = 5, HP = 100, MaxHP = 100 };

    [Fact]
    public void HandleEquip_TypoInName_FuzzyMatchEquipsAndShowsDidYouMean()
    {
        var display = new FakeDisplayService();
        var manager = MakeManager(display);
        var player = MakePlayer();
        var shoulders = new Item
        {
            Name = "Sage's Shoulderguards",
            Type = ItemType.Armor,
            DefenseBonus = 6,
            IsEquippable = true,
            Slot = ArmorSlot.Shoulders
        };
        player.Inventory.Add(shoulders);

        // Typo: "Shouldergaurds" instead of "Shoulderguards"
        manager.HandleEquip(player, "Sage's Shouldergaurds");

        player.EquippedShoulders.Should().NotBeNull("the item should be equipped despite the typo");
        player.EquippedShoulders.Should().BeSameAs(shoulders);
        display.Messages.Should().Contain(m => m.Contains("Did you mean") && m.Contains("Sage's Shoulderguards"));
    }

    [Fact]
    public void HandleEquip_ExactMatch_NoDidYouMeanMessage()
    {
        var display = new FakeDisplayService();
        var manager = MakeManager(display);
        var player = MakePlayer();
        var shoulders = new Item
        {
            Name = "Sage's Shoulderguards",
            Type = ItemType.Armor,
            DefenseBonus = 6,
            IsEquippable = true,
            Slot = ArmorSlot.Shoulders
        };
        player.Inventory.Add(shoulders);

        manager.HandleEquip(player, "Sage's Shoulderguards");

        player.EquippedShoulders.Should().BeSameAs(shoulders);
        display.Messages.Should().NotContain(m => m.Contains("Did you mean"));
    }

    [Fact]
    public void HandleEquip_CompletelyUnrelatedTypo_ShowsNotInInventoryError()
    {
        var display = new FakeDisplayService();
        var manager = MakeManager(display);
        var player = MakePlayer();
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        player.Inventory.Add(sword);

        manager.HandleEquip(player, "zzzzzzzzz");

        display.Errors.Should().ContainSingle().Which.Should().Contain("don't have");
    }

    [Fact]
    public void LevenshteinDistance_KnownValues_AreCorrect()
    {
        EquipmentManager.LevenshteinDistance("kitten", "sitting").Should().Be(3);
        EquipmentManager.LevenshteinDistance("", "abc").Should().Be(3);
        EquipmentManager.LevenshteinDistance("abc", "abc").Should().Be(0);
        EquipmentManager.LevenshteinDistance("shoulderguards", "shouldergaurds").Should().BeLessThanOrEqualTo(3);
    }
}
