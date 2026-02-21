using Dungnz.Models;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class LootTableTests
{
    [Fact]
    public void EmptyDropsTable_OnlyGoldReturned()
    {
        // Use controlled RNG (0.95 > 0.30) to prevent the 30% tiered item drop from firing
        var table = new LootTable(new ControlledRandom(defaultDouble: 0.95), minGold: 5, maxGold: 5);
        var result = table.RollDrop(null!);
        result.Gold.Should().Be(5);
        result.Item.Should().BeNull();
    }

    [Fact]
    public void ZeroGoldRange_GoldIsZero()
    {
        var table = new LootTable(minGold: 0, maxGold: 0);
        var result = table.RollDrop(null!);
        result.Gold.Should().Be(0);
    }

    [Fact]
    public void FixedGold_ExactAmount()
    {
        var table = new LootTable(minGold: 42, maxGold: 42);
        var result = table.RollDrop(null!);
        result.Gold.Should().Be(42);
    }

    [Fact]
    public void ItemDrop_At100Percent_AlwaysReturned()
    {
        var item = new Item { Name = "TestItem" };
        var table = new LootTable(new Random(0), minGold: 0, maxGold: 0);
        table.AddDrop(item, 1.0);

        for (int i = 0; i < 20; i++)
        {
            var result = table.RollDrop(null!);
            result.Item.Should().BeSameAs(item);
        }
    }

    [Fact]
    public void ItemDrop_At0Percent_NeverReturned()
    {
        var item = new Item { Name = "ZeroChanceItem" };
        var table = new LootTable(new Random(0), minGold: 0, maxGold: 0);
        table.AddDrop(item, 0.0);

        for (int i = 0; i < 20; i++)
        {
            var result = table.RollDrop(null!);
            // The 0% configured drop should never appear (tiered drops from the pool may still appear)
            result.Item.Should().NotBeSameAs(item);
        }
    }

    [Fact]
    public void FirstMatchingDropWins()
    {
        var item1 = new Item { Name = "First" };
        var item2 = new Item { Name = "Second" };
        var table = new LootTable(new Random(0), minGold: 0, maxGold: 0);
        table.AddDrop(item1, 1.0);
        table.AddDrop(item2, 1.0);

        var result = table.RollDrop(null!);
        result.Item.Should().BeSameAs(item1);
    }

    [Fact]
    public void GoldInRange()
    {
        var rng = new Random(12345);
        var table = new LootTable(rng, minGold: 10, maxGold: 20);

        for (int i = 0; i < 50; i++)
        {
            var result = table.RollDrop(null!);
            result.Gold.Should().BeInRange(10, 20);
        }
    }

    [Fact]
    public void SeededRandom_GivesReproducibleResults()
    {
        var table1 = new LootTable(new Random(999), minGold: 1, maxGold: 100);
        var table2 = new LootTable(new Random(999), minGold: 1, maxGold: 100);

        var r1 = table1.RollDrop(null!);
        var r2 = table2.RollDrop(null!);
        r1.Gold.Should().Be(r2.Gold);
    }
}
