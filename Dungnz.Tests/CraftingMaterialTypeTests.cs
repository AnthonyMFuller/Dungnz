using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Regression tests for the CraftingMaterial ItemType introduced in Issue #671.
/// Verifies that crafting materials are excluded from the USE menu filter and that
/// the 9 reclassified items (goblin-ear, skeleton-dust, troll-blood, wraith-essence,
/// dragon-scale, wyvern-fang, soul-gem, iron-ore, rodent-pelt) have the correct type.
/// </summary>
public class CraftingMaterialTypeTests
{
    private static Player MakePlayer()
    {
        return new Player
        {
            Name = "TestHero",
            HP = 50,
            MaxHP = 100,
            Attack = 10,
            Defense = 5
        };
    }

    [Fact]
    public void CraftingMaterial_Items_NotInUseMenu()
    {
        // Arrange — player with only CraftingMaterial items
        var player = MakePlayer();
        player.Inventory.Add(new Item { Name = "Goblin Ear", Type = ItemType.CraftingMaterial });
        player.Inventory.Add(new Item { Name = "Dragon Scale", Type = ItemType.CraftingMaterial });
        player.Inventory.Add(new Item { Name = "Iron Ore", Type = ItemType.CraftingMaterial });

        // Act — apply the USE menu filter
        var usableItems = player.Inventory.Where(i => i.Type == ItemType.Consumable).ToList();

        // Assert — crafting materials are excluded
        usableItems.Should().BeEmpty("CraftingMaterial items should not appear in the USE menu");
    }

    [Fact]
    public void Consumable_Items_AreInUseMenu()
    {
        // Arrange — player with consumables that have HealAmount > 0
        var player = MakePlayer();
        var healthPotion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        var manaPotion = new Item { Name = "Mana Potion", Type = ItemType.Consumable, ManaRestore = 20 };
        player.Inventory.Add(healthPotion);
        player.Inventory.Add(manaPotion);

        // Act — apply the USE menu filter
        var usableItems = player.Inventory.Where(i => i.Type == ItemType.Consumable).ToList();

        // Assert — consumables appear in the filter
        usableItems.Should().HaveCount(2, "Consumable items should appear in the USE menu");
        usableItems.Should().Contain(healthPotion);
        usableItems.Should().Contain(manaPotion);
    }

    [Fact]
    public void CraftingMaterial_And_Consumable_Mixed()
    {
        // Arrange — player with both CraftingMaterial and Consumable items
        var player = MakePlayer();
        var craftingItem1 = new Item { Name = "Troll Blood", Type = ItemType.CraftingMaterial };
        var craftingItem2 = new Item { Name = "Wraith Essence", Type = ItemType.CraftingMaterial };
        var healthPotion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        var elixir = new Item { Name = "Elixir", Type = ItemType.Consumable, HealAmount = 50 };

        player.Inventory.Add(craftingItem1);
        player.Inventory.Add(healthPotion);
        player.Inventory.Add(craftingItem2);
        player.Inventory.Add(elixir);

        // Act — apply the USE menu filter
        var usableItems = player.Inventory.Where(i => i.Type == ItemType.Consumable).ToList();

        // Assert — only consumables appear
        usableItems.Should().HaveCount(2, "only Consumable items should appear in the USE menu");
        usableItems.Should().Contain(healthPotion);
        usableItems.Should().Contain(elixir);
        usableItems.Should().NotContain(craftingItem1);
        usableItems.Should().NotContain(craftingItem2);
    }

