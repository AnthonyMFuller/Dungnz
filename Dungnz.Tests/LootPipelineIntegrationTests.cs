using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class LootPipelineIntegrationTests
{
    [Fact]
    public void Loot_GuaranteedDrop_AppendedToInventoryAfterCombatWin()
    {
        var display = new FakeDisplayService();
        var sword = new Item { Name = "Relic Blade", Type = ItemType.Weapon, AttackBonus = 7 };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        enemy.LootTable.AddDrop(sword, 1.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        var engine = new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9));
        engine.RunCombat(player, enemy).Should().Be(CombatResult.Won);
        player.Inventory.Should().Contain(i => i.Name == "Relic Blade");
    }

    [Fact]
    public void Loot_GoldDrop_AddsToPlayerGold()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 50, maxGold: 50);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).WithGold(0).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.Gold.Should().Be(50);
    }

    [Fact]
    public void Loot_EquipDroppedWeapon_IncreasesPlayerAttack()
    {
        var display = new FakeDisplayService();
        var weapon = new Item { Name = "Power Axe", Type = ItemType.Weapon, AttackBonus = 12, IsEquippable = true };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        enemy.LootTable.AddDrop(weapon, 1.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(10).Build();
        var baseAttack = player.Attack;
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        var looted = player.Inventory.Find(i => i.Name == "Power Axe");
        looted.Should().NotBeNull();
        player.EquipItem(looted!);
        player.Attack.Should().Be(baseAttack + 12);
    }

    [Fact]
    public void Loot_EquipDroppedArmor_IncreasesPlayerDefense()
    {
        var display = new FakeDisplayService();
        var armor = new Item { Name = "Iron Chestplate", Type = ItemType.Armor, DefenseBonus = 8, IsEquippable = true, Slot = ArmorSlot.Chest };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        enemy.LootTable.AddDrop(armor, 1.0);
        var player = new PlayerBuilder().WithHP(100).WithDefense(5).Build();
        var baseDef = player.Defense;
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        var looted = player.Inventory.Find(i => i.Name == "Iron Chestplate");
        player.EquipItem(looted!);
        player.Defense.Should().Be(baseDef + 8);
    }

    [Fact]
    public void Equip_UnequipWeapon_AttackReverts()
    {
        var player = new PlayerBuilder().WithHP(100).WithAttack(10).Build();
        var weapon = new Item { Name = "Test Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        player.Inventory.Add(weapon);
        var baseAttack = player.Attack;
        player.EquipItem(weapon);
        player.Attack.Should().Be(baseAttack + 5);
        player.UnequipItem("weapon");
        player.Attack.Should().Be(baseAttack);
    }

    [Fact]
    public void Loot_ZeroChanceDrop_NeverOccurs()
    {
        var display = new FakeDisplayService();
        var rareItem = new Item { Name = "Mythic Gem", Type = ItemType.Consumable };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        enemy.LootTable.AddDrop(rareItem, 0.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.Inventory.Should().NotContain(i => i.Name == "Mythic Gem");
    }

    [Fact]
    public void Loot_TwoEnemiesSequential_GoldStacks()
    {
        var display = new FakeDisplayService();
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).WithGold(0).Build();
        var e1 = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        e1.LootTable = new LootTable(new Random(1), minGold: 20, maxGold: 20);
        new CombatEngine(display, new FakeInputReader("A"), rng).RunCombat(player, e1);
        var goldAfterFirst = player.Gold;
        var e2 = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        e2.LootTable = new LootTable(new Random(2), minGold: 30, maxGold: 30);
        new CombatEngine(display, new FakeInputReader("A"), rng).RunCombat(player, e2);
        goldAfterFirst.Should().Be(20);
        player.Gold.Should().Be(50);
    }

    [Fact]
    public void Loot_AfterCombatWin_RunStatsEnemiesDefeatedIncremented()
    {
        var display = new FakeDisplayService();
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        var stats = new RunStats();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy, stats);
        stats.EnemiesDefeated.Should().Be(1);
    }

    [Fact]
    public void Loot_GuaranteedItemDrops_NeverDropsZeroProbabilityItem()
    {
        var display = new FakeDisplayService();
        var guaranteed = new Item { Name = "Certain Drop", Type = ItemType.Consumable };
        var never = new Item { Name = "Never Drop", Type = ItemType.Consumable };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        enemy.LootTable.AddDrop(guaranteed, 1.0);
        enemy.LootTable.AddDrop(never, 0.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.Inventory.Should().Contain(i => i.Name == "Certain Drop");
        player.Inventory.Should().NotContain(i => i.Name == "Never Drop");
    }

    [Fact]
    public void Loot_BossGoldRange_WithinConfiguredMinMax()
    {
        var display = new FakeDisplayService();
        var boss = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 100);
        boss.LootTable = new LootTable(new Random(42), minGold: 100, maxGold: 200);
        var player = new PlayerBuilder().WithHP(100).WithAttack(50).WithGold(0).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, boss);
        player.Gold.Should().BeInRange(100, 200);
    }

    [Fact]
    public void LootPipeline_FullFlow_CombatToEquippedStatBonus()
    {
        var display = new FakeDisplayService();
        var helmet = new Item { Name = "Steel Helmet", Type = ItemType.Armor, DefenseBonus = 5, IsEquippable = true, Slot = ArmorSlot.Head };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 25);
        enemy.LootTable = new LootTable(new Random(42), minGold: 10, maxGold: 10);
        enemy.LootTable.AddDrop(helmet, 1.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(30).WithDefense(0).Build();
        var result = new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        var looted = player.Inventory.Find(i => i.Name == "Steel Helmet");
        player.EquipItem(looted!);
        result.Should().Be(CombatResult.Won);
        player.Gold.Should().Be(10);
        player.Defense.Should().Be(5);
    }

    [Fact]
    public void Loot_InventoryFull_CombatCompletesWithoutException()
    {
        var display = new FakeDisplayService();
        var prize = new Item { Name = "Overflow Prize", Type = ItemType.Consumable };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 0, maxGold: 0);
        enemy.LootTable.AddDrop(prize, 1.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).Build();
        for (int i = 0; i < Player.MaxInventorySize; i++)
            player.Inventory.Add(new Item { Name = $"Filler {i}", Type = ItemType.Consumable });
        var act = () => new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        act.Should().NotThrow();
    }

    [Fact]
    public void Loot_GoldAndItem_BothApplied()
    {
        var display = new FakeDisplayService();
        var ring = new Item { Name = "Lucky Ring", Type = ItemType.Accessory };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 10);
        enemy.LootTable = new LootTable(new Random(42), minGold: 15, maxGold: 15);
        enemy.LootTable.AddDrop(ring, 1.0);
        var player = new PlayerBuilder().WithHP(100).WithAttack(20).WithGold(0).Build();
        new CombatEngine(display, new FakeInputReader("A"), new ControlledRandom(defaultDouble: 0.9)).RunCombat(player, enemy);
        player.Gold.Should().Be(15);
        player.Inventory.Should().Contain(i => i.Name == "Lucky Ring");
    }
}
