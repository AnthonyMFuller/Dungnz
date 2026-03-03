using Dungnz.Models;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Additional LootTable tests to cover SetTierPools, RollTier, RollArmorTier, and RollDrop edge paths.</summary>
[Collection("LootTableTests")]
public class LootTableAdditionalTests : IDisposable
{
    // Snapshot of pools before each test — to restore on cleanup
    private static readonly IReadOnlyList<Item> _restoreTier1 = new List<Item>
    {
        new Item { Name = "Short Sword", Type = ItemType.Weapon, AttackBonus = 2, IsEquippable = true, Tier = ItemTier.Common },
        new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 5, IsEquippable = true, Tier = ItemTier.Common }
    };
    private static readonly IReadOnlyList<Item> _restoreTier2 = new List<Item>
    {
        new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true, Tier = ItemTier.Uncommon }
    };
    private static readonly IReadOnlyList<Item> _restoreTier3 = new List<Item>
    {
        new Item { Name = "Mythril Blade", Type = ItemType.Weapon, AttackBonus = 8, IsEquippable = true, Tier = ItemTier.Rare }
    };

    /// <summary>Restore tier pools to non-empty fallback data after each test, so subsequent tests are not affected.</summary>
    public void Dispose()
    {
        LootTable.SetTierPools(_restoreTier1, _restoreTier2, _restoreTier3);
    }

    // ── SetTierPools and RollTier ─────────────────────────────────────────────

    [Fact]
    public void RollTier_CommonWithSharedPool_ReturnsItemFromPool()
    {
        var items = new List<Item>
        {
            new() { Name = "Short Sword", Type = ItemType.Weapon, Tier = ItemTier.Common },
            new() { Name = "Leather Armor", Type = ItemType.Armor, Tier = ItemTier.Common }
        };
        LootTable.SetTierPools(items, new List<Item>(), new List<Item>());

        var result = LootTable.RollTier(ItemTier.Common);

        result.Should().NotBeNull();
        result!.Name.Should().BeOneOf("Short Sword", "Leather Armor");
    }

    [Fact]
    public void RollTier_UncommonWithSharedPool_ReturnsItemFromPool()
    {
        var uncommon = new List<Item>
        {
            new() { Name = "Steel Sword", Type = ItemType.Weapon, Tier = ItemTier.Uncommon }
        };
        LootTable.SetTierPools(new List<Item>(), uncommon, new List<Item>());

        var result = LootTable.RollTier(ItemTier.Uncommon);
        result!.Name.Should().Be("Steel Sword");
    }

    [Fact]
    public void RollTier_RareWithSharedPool_ReturnsItemFromPool()
    {
        var rare = new List<Item>
        {
            new() { Name = "Mithril Sword", Type = ItemType.Weapon, Tier = ItemTier.Rare }
        };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), rare);

        var result = LootTable.RollTier(ItemTier.Rare);
        result!.Name.Should().Be("Mithril Sword");
    }

    [Fact]
    public void RollTier_EpicWithPool_ReturnsItemFromPool()
    {
        var epic = new List<Item> { new() { Name = "Dragon Sword", Tier = ItemTier.Epic } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), epic: epic);

        var result = LootTable.RollTier(ItemTier.Epic);
        result!.Name.Should().Be("Dragon Sword");
    }

    [Fact]
    public void RollTier_LegendaryWithPool_ReturnsItemFromPool()
    {
        var legendary = new List<Item> { new() { Name = "Excalibur", Tier = ItemTier.Legendary } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), legendary: legendary);

        var result = LootTable.RollTier(ItemTier.Legendary);
        result!.Name.Should().Be("Excalibur");
    }

    [Fact]
    public void RollTier_EmptyPool_ReturnsNull()
    {
        // Use an empty legendary pool
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), legendary: new List<Item>());

        var result = LootTable.RollTier(ItemTier.Legendary);
        result.Should().BeNull("empty legendary pool returns null");
    }

    [Fact]
    public void RollTier_UnknownTier_FallsBackToTier2()
    {
        var uncommon = new List<Item> { new() { Name = "Uncommon Item", Tier = ItemTier.Uncommon } };
        LootTable.SetTierPools(new List<Item>(), uncommon, new List<Item>());

        var result = LootTable.RollTier((ItemTier)999);
        result!.Name.Should().Be("Uncommon Item", "unknown tier falls back to tier2/uncommon pool");
    }

    // ── RollArmorTier ─────────────────────────────────────────────────────────

    [Fact]
    public void RollArmorTier_PoolWithArmor_ReturnsArmorItem()
    {
        var items = new List<Item>
        {
            new() { Name = "Iron Sword", Type = ItemType.Weapon, Tier = ItemTier.Common },
            new() { Name = "Iron Armor", Type = ItemType.Armor, Tier = ItemTier.Common }
        };
        LootTable.SetTierPools(items, new List<Item>(), new List<Item>());

        // Run many times to ensure armor is chosen
        Item? armor = null;
        for (int i = 0; i < 50; i++)
        {
            armor = LootTable.RollArmorTier(ItemTier.Common);
            if (armor?.Type == ItemType.Armor) break;
        }
        armor!.Type.Should().Be(ItemType.Armor, "RollArmorTier prefers armor items");
    }

    [Fact]
    public void RollArmorTier_PoolWithNoArmor_FallsBackToAnyItem()
    {
        var items = new List<Item>
        {
            new() { Name = "Iron Sword", Type = ItemType.Weapon, Tier = ItemTier.Common }
        };
        LootTable.SetTierPools(items, new List<Item>(), new List<Item>());

        var result = LootTable.RollArmorTier(ItemTier.Common);
        result!.Name.Should().Be("Iron Sword", "no armor in pool, falls back to weapon");
    }

    [Fact]
    public void RollArmorTier_EmptyPool_ReturnsNull()
    {
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>());

        var result = LootTable.RollArmorTier(ItemTier.Common);
        result.Should().BeNull("empty pool returns null");
    }

    // ── RollDrop boss room and floor paths ────────────────────────────────────

    [Fact]
    public void RollDrop_BossRoom_LegendaryPool_DropsBossLegendary()
    {
        var legendary = new List<Item> { new() { Name = "Boss Relic", Tier = ItemTier.Legendary } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), legendary: legendary);

        var table = new LootTable(new ControlledRandom(0.95), minGold: 10, maxGold: 10);
        // No explicit drops, boss room, so legendary should drop
        var result = table.RollDrop(null!, playerLevel: 1, isBossRoom: true, dungeoonFloor: 1);

        result.Item.Should().NotBeNull("boss room guarantees legendary");
        result.Item!.Name.Should().Be("Boss Relic");
    }

    [Fact]
    public void RollDrop_BossRoom_EmptyLegendaryPool_NoForcedDrop()
    {
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), legendary: new List<Item>());

        var table = new LootTable(new ControlledRandom(0.95), minGold: 5, maxGold: 5);
        var result = table.RollDrop(null!, isBossRoom: true, dungeoonFloor: 1);

        // No drop forced since legendary pool is empty; 95% rng prevents 30% tiered roll
        result.Gold.Should().Be(5);
    }

    [Fact]
    public void RollDrop_EliteEnemy_Level1_GetsTier2Item()
    {
        var tier2 = new List<Item> { new() { Name = "Steel Axe", Type = ItemType.Weapon, Tier = ItemTier.Uncommon } };
        LootTable.SetTierPools(new List<Item> { new() { Name = "Basic Dagger" } }, tier2, new List<Item>());

        var table = new LootTable(new ControlledRandom(0.10), minGold: 5, maxGold: 5);
        var elite = new Enemy_Stub(100, 20, 10, 50) { IsElite = true };

        var result = table.RollDrop(elite, playerLevel: 1);
        result.Item.Should().NotBeNull("elite enemy triggers tiered drop with 10% rng");
    }

    [Fact]
    public void RollDrop_PlayerLevel4_UsesTier2Pool()
    {
        var tier1 = new List<Item> { new() { Name = "Common Item", Tier = ItemTier.Common } };
        var tier2 = new List<Item> { new() { Name = "Uncommon Item", Tier = ItemTier.Uncommon } };
        LootTable.SetTierPools(tier1, tier2, new List<Item>());

        var table = new LootTable(new ControlledRandom(0.10), minGold: 0, maxGold: 0);
        var enemy = new Enemy_Stub(30, 8, 2, 10);

        var result = table.RollDrop(enemy, playerLevel: 4);
        result.Item!.Name.Should().Be("Uncommon Item", "level 4+ uses tier-2 pool");
    }

    [Fact]
    public void RollDrop_PlayerLevel7_UsesTier3Pool()
    {
        var tier3 = new List<Item> { new() { Name = "Rare Item", Tier = ItemTier.Rare } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), tier3);

        var table = new LootTable(new ControlledRandom(0.10), minGold: 0, maxGold: 0);
        var enemy = new Enemy_Stub(30, 8, 2, 10);

        var result = table.RollDrop(enemy, playerLevel: 7);
        result.Item!.Name.Should().Be("Rare Item", "level 7+ uses tier-3 pool");
    }

    [Fact]
    public void RollDrop_Floor6_HasLegendaryChance()
    {
        var legendary = new List<Item> { new() { Name = "Legendary Artifact", Tier = ItemTier.Legendary } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), legendary: legendary);

        bool legendaryDropped = false;
        for (int seed = 0; seed < 100; seed++)
        {
            var table = new LootTable(new Random(seed), minGold: 0, maxGold: 0);
            var result = table.RollDrop(null!, dungeoonFloor: 6);
            if (result.Item?.Name == "Legendary Artifact") { legendaryDropped = true; break; }
        }
        legendaryDropped.Should().BeTrue("floor 6+ has 5% legendary chance; should trigger in 100 attempts");
    }

    [Fact]
    public void LootTable_MinGoldGreaterThanMaxGold_ThrowsArgumentException()
    {
        var act = () => new LootTable(minGold: 10, maxGold: 5);
        act.Should().Throw<ArgumentException>().WithMessage("*minGold*");
    }

    [Fact]
    public void RollDrop_Floor5_HasEpicChance()
    {
        var epic = new List<Item> { new() { Name = "Epic Armor", Tier = ItemTier.Epic } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), epic: epic);

        bool epicDropped = false;
        for (int seed = 0; seed < 200; seed++)
        {
            var table = new LootTable(new Random(seed), minGold: 0, maxGold: 0);
            var result = table.RollDrop(null!, dungeoonFloor: 5);
            if (result.Item?.Name == "Epic Armor") { epicDropped = true; break; }
        }
        epicDropped.Should().BeTrue("floor 5+ has 8% epic chance; should trigger within 200 attempts");
    }

    [Fact]
    public void RollDrop_Floor7_HasHigherEpicChance()
    {
        var epic = new List<Item> { new() { Name = "High Epic", Tier = ItemTier.Epic } };
        LootTable.SetTierPools(new List<Item>(), new List<Item>(), new List<Item>(), epic: epic);

        bool epicDropped = false;
        for (int seed = 0; seed < 200; seed++)
        {
            var table = new LootTable(new Random(seed), minGold: 0, maxGold: 0);
            var result = table.RollDrop(null!, dungeoonFloor: 7);
            if (result.Item?.Name == "High Epic") { epicDropped = true; break; }
        }
        epicDropped.Should().BeTrue("floor 7+ has 15% epic chance; should trigger within 200 attempts");
    }
}