    [Fact]
    public void ItemType_CraftingMaterial_EnumExists()
    {
        // Act — verify the enum value exists and is distinct
        var craftingMaterialValue = ItemType.CraftingMaterial;

        // Assert
        craftingMaterialValue.Should().NotBe(ItemType.Consumable, "CraftingMaterial should be distinct from Consumable");
        craftingMaterialValue.Should().NotBe(ItemType.Weapon, "CraftingMaterial should be distinct from Weapon");
        craftingMaterialValue.Should().NotBe(ItemType.Armor, "CraftingMaterial should be distinct from Armor");
        craftingMaterialValue.Should().NotBe(ItemType.Accessory, "CraftingMaterial should be distinct from Accessory");
        craftingMaterialValue.Should().NotBe(ItemType.Gold, "CraftingMaterial should be distinct from Gold");
        
        // Verify it can be parsed from string
        Enum.TryParse<ItemType>("CraftingMaterial", ignoreCase: true, out var parsed).Should().BeTrue();
        parsed.Should().Be(ItemType.CraftingMaterial);
    }

    [Fact]
    public void KnownCraftingMaterials_HaveCorrectType()
    {
        // Arrange — load item-stats.json through ItemConfig
        var itemStatsPath = Path.Combine("Data", "item-stats.json");
        var allItemStats = ItemConfig.Load(itemStatsPath);

        // Act — convert to Item instances and filter for the 9 reclassified items
        var allItems = allItemStats.Select(ItemConfig.CreateItem).ToList();
        var reclassifiedIds = new[]
        {
            "goblin-ear",
            "skeleton-dust",
            "troll-blood",
            "wraith-essence",
            "dragon-scale",
            "wyvern-fang",
            "soul-gem",
            "iron-ore",
            "rodent-pelt"
        };

        var craftingMaterials = allItems
            .Where(i => reclassifiedIds.Contains(i.Id))
            .ToList();

        // Assert — all 9 items should have Type == CraftingMaterial
        craftingMaterials.Should().HaveCount(9, "all 9 reclassified items should be found in item-stats.json");
        
        foreach (var item in craftingMaterials)
        {
            item.Type.Should().Be(ItemType.CraftingMaterial,
                $"item '{item.Name}' (id: {item.Id}) should have Type == CraftingMaterial");
        }

        // Spot-check specific items mentioned in the charter
        var goblinEar = craftingMaterials.FirstOrDefault(i => i.Id == "goblin-ear");
        var skeletonDust = craftingMaterials.FirstOrDefault(i => i.Id == "skeleton-dust");
        var dragonScale = craftingMaterials.FirstOrDefault(i => i.Id == "dragon-scale");
        var ironOre = craftingMaterials.FirstOrDefault(i => i.Id == "iron-ore");

        goblinEar.Should().NotBeNull("goblin-ear should exist in item-stats.json");
        goblinEar!.Type.Should().Be(ItemType.CraftingMaterial);

        skeletonDust.Should().NotBeNull("skeleton-dust should exist in item-stats.json");
        skeletonDust!.Type.Should().Be(ItemType.CraftingMaterial);

        dragonScale.Should().NotBeNull("dragon-scale should exist in item-stats.json");
        dragonScale!.Type.Should().Be(ItemType.CraftingMaterial);

        ironOre.Should().NotBeNull("iron-ore should exist in item-stats.json");
        ironOre!.Type.Should().Be(ItemType.CraftingMaterial);
    }

    [Fact]
    public void CraftingMaterial_ItemTypeIcon_IsAlembic()
    {
        // Arrange — CraftingMaterial item
        var craftingItem = new Item
        {
            Name = "Wyvern Fang",
            Type = ItemType.CraftingMaterial
        };

        // Note: DisplayService.ItemTypeIcon is private, but we can test indirectly
        // by capturing console output from methods that use it.
        // We'll create a simple test that verifies the icon via ShowLootDrop.

        using var consoleCapture = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleCapture);

        try
        {
            var display = new ConsoleDisplayService();
            var player = MakePlayer();

            // Act — show a loot drop which internally calls ItemTypeIcon
            display.ShowLootDrop(craftingItem, player);

            // Assert — captured output should contain the alembic icon (⚗)
            var output = consoleCapture.ToString();
            output.Should().Contain("⚗", "CraftingMaterial items should display the alembic icon (⚗)");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
