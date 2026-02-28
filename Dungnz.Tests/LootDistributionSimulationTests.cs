using Dungnz.Models;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Dungnz.Tests;

/// <summary>
/// Simulates 10,000 loot drops across a representative mix of player levels and asserts
/// that the tier distribution of dropped items stays within ±5 % of the target design ratios:
///   Common ~60 %  |  Uncommon ~30 %  |  Rare ~10 %
///
/// The level distribution mirrors expected dungeon progression:
///   60 % of encounters at levels 1–3  (Common tier)
///   30 % of encounters at levels 4–6  (Uncommon tier)
///   10 % of encounters at levels 7–9  (Rare tier)
///
/// This acts as a regression guard: it will fail if Phase 2 (or later) additions skew the
/// tier pools embedded in <see cref="LootTable"/> away from the intended ratios.
/// </summary>
[Collection("LootTableTests")]
public class LootDistributionSimulationTests
{
    private const int SimulationRolls   = 10_000;
    private const double TargetCommon   = 0.60;
    private const double TargetUncommon = 0.30;
    private const double TargetRare     = 0.10;
    private const double Tolerance      = 0.05;

    private readonly ITestOutputHelper _output;

    public LootDistributionSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LootDrops_10000Rolls_TierDistributionWithinTolerance()
    {
        // Arrange
        // Fixed seed for reproducibility across CI runs.
        var rng  = new Random(42);
        var table = new LootTable(new Random(42), minGold: 0, maxGold: 0);

        int countCommon   = 0;
        int countUncommon = 0;
        int countRare     = 0;
        int totalItems    = 0;

        // Act — simulate dungeon encounters, sampling player level from the 60/30/10 design mix.
        for (int i = 0; i < SimulationRolls; i++)
        {
            int playerLevel = SamplePlayerLevel(rng);
            var result      = table.RollDrop(null!, playerLevel);

            if (result.Item is null) continue;

            totalItems++;
            switch (result.Item.Tier)
            {
                case ItemTier.Common:   countCommon++;   break;
                case ItemTier.Uncommon: countUncommon++; break;
                case ItemTier.Rare:     countRare++;     break;
            }
        }

        // Compute percentages (guard against the unlikely case of zero item drops).
        totalItems.Should().BeGreaterThan(0, "at least some items must drop in 10,000 rolls");

        double pctCommon   = (double)countCommon   / totalItems;
        double pctUncommon = (double)countUncommon / totalItems;
        double pctRare     = (double)countRare     / totalItems;

        // Assert — report first so failures print the distribution before the assertion fires.
        PrintReport(totalItems, countCommon, pctCommon,
                                countUncommon, pctUncommon,
                                countRare, pctRare);

        pctCommon.Should().BeApproximately(TargetCommon,   Tolerance,
            $"Common drops should be ~{TargetCommon:P0} ±{Tolerance:P0} but was {pctCommon:P1}");

        pctUncommon.Should().BeApproximately(TargetUncommon, Tolerance,
            $"Uncommon drops should be ~{TargetUncommon:P0} ±{Tolerance:P0} but was {pctUncommon:P1}");

        pctRare.Should().BeApproximately(TargetRare, Tolerance,
            $"Rare drops should be ~{TargetRare:P0} ±{Tolerance:P0} but was {pctRare:P1}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Samples a player level that reflects expected dungeon progression:
    ///   60 % → levels 1–3  (Common tier pool in LootTable)
    ///   30 % → levels 4–6  (Uncommon tier pool)
    ///   10 % → levels 7–9  (Rare tier pool)
    /// </summary>
    private static int SamplePlayerLevel(Random rng)
    {
        double roll = rng.NextDouble();
        if (roll < 0.60) return rng.Next(1, 4);   // 1, 2, or 3
        if (roll < 0.90) return rng.Next(4, 7);   // 4, 5, or 6
        return rng.Next(7, 10);                    // 7, 8, or 9
    }

    private void PrintReport(int total,
                             int common,   double pctCommon,
                             int uncommon, double pctUncommon,
                             int rare,     double pctRare)
    {
        _output.WriteLine("=== Loot Distribution Simulation Report ===");
        _output.WriteLine($"Total rolls    : {SimulationRolls:N0}");
        _output.WriteLine($"Items dropped  : {total:N0}  ({(double)total / SimulationRolls:P1} drop rate)");
        _output.WriteLine("");
        _output.WriteLine($"{"Tier",-12} {"Count",8}  {"Actual",8}  {"Target",8}  {"Delta",8}  {"Pass?",6}");
        _output.WriteLine(new string('-', 60));
        PrintRow("Common",   common,   pctCommon,   TargetCommon);
        PrintRow("Uncommon", uncommon, pctUncommon, TargetUncommon);
        PrintRow("Rare",     rare,     pctRare,     TargetRare);
        _output.WriteLine("");
        _output.WriteLine($"Tolerance: ±{Tolerance:P0}");
    }

    private void PrintRow(string tier, int count, double actual, double target)
    {
        double delta = actual - target;
        bool pass    = Math.Abs(delta) <= Tolerance;
        _output.WriteLine(
            $"{tier,-12} {count,8}  {actual,7:P1}  {target,7:P0}  {delta,+8:P1}  {(pass ? "PASS" : "FAIL"),6}");
    }
}
