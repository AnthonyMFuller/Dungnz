using CsCheck;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Property-based tests using CsCheck to verify invariants that must hold for all
/// generated inputs — LootTable RNG determinism, Item.Clone isolation, and
/// Player stat non-negativity after equip/unequip cycles.
/// </summary>
public class PropertyTests
{
    // ── LootTable: two tables seeded identically produce identical gold rolls ──

    [Fact]
    public void LootTable_IdenticalSeeds_ProduceIdenticalGoldRolls()
    {
        Gen.Int[1, 9999].Sample(seed =>
        {
            var table1 = new LootTable(new Random(seed), minGold: 1, maxGold: 50);
            var table2 = new LootTable(new Random(seed), minGold: 1, maxGold: 50);

            var dummy = new Dungnz.Systems.Enemies.GiantRat();
            var result1 = table1.RollDrop(dummy);
            var result2 = table2.RollDrop(dummy);

            result1.Gold.Should().Be(result2.Gold,
                $"identical seed {seed} must yield identical gold rolls");
        });
    }

    // ── Item.Clone: clone is independent of source ────────────────────────

    [Fact]
    public void Item_Clone_IsIndependent()
    {
        Gen.Int[0, 50].Select(Gen.Int[0, 50]).Sample((atk, def) =>
        {
            var original = new Item
            {
                Name = "Test Sword",
                Type = ItemType.Weapon,
                AttackBonus = atk,
                DefenseBonus = def,
                IsEquippable = true,
                Tier = ItemTier.Common
            };
            var clone = original.Clone();

            // Mutate original — clone should not be affected
            original.AttackBonus = 999;

            clone.AttackBonus.Should().Be(atk,
                $"Clone should be independent: original.AttackBonus mutated to 999 but clone should remain {atk}");
        });
    }

    // ── Player defense never goes below zero after equip/unequip ─────────

    [Fact]
    public void Player_Defense_NeverNegativeAfterUnequip()
    {
        Gen.Int[0, 20].Sample(defBonus =>
        {
            var player = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Level = 1 };
            var armor = new Item
            {
                Name = "Test Armor",
                Type = ItemType.Armor,
                DefenseBonus = defBonus,
                IsEquippable = true,
                Tier = ItemTier.Common,
                Slot = ArmorSlot.Chest
            };
            player.Inventory.Add(armor);
            player.EquipItem(armor);
            player.UnequipItem("chest");

            player.Defense.Should().BeGreaterThanOrEqualTo(0,
                $"Defense must not go negative after equipping then unequipping +{defBonus} DEF armor");
        });
    }
}
