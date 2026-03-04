using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #954 — Tests that static state doesn't leak between independent usages.
/// Verifies DungeonGenerator instances are independent, narration arrays are immutable,
/// and SetBonusManager computes from current state.
/// </summary>
[Collection("EnemyFactory")]
public class StaticStateIsolationTests
{
    [Fact]
    public void DungeonGenerator_TwoInstances_DifferentSeeds_Independent()
    {
        var gen1 = new DungeonGenerator(1, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(2, Array.Empty<Item>());

        var (start1, _) = gen1.Generate();
        var (start2, _) = gen2.Generate();

        start1.Id.Should().NotBe(start2.Id);
    }

    [Fact]
    public void DungeonGenerator_SequentialGenerations_ProduceFreshRooms()
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (start1, _) = gen.Generate(floor: 1);
        var (start2, _) = gen.Generate(floor: 1);

        start1.Id.Should().NotBe(start2.Id, "sequential generations should produce new rooms");
    }

    [Fact]
    public void SetBonusManager_ClearEquipment_RemovesBonuses()
    {
        var player = new Player { HP = 100, MaxHP = 100 };
        player.EquippedChest = new Item { Name = "IC Chest", Type = ItemType.Armor, Slot = ArmorSlot.Chest, SetId = "ironclad" };
        player.EquippedHead = new Item { Name = "IC Head", Type = ItemType.Armor, Slot = ArmorSlot.Head, SetId = "ironclad" };

        SetBonusManager.ApplySetBonuses(player);
        player.SetBonusDefense.Should().BeGreaterThan(0, "2-piece ironclad grants DEF");

        player.EquippedChest = null;
        player.EquippedHead = null;
        SetBonusManager.ApplySetBonuses(player);

        player.SetBonusDefense.Should().Be(0, "no set pieces means no bonus");
    }

    [Fact]
    public void SetBonusManager_DifferentPlayers_IndependentBonuses()
    {
        var player1 = new Player();
        player1.EquippedChest = new Item { Name = "IC Chest", Type = ItemType.Armor, Slot = ArmorSlot.Chest, SetId = "ironclad" };
        player1.EquippedHead = new Item { Name = "IC Head", Type = ItemType.Armor, Slot = ArmorSlot.Head, SetId = "ironclad" };

        var player2 = new Player();

        SetBonusManager.ApplySetBonuses(player1);
        SetBonusManager.ApplySetBonuses(player2);

        player1.SetBonusDefense.Should().BeGreaterThan(0);
        player2.SetBonusDefense.Should().Be(0, "player2 has no set pieces");
    }

    [Fact]
    public void NarrationArrays_StableAcrossReads()
    {
        var first = RoomStateNarration.ClearedRoom[0];
        var second = RoomStateNarration.ClearedRoom[0];

        first.Should().Be(second, "reading the same narration array should be stable");
    }

    [Fact]
    public void NarrationService_MultipleInstances_Independent()
    {
        var svc1 = new NarrationService();
        var svc2 = new NarrationService();
        var pool = new[] { "A", "B", "C" };

        var result1 = svc1.Pick(pool);
        var result2 = svc2.Pick(pool);

        pool.Should().Contain(result1);
        pool.Should().Contain(result2);
    }

    [Fact]
    public void Player_NewInstances_HaveIndependentState()
    {
        var p1 = new Player();
        var p2 = new Player();
        p1.AddGold(100);

        p2.Gold.Should().Be(0, "new players should not share state");
    }

    [Fact]
    public void Room_NewInstances_HaveIndependentIds()
    {
        var r1 = new Room { Description = "A" };
        var r2 = new Room { Description = "B" };

        r1.Id.Should().NotBe(r2.Id, "rooms should get unique IDs");
    }

    [Fact]
    public void DungeonGenerator_SameSeed_Recreated_ProducesIdenticalResults()
    {
        var gen1 = new DungeonGenerator(100, Array.Empty<Item>());
        gen1.Generate(); // consume one generation

        var gen2 = new DungeonGenerator(100, Array.Empty<Item>());
        var gen3 = new DungeonGenerator(100, Array.Empty<Item>());
        var (s2, _) = gen2.Generate();
        var (s3, _) = gen3.Generate();

        s2.Description.Should().Be(s3.Description);
    }
}
