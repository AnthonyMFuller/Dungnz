using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

/// <summary>
/// #1374 — End-to-end integration tests for the boss loot pipeline.
/// Verifies: DungeonBoss (floor 5) + isBossRoom=true -> Legendary drop.
/// </summary>
[Collection("LootTableTests")]
public class BossLootIntegrationTests : IDisposable
{
    private static readonly Item LegendaryItem1 = new() { Name = "Void Blade",     Tier = ItemTier.Legendary };
    private static readonly Item LegendaryItem2 = new() { Name = "Soul Aegis",     Tier = ItemTier.Legendary };
    private static readonly Item EpicItem1      = new() { Name = "Crimson Mantle", Tier = ItemTier.Epic      };
    private static readonly Item EpicItem2      = new() { Name = "Dread Signet",   Tier = ItemTier.Epic      };
    private static readonly IReadOnlyList<Item> EmptyPool     = Array.Empty<Item>();
    private static readonly IReadOnlyList<Item> LegendaryPool = new[] { LegendaryItem1, LegendaryItem2 };
    private static readonly IReadOnlyList<Item> EpicPool      = new[] { EpicItem1, EpicItem2 };
    private static readonly IReadOnlyList<Item> T1 = new[] { new Item { Name = "Short Sword", Tier = ItemTier.Common   } };
    private static readonly IReadOnlyList<Item> T2 = new[] { new Item { Name = "Iron Axe",    Tier = ItemTier.Uncommon } };
    private static readonly IReadOnlyList<Item> T3 = new[] { new Item { Name = "War Blade",   Tier = ItemTier.Rare     } };

    public void Dispose() => LootTable.SetTierPools(T1, T2, T3);

    // Creates a DungeonBoss with no configured drops so the Legendary pool branch fires
    private static DungeonBoss MakeBossNoDrops() => new DungeonBoss(stats: null, itemConfig: new List<ItemStats>());

    [Fact]
    public void BossRoom_Floor5_DropsLegendaryItem()
    {
        LootTable.SetTierPools(T1, T2, T3, LegendaryPool, EpicPool);
        var boss = MakeBossNoDrops();
        boss.LootTable = new LootTable(new ControlledRandom(defaultDouble: 0.9), minGold: 50, maxGold: 50);

        var result = boss.LootTable.RollDrop(boss, playerLevel: 10, isBossRoom: true, dungeonFloor: 5);

        result.Item.Should().NotBeNull("boss room must yield an item");
        result.Item!.Tier.Should().Be(ItemTier.Legendary, "isBossRoom=true + Legendary pool = guaranteed Legendary");
    }

    [Fact]
    public void BossRoom_Floor5_TenIterations_AllDropsAreLegendary()
    {
        LootTable.SetTierPools(T1, T2, T3, LegendaryPool, EpicPool);
        var boss = MakeBossNoDrops();
        boss.LootTable = new LootTable(new Random(42), minGold: 50, maxGold: 100);

        var drops = new List<Item?>();
        for (int i = 0; i < 10; i++)
            drops.Add(boss.LootTable.RollDrop(boss, playerLevel: 10, isBossRoom: true, dungeonFloor: 5).Item);

        drops.Should().AllSatisfy(item =>
        {
            item.Should().NotBeNull();
            item!.Tier.Should().Be(ItemTier.Legendary, "boss room must guarantee Legendary tier");
        });
    }

    [Fact]
    public void BossRoom_Floor5_AlwaysDropsGold()
    {
        LootTable.SetTierPools(T1, T2, T3, LegendaryPool, EpicPool);
        var boss = MakeBossNoDrops();
        boss.LootTable = new LootTable(new Random(7), minGold: 50, maxGold: 100);

        var result = boss.LootTable.RollDrop(boss, isBossRoom: true, dungeonFloor: 5);

        result.Gold.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public void NonBossRoom_Floor5_CanDropEpicButNotGuaranteedLegendary()
    {
        LootTable.SetTierPools(T1, T2, T3, LegendaryPool, EpicPool);
        var enemy = new EnemyBuilder().WithHP(10).Build();
        var rng   = new ControlledRandom(defaultDouble: 0.05); // 0.05 < 0.08 Epic threshold
        var table = new LootTable(rng, minGold: 5, maxGold: 5);

        var result = table.RollDrop(enemy, playerLevel: 8, isBossRoom: false, dungeonFloor: 5);

        result.Item.Should().NotBeNull();
        result.Item!.Tier.Should().Be(ItemTier.Epic, "floor-5 non-boss with RNG<0.08 should drop Epic");
    }

    [Fact]
    public void CrimsonVampire_BossRoom_DropsLegendaryOrBossKey()
    {
        LootTable.SetTierPools(T1, T2, T3, LegendaryPool, EpicPool);
        var boss   = new CrimsonVampire();
        var result = boss.LootTable!.RollDrop(boss, playerLevel: 10, isBossRoom: true, dungeonFloor: 5);

        result.Item.Should().NotBeNull();
        result.Item!.Name.Should().Be(ItemNames.BossKey, "100% Boss Key drop takes priority over Legendary pool");
    }

    [Fact]
    public void BossRoom_EmptyLegendaryPool_DoesNotCrash()
    {
        LootTable.SetTierPools(T1, T2, T3, EmptyPool, EmptyPool);
        var boss = MakeBossNoDrops();
        boss.LootTable = new LootTable(new Random(1), minGold: 10, maxGold: 10);

        var act = () => boss.LootTable.RollDrop(boss, isBossRoom: true, dungeonFloor: 5);

        act.Should().NotThrow("empty Legendary pool with isBossRoom=true must not crash");
    }

    [Fact]
    public void CombatEngine_KillDungeonBoss_Floor5_RecordsLootInDisplay()
    {
        LootTable.SetTierPools(T1, T2, T3, LegendaryPool, EpicPool);
        var player = new PlayerBuilder().WithHP(200).WithMaxHP(200).WithAttack(9999).WithDefense(10).Build();
        var boss   = MakeBossNoDrops();
        boss.HP     = 1;
        boss.MaxHP  = 1;
        boss.Attack = 1;
        boss.Defense = 0;
        boss.LootTable = new LootTable(new Random(42), minGold: 50, maxGold: 100);

        var display = new FakeDisplayService();
        var input   = new FakeInputReader("A");
        var engine  = new CombatEngine(display, input, new Random(42));
        engine.DungeonFloor = 5;

        var result = engine.RunCombat(player, boss);

        result.Should().Be(CombatResult.Won);
        display.AllOutput.Should().Contain(o => o.StartsWith("loot:"), "loot must be recorded after boss kill");
    }
}
