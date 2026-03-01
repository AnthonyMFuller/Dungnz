using CsCheck;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests.PropertyBased;

/// <summary>
/// Property-based tests using CsCheck to verify game mechanic invariants
/// that must hold for all generated inputs.
/// </summary>
public class GameMechanicPropertyTests
{
    // ── Damage calculation: TakeDamage never results in HP > starting HP ──
    [Fact]
    public void TakeDamage_NeverIncreasesHP()
    {
        Gen.Int[1, 500].Select(Gen.Int[0, 200]).Sample((startHp, damage) =>
        {
            var player = new Player { MaxHP = startHp };
            player.SetHPDirect(startHp);
            var hpBefore = player.HP;

            player.TakeDamage(damage);

            player.HP.Should().BeLessThanOrEqualTo(hpBefore,
                $"TakeDamage({damage}) on {startHp} HP must not increase HP");
            player.HP.Should().BeGreaterThanOrEqualTo(0,
                $"HP must never go negative after taking {damage} damage from {startHp} HP");
        });
    }

    // ── Healing: Heal never exceeds MaxHP ─────────────────────────────────
    [Fact]
    public void Heal_NeverExceedsMaxHP()
    {
        Gen.Int[1, 500].Select(Gen.Int[1, 500], Gen.Int[0, 1000])
            .Sample((maxHp, currentHp, healAmount) =>
            {
                var effectiveCurrentHp = Math.Min(currentHp, maxHp);
                var player = new Player { MaxHP = maxHp };
                player.SetHPDirect(effectiveCurrentHp);

                player.Heal(healAmount);

                player.HP.Should().BeLessThanOrEqualTo(player.MaxHP,
                    $"Heal({healAmount}) from {effectiveCurrentHp}/{maxHp} HP must not exceed MaxHP");
                player.HP.Should().BeGreaterThanOrEqualTo(effectiveCurrentHp,
                    $"Heal({healAmount}) from {effectiveCurrentHp} HP must not decrease HP");
            });
    }

    // ── Loot generation: loot tier scales with player level ───────────────
    [Fact]
    public void LootTier_ScalesWithPlayerLevel()
    {
        // For levels 1-3, base tier is Tier1 (Common)
        // For levels 4-6, base tier is Tier2 (Uncommon)
        // For levels 7+, base tier is Tier3 (Rare)
        // When an item drops from the tiered roll, it should be at least the expected base tier.
        Gen.Int[1, 10].Sample(level =>
        {
            var enemy = new GiantRat();
            var seededRng = new Random(42);
            // Force the 30% tiered roll to always succeed by using a low RNG value
            var table = new LootTable(new ForcedDropRandom(seededRng), minGold: 1, maxGold: 10);

            var result = table.RollDrop(enemy, playerLevel: level);

            if (result.Item != null)
            {
                var expectedMinTier = level >= 7 ? ItemTier.Rare
                                    : level >= 4 ? ItemTier.Uncommon
                                    : ItemTier.Common;
                ((int)result.Item.Tier).Should().BeGreaterThanOrEqualTo((int)expectedMinTier,
                    $"At level {level}, dropped item tier should be >= {expectedMinTier}");
            }
        });
    }

    // ── Gold generation: gold reward is always non-negative ───────────────
    [Fact]
    public void GoldReward_AlwaysNonNegative()
    {
        Gen.Int[0, 100].Select(Gen.Int[0, 100]).Sample((min, range) =>
        {
            var maxGold = min + range;
            var table = new LootTable(minGold: min, maxGold: maxGold);
            var enemy = new GiantRat();

            var result = table.RollDrop(enemy);

            result.Gold.Should().BeGreaterThanOrEqualTo(0,
                $"Gold from LootTable(min={min}, max={maxGold}) must be non-negative");
            result.Gold.Should().BeGreaterThanOrEqualTo(min,
                $"Gold should be >= minGold ({min})");
            result.Gold.Should().BeLessThanOrEqualTo(maxGold,
                $"Gold should be <= maxGold ({maxGold})");
        });
    }

    // ── TakeDamage + Heal round-trip: HP stays in [0, MaxHP] range ───────
    [Fact]
    public void DamageAndHeal_HPAlwaysInValidRange()
    {
        Gen.Int[1, 300].Select(Gen.Int[0, 200], Gen.Int[0, 200])
            .Sample((maxHp, damage, heal) =>
            {
                var player = new Player { MaxHP = maxHp };
                player.SetHPDirect(maxHp);

                player.TakeDamage(damage);
                player.HP.Should().BeInRange(0, maxHp);

                player.Heal(heal);
                player.HP.Should().BeInRange(0, maxHp);
            });
    }

    /// <summary>
    /// Random subclass that forces the tiered item roll to succeed (NextDouble returns 0.01
    /// for the drop chance check) while delegating other calls to the inner RNG.
    /// </summary>
    private class ForcedDropRandom : Random
    {
        private readonly Random _inner;
        private int _callCount;

        public ForcedDropRandom(Random inner) => _inner = inner;

        public override double NextDouble()
        {
            _callCount++;
            // The first NextDouble calls are for explicit drops, then the tiered roll check
            // Return low value to force drops to succeed
            return 0.01;
        }

        public override int Next(int maxValue) => _inner.Next(maxValue);
        public override int Next(int minValue, int maxValue) => _inner.Next(minValue, maxValue);
    }
}
