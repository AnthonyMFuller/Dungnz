using Dungnz.Display.Spectre;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.Display;

/// <summary>
/// Regression tests asserting that rendered panel markup stays within the height bounds
/// defined in <see cref="LayoutConstants"/>.
///
/// Guards against the enemy-stats-overflow bug (issue #1333) and the cooldown-line
/// overflow bug (issue #1350, fixed by bumping <see cref="LayoutConstants.StatsPanelHeight"/>
/// from 8 to 9). Expanded in issue #1354 to cover all 5 panels, all 6 player classes,
/// and the previously-blocked cooldown-active path.
/// </summary>
/// <remarks>
/// All tests call internal static seam methods directly (visible via InternalsVisibleTo)
/// so no live terminal or Live context is required in CI.
/// The [Collection("console-output")] attribute prevents parallel interference with
/// tests that write to AnsiConsole.
/// </remarks>
[Collection("console-output")]
public sealed class PanelHeightRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int LineCount(string markup) => markup.Count(c => c == '\n');

    // ── 1. Stats panel — per-class baseline tests (no momentum, no cooldowns) ─

    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Mage)]
    [InlineData(PlayerClass.Rogue)]
    [InlineData(PlayerClass.Paladin)]
    [InlineData(PlayerClass.Necromancer)]
    [InlineData(PlayerClass.Ranger)]
    public void StatsPanelLineCount_AllClasses_WithNoResourceOrCooldown_IsWithinStatsPanelHeight(PlayerClass playerClass)
    {
        var player = new PlayerBuilder()
            .Named("Hero")
            .WithClass(playerClass)
            .WithHP(80).WithMaxHP(100)
            .WithMana(20).WithMaxMana(30)
            .WithAttack(12).WithDefense(6)
            .WithLevel(5).WithGold(150)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"{playerClass} (no resource, no cooldowns) produced {count} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 2. Stats panel — max-level CHARGED Fury (Warrior) ────────────────────

    [Fact]
    public void StatsPanelLineCount_Warrior_ChargedFury_IsWithinStatsPanelHeight()
    {
        var player = new PlayerBuilder()
            .Named("MaxWarrior")
            .WithClass(PlayerClass.Warrior)
            .WithHP(250).WithMaxHP(250)
            .WithMana(80).WithMaxMana(80)
            .WithAttack(55).WithDefense(30)
            .WithLevel(20).WithGold(9999).WithXP(1850)
            .Build();
        player.Momentum = new MomentumResource(5);
        player.Momentum.Add(5);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"Max-level CHARGED Warrior produced {count} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 3. Stats panel — all momentum-bearing classes at full charge ──────────

    [Theory]
    [InlineData(PlayerClass.Mage,    3)]
    [InlineData(PlayerClass.Paladin, 4)]
    [InlineData(PlayerClass.Ranger,  3)]
    public void StatsPanelLineCount_ChargedMomentum_IsWithinStatsPanelHeight(PlayerClass playerClass, int maxMomentum)
    {
        var player = new PlayerBuilder()
            .Named("Charged")
            .WithClass(playerClass)
            .WithHP(100).WithMaxHP(100)
            .WithMana(40).WithMaxMana(40)
            .WithAttack(14).WithDefense(7)
            .WithLevel(10).WithGold(500)
            .Build();
        player.Momentum = new MomentumResource(maxMomentum);
        player.Momentum.Add(maxMomentum);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"{playerClass} with charged momentum produced {count} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 4. Stats panel — Rogue with max combo points ──────────────────────────

    [Fact]
    public void StatsPanelLineCount_Rogue_MaxComboPoints_IsWithinStatsPanelHeight()
    {
        var player = new PlayerBuilder()
            .Named("Sable")
            .WithClass(PlayerClass.Rogue)
            .WithHP(75).WithMaxHP(90)
            .WithMana(25).WithMaxMana(25)
            .WithAttack(18).WithDefense(5)
            .WithLevel(8).WithGold(300)
            .Build();
        player.AddComboPoints(5);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"Rogue with 5 combo points produced {count} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 5. Stats panel — cooldown-active path (unblocked by #1350 fix) ────────

    /// <summary>
    /// When at least one ability is on cooldown, BuildPlayerStatsPanelMarkup appends a
    /// CD: row. Before issue #1350 was fixed (StatsPanelHeight was 8), this row caused
    /// an overflow. Now that the constant is 9, the CD row must fit for all classes.
    /// </summary>
    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Mage)]
    [InlineData(PlayerClass.Rogue)]
    [InlineData(PlayerClass.Paladin)]
    [InlineData(PlayerClass.Necromancer)]
    [InlineData(PlayerClass.Ranger)]
    public void StatsPanelLineCount_AllClasses_WithActiveCooldown_IsWithinStatsPanelHeight(PlayerClass playerClass)
    {
        var player = new PlayerBuilder()
            .Named("Hero")
            .WithClass(playerClass)
            .WithHP(80).WithMaxHP(100)
            .WithMana(30).WithMaxMana(30)
            .WithAttack(12).WithDefense(6)
            .WithLevel(5).WithGold(150)
            .Build();

        var cooldowns = new (string, int)[] { ("Power Strike", 2) };

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(player, cooldowns);

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"{playerClass} with active cooldown produced {count} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows (bump from 8 to 9 in #1350 fix)");
    }

    // ── 6. Stats panel — long player name does not overflow ───────────────────

    [Fact]
    public void StatsPanelLineCount_LongPlayerName_IsWithinStatsPanelHeight()
    {
        var longName = "Sir Reginald Bartholomew The Third";  // 34 chars
        longName.Length.Should().BeGreaterThan(30, "precondition: name must be 30+ chars");

        var player = new PlayerBuilder()
            .Named(longName)
            .WithClass(PlayerClass.Paladin)
            .WithHP(100).WithMaxHP(100)
            .WithMana(40).WithMaxMana(50)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"Long name '{longName}' produced {count} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 7. Gear panel — line count within GearPanelHeight (unblocked by #1349) ─

    /// <summary>
    /// An empty loadout (all slots null) must render 10 slot rows and stay well within
    /// <see cref="LayoutConstants.GearPanelHeight"/>. Unblocked by issue #1349 which
    /// extracted <see cref="SpectreLayoutDisplayService.BuildGearPanelMarkup"/>.
    /// </summary>
    [Fact]
    public void GearPanelLineCount_EmptyLoadout_IsWithinGearPanelHeight()
    {
        var player = new PlayerBuilder()
            .Named("Bare")
            .WithClass(PlayerClass.Warrior)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildGearPanelMarkup(player);

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.GearPanelHeight,
            $"Empty gear loadout produced {count} newlines; " +
            $"panel only has {LayoutConstants.GearPanelHeight} rows");
    }

    [Fact]
    public void GearPanelLineCount_FullLoadout_IsWithinGearPanelHeight()
    {
        Item ArmorPiece(string name, string id, int def, ArmorSlot slot) =>
            new ItemBuilder().Named(name).WithId(id).WithDefense(def).WithSlot(slot)
                .WithTier(ItemTier.Uncommon).Build();

        var player = new PlayerBuilder()
            .Named("Aldric")
            .WithClass(PlayerClass.Warrior)
            .WithWeapon(new ItemBuilder().Named("Steel Sword").WithId("steel-sword")
                .WithDamage(10).WithTier(ItemTier.Common).Build())
            .WithAccessory(new ItemBuilder().Named("Lucky Charm").WithId("lucky-charm")
                .OfType(ItemType.Accessory).WithTier(ItemTier.Uncommon).AsEquippable().Build())
            .Build();

        player.EquippedHead      = ArmorPiece("Iron Helm",         "iron-helm",       2, ArmorSlot.Head);
        player.EquippedShoulders = ArmorPiece("Leather Spaulders", "leather-spaul",   1, ArmorSlot.Shoulders);
        player.EquippedChest     = ArmorPiece("Chainmail",         "chainmail",       5, ArmorSlot.Chest);
        player.EquippedHands     = ArmorPiece("Iron Gauntlets",    "iron-gauntlets",  2, ArmorSlot.Hands);
        player.EquippedLegs      = ArmorPiece("Plate Greaves",     "plate-greaves",   4, ArmorSlot.Legs);
        player.EquippedFeet      = ArmorPiece("Ironshod Boots",    "ironshod-boots",  1, ArmorSlot.Feet);
        player.EquippedBack      = ArmorPiece("Wolf Pelt Cloak",   "wolf-pelt-cloak", 2, ArmorSlot.Back);
        player.EquippedOffHand   = ArmorPiece("Tower Shield",      "tower-shield",    6, ArmorSlot.OffHand);

        var markup = SpectreLayoutDisplayService.BuildGearPanelMarkup(player);

        var count = LineCount(markup);
        count.Should().BeLessOrEqualTo(LayoutConstants.GearPanelHeight,
            $"Full gear loadout produced {count} newlines; " +
            $"panel only has {LayoutConstants.GearPanelHeight} rows");
    }

    // ── 8. LayoutConstants smoke test — all 5 panels ─────────────────────────

    /// <summary>
    /// Asserts the exact numeric values of all five <see cref="LayoutConstants"/> panel
    /// heights. Any accidental change fails loudly here, prompting developers to also
    /// update all panel-height-dependent tests and rendering code.
    ///
    /// Panel layout ratios (40-row baseline terminal):
    ///   TopRow    (20%) = 8 rows — Map + Stats side by side
    ///   MiddleRow (50%) = 20 rows — Content + Gear side by side
    ///   BottomRow (30%) = 12 rows — Log (70% of 30% ≈ 8 rows) + Input
    /// </summary>
    [Fact]
    public void LayoutConstants_AllFivePanels_HaveCorrectValues()
    {
        LayoutConstants.BaselineTerminalHeight.Should().Be(40,
            "BaselineTerminalHeight is the 40-row reference terminal used to derive all panel heights");

        LayoutConstants.StatsPanelHeight.Should().Be(9,
            "StatsPanelHeight is 20% of 40-row baseline (8 rows) +1 for the cooldown line (issue #1350 fix); " +
            "if this changes, update all stats panel regression tests and renderers");

        LayoutConstants.MapPanelHeight.Should().Be(8,
            "MapPanelHeight is TopRow = 20% of 40 = 8 rows (shares row with Stats panel); " +
            "if this changes, update map rendering");

        LayoutConstants.GearPanelHeight.Should().Be(20,
            "GearPanelHeight is MiddleRow = 50% of 40 = 20 rows; " +
            "if this changes, update gear panel rendering and seam tests");

        LayoutConstants.ContentPanelHeight.Should().Be(20,
            "ContentPanelHeight is MiddleRow = 50% of 40 = 20 rows; " +
            "if this changes, update content panel rendering");

        LayoutConstants.LogPanelHeight.Should().Be(8,
            "LogPanelHeight is BottomRow 30% x Log 70% of 40 approx 8 rows; " +
            "if this changes, update log panel rendering");
    }
}
