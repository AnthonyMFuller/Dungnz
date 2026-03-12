using Dungnz.Display.Spectre;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using VerifyXunit;
using Xunit;

namespace Dungnz.Tests.Display;

/// <summary>
/// Snapshot tests for <see cref="SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup"/>
/// and <see cref="SpectreLayoutDisplayService.BuildGearPanelMarkup"/>.
///
/// Every notable player class × state combination is captured so that layout changes
/// produce reviewable diffs rather than silent regressions.
/// Verified snapshots (*.verified.txt) are committed alongside this file.
///
/// Resolves issues #1353 and #1354.
/// </summary>
public class PanelMarkupSnapshotTests
{
    // ── Stats panel — per-class / per-state baselines ────────────────────────

    [Fact]
    public Task StatsPanel_Warrior_NoMomentum_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Aldric")
            .WithClass(PlayerClass.Warrior)
            .WithHP(80).WithMaxHP(100)
            .WithMana(20).WithMaxMana(30)
            .WithAttack(15).WithDefense(8)
            .WithLevel(5).WithGold(200).WithXP(380)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task StatsPanel_Warrior_ChargedFury_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Aldric")
            .WithClass(PlayerClass.Warrior)
            .WithHP(80).WithMaxHP(100)
            .WithMana(20).WithMaxMana(30)
            .WithAttack(15).WithDefense(8)
            .WithLevel(5).WithGold(200).WithXP(380)
            .Build();
        player.Momentum = new MomentumResource(5);
        player.Momentum.Add(5);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task StatsPanel_Mage_ArcaneCharge_WithCooldown_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Lyra")
            .WithClass(PlayerClass.Mage)
            .WithHP(60).WithMaxHP(80)
            .WithMana(50).WithMaxMana(60)
            .WithAttack(8).WithDefense(3)
            .WithLevel(7).WithGold(150).WithXP(620)
            .Build();
        player.Momentum = new MomentumResource(3);
        player.Momentum.Add(2);

        var cooldowns = new (string, int)[] { ("Fireball", 2), ("Blink", 0) };

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(player, cooldowns);

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task StatsPanel_Rogue_WithComboPoints_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Sable")
            .WithClass(PlayerClass.Rogue)
            .WithHP(70).WithMaxHP(90)
            .WithMana(25).WithMaxMana(25)
            .WithAttack(18).WithDefense(5)
            .WithLevel(6).WithGold(300).WithXP(500)
            .Build();
        player.AddComboPoints(3);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task StatsPanel_Paladin_PartialDevotion_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Crestfall")
            .WithClass(PlayerClass.Paladin)
            .WithHP(120).WithMaxHP(150)
            .WithMana(30).WithMaxMana(35)
            .WithAttack(12).WithDefense(14)
            .WithLevel(8).WithGold(100).WithXP(750)
            .Build();
        player.Momentum = new MomentumResource(4);
        player.Momentum.Add(2);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task StatsPanel_Necromancer_Baseline_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Morthis")
            .WithClass(PlayerClass.Necromancer)
            .WithHP(50).WithMaxHP(70)
            .WithMana(70).WithMaxMana(70)
            .WithAttack(6).WithDefense(3)
            .WithLevel(4).WithGold(80).WithXP(280)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task StatsPanel_Ranger_WithFocus_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Vael")
            .WithClass(PlayerClass.Ranger)
            .WithHP(90).WithMaxHP(105)
            .WithMana(35).WithMaxMana(40)
            .WithAttack(16).WithDefense(6)
            .WithLevel(6).WithGold(175).WithXP(480)
            .Build();
        player.Momentum = new MomentumResource(3);
        player.Momentum.Add(1);

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        return Verifier.Verify(markup);
    }

    // ── Gear panel — equipment load-out baselines ─────────────────────────────

    [Fact]
    public Task GearPanel_EmptyLoadout_MatchesSnapshot()
    {
        var player = new PlayerBuilder()
            .Named("Bare")
            .WithClass(PlayerClass.Warrior)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildGearPanelMarkup(player);

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task GearPanel_WeaponAndArmor_MatchesSnapshot()
    {
        var weapon = new ItemBuilder()
            .Named("Iron Sword")
            .WithId("iron-sword")
            .WithDamage(8)
            .WithTier(ItemTier.Common)
            .Build();

        var chest = new ItemBuilder()
            .Named("Chainmail")
            .WithId("chainmail")
            .WithDefense(5)
            .WithSlot(ArmorSlot.Chest)
            .WithTier(ItemTier.Uncommon)
            .Build();

        var player = new PlayerBuilder()
            .Named("Aldric")
            .WithClass(PlayerClass.Warrior)
            .WithWeapon(weapon)
            .Build();
        player.EquippedChest = chest;

        var markup = SpectreLayoutDisplayService.BuildGearPanelMarkup(player);

        return Verifier.Verify(markup);
    }

    [Fact]
    public Task GearPanel_FullLoadout_MatchesSnapshot()
    {
        var weapon = new ItemBuilder()
            .Named("Runed Blade")
            .WithId("runed-blade")
            .WithDamage(15)
            .WithTier(ItemTier.Rare)
            .Build();

        var accessory = new ItemBuilder()
            .Named("Ring of the Fallen")
            .WithId("ring-fallen")
            .OfType(ItemType.Accessory)
            .WithSellPrice(80)
            .WithTier(ItemTier.Epic)
            .AsEquippable()
            .Build();

        Item ArmorPiece(string name, string id, int def, ArmorSlot slot, ItemTier tier) =>
            new ItemBuilder()
                .Named(name).WithId(id).WithDefense(def).WithSlot(slot).WithTier(tier).Build();

        var player = new PlayerBuilder()
            .Named("Aldric")
            .WithClass(PlayerClass.Warrior)
            .WithWeapon(weapon)
            .WithAccessory(accessory)
            .Build();

        player.EquippedHead      = ArmorPiece("Iron Helm",        "iron-helm",       2, ArmorSlot.Head,      ItemTier.Common);
        player.EquippedShoulders = ArmorPiece("Leather Spaulders", "leather-spaul",   1, ArmorSlot.Shoulders, ItemTier.Common);
        player.EquippedChest     = ArmorPiece("Chainmail",         "chainmail",       5, ArmorSlot.Chest,     ItemTier.Uncommon);
        player.EquippedHands     = ArmorPiece("Iron Gauntlets",    "iron-gauntlets",  2, ArmorSlot.Hands,     ItemTier.Common);
        player.EquippedLegs      = ArmorPiece("Plate Greaves",     "plate-greaves",   4, ArmorSlot.Legs,      ItemTier.Uncommon);
        player.EquippedFeet      = ArmorPiece("Ironshod Boots",    "ironshod-boots",  1, ArmorSlot.Feet,      ItemTier.Common);
        player.EquippedBack      = ArmorPiece("Wolf Pelt Cloak",   "wolf-pelt-cloak", 2, ArmorSlot.Back,      ItemTier.Uncommon);
        player.EquippedOffHand   = ArmorPiece("Tower Shield",      "tower-shield",    6, ArmorSlot.OffHand,   ItemTier.Rare);

        var markup = SpectreLayoutDisplayService.BuildGearPanelMarkup(player);

        return Verifier.Verify(markup);
    }
}
