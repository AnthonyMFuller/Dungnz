using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for RNG determinism — verifies that two runs initialised with the same seed
/// produce identical item sequences and identical combat damage outcomes.
/// </summary>
[Collection("EnemyFactory")]
public class RngDeterminismTests
{
    // ── Item drop determinism ─────────────────────────────────────────────────

    [Fact]
    public void TwoRunsWithSameSeed_ProduceSameFirstItemDrop()
    {
        var gen1 = new DungeonGenerator(42, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(42, Array.Empty<Item>());

        var (start1, _) = gen1.Generate();
        var (start2, _) = gen2.Generate();

        // Collect items from all reachable rooms in BFS order so order is deterministic.
        var items1 = CollectAllItems(start1);
        var items2 = CollectAllItems(start2);

        items1.Count.Should().Be(items2.Count, "same seed must produce same number of items");
        for (int i = 0; i < items1.Count; i++)
        {
            items1[i].Name.Should().Be(items2[i].Name,
                $"item[{i}] must be identical between two same-seed runs");
        }
    }

    [Fact]
    public void TwoRunsWithSameSeed_DifferentSeeds_ProduceDifferentLayouts()
    {
        var genA = new DungeonGenerator(1, Array.Empty<Item>());
        var genB = new DungeonGenerator(99999, Array.Empty<Item>());

        var (startA, _) = genA.Generate();
        var (startB, _) = genB.Generate();

        // It is extremely unlikely that two different seeds produce identical descriptions
        // for every room. We just check the start room description differs OR enemy name differs.
        bool differ = startA.Description != startB.Description
                      || startA.Enemy?.Name != startB.Enemy?.Name;

        differ.Should().BeTrue("different seeds should produce different dungeon layouts");
    }

    // ── Combat damage determinism ─────────────────────────────────────────────

    [Fact]
    public void TwoRunsWithSameSeed_ProduceSameCombatDamage()
    {
        // Both players start at 100 HP. After running the same combat with the same
        // seeded RNG, both should end at exactly the same HP.
        var player1 = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Mana = 0, MaxMana = 100 };
        var player2 = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Mana = 0, MaxMana = 100 };

        var enemy1 = new Enemy_Stub(hp: 30, atk: 8, def: 2, xp: 10);
        var enemy2 = new Enemy_Stub(hp: 30, atk: 8, def: 2, xp: 10);

        var display = new FakeDisplayService();
        var input1  = new FakeInputReader("A", "A", "A", "A", "A", "A", "A", "A");
        var input2  = new FakeInputReader("A", "A", "A", "A", "A", "A", "A", "A");

        var engine1 = new CombatEngine(display, input1, new Random(42));
        var engine2 = new CombatEngine(display, input2, new Random(42));

        engine1.RunCombat(player1, enemy1);
        engine2.RunCombat(player2, enemy2);

        player1.HP.Should().Be(player2.HP,
            "identical seed must produce identical HP loss across two combat runs");
    }

    [Fact]
    public void TwoRunsWithDifferentSeeds_MayProduceDifferentCombatDamage()
    {
        // Seed 1 vs seed 2 — damage outcomes will almost certainly differ because crit
        // and dodge rolls diverge. We test the seeding infrastructure works at all.
        var player1 = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Mana = 0, MaxMana = 100 };
        var player2 = new Player { HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Mana = 0, MaxMana = 100 };

        var enemy1 = new Enemy_Stub(hp: 30, atk: 8, def: 2, xp: 10);
        var enemy2 = new Enemy_Stub(hp: 30, atk: 8, def: 2, xp: 10);

        var display = new FakeDisplayService();
        var input1  = new FakeInputReader("A", "A", "A", "A", "A", "A", "A", "A");
        var input2  = new FakeInputReader("A", "A", "A", "A", "A", "A", "A", "A");

        // Just verify no exception; the actual outcome difference is probabilistic.
        var engine1 = new CombatEngine(display, input1, new Random(1));
        var engine2 = new CombatEngine(display, input2, new Random(2));

        var act = () => { engine1.RunCombat(player1, enemy1); engine2.RunCombat(player2, enemy2); };
        act.Should().NotThrow("different seeds must still produce valid combat outcomes");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<Item> CollectAllItems(Room start)
    {
        var visited = new HashSet<Room>();
        var queue   = new Queue<Room>();
        var items   = new List<Item>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            items.AddRange(room.Items);
            foreach (var (_, next) in room.Exits)
            {
                if (visited.Add(next))
                    queue.Enqueue(next);
            }
        }
        return items;
    }
}
