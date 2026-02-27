using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class CraftingSystemTests
{
    private static Player MakePlayer(int gold = 100)
    {
        return new Player { Gold = gold };
    }

    private static Item MakeItem(string name) => new Item { Name = name, Type = ItemType.Consumable };

    // Items with canonical Ids that match recipe ingredient ItemIds
    private static Item MakeHealthPotion() => new Item { Id = "health-potion", Name = "Health Potion", Type = ItemType.Consumable };
    private static Item MakeIronSword() => new Item { Id = "iron-sword", Name = "Iron Sword", Type = ItemType.Weapon };
    private static Item MakeLeatherArmor() => new Item { Id = "leather-armor", Name = "Leather Armor", Type = ItemType.Armor };

    [Fact]
    public void TryCraft_WithValidIngredients_Succeeds()
    {
        var player = MakePlayer(gold: 30);
        player.Inventory.Add(MakeIronSword());

        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Reinforced Sword");
        var (success, message) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeTrue();
        message.Should().Contain("Reinforced Sword");
        player.Inventory.Should().ContainSingle(i => i.Name == "Reinforced Sword");
        player.Inventory.Should().NotContain(i => i.Name == "Iron Sword");
        player.Gold.Should().Be(0);
    }

    [Fact]
    public void TryCraft_MissingIngredient_Fails_ReturnsMessage()
    {
        var player = MakePlayer(gold: 50);
        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Reinforced Sword");

        var (success, message) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeFalse();
        message.Should().Contain("Iron Sword");
        player.Inventory.Should().BeEmpty();
    }

    [Fact]
    public void TryCraft_InsufficientGold_Fails()
    {
        var player = MakePlayer(gold: 10);
        player.Inventory.Add(MakeIronSword());

        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Reinforced Sword");
        var (success, message) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeFalse();
        message.Should().Contain("gold");
        player.Inventory.Should().ContainSingle(i => i.Name == "Iron Sword");
    }

    [Fact]
    public void TryCraft_InventoryAtCapacity_Fails()
    {
        // Inventory starts at MaxInventorySize + 2 (2 potions + MaxInventorySize others).
        // After removing the 2 potions, MaxInventorySize items remain — still full — so crafting fails (#555).
        var player = MakePlayer(gold: 0);
        for (int i = 0; i < Player.MaxInventorySize; i++)
            player.Inventory.Add(MakeItem($"Item{i}"));
        // Force-add ingredients beyond normal cap to simulate the post-removal still-full edge case
        player.Inventory.Add(MakeHealthPotion());
        player.Inventory.Add(MakeHealthPotion());

        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Health Elixir");
        var (success, message) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeFalse();
        message.Should().Contain("full");
        // Ingredients must have been re-added
        player.Inventory.Count(i => i.Id == "health-potion").Should().Be(2);
    }

    [Fact]
    public void TryCraft_CaseInsensitive_RecipeName()
    {
        // Verify ingredient matching uses item.Id (not DisplayName), case-insensitive not required for Id
        var player = MakePlayer(gold: 0);
        player.Inventory.Add(MakeHealthPotion());
        player.Inventory.Add(MakeHealthPotion());

        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Health Elixir");
        var (success, _) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeTrue();
    }

    [Fact]
    public void TryCraft_InvalidRecipeName_IngredientCheckFails()
    {
        // A recipe requiring an item the player doesn't have returns an appropriate error
        var player = MakePlayer(gold: 0);
        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Health Elixir");

        var (success, message) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeFalse();
        message.Should().Contain("Health Potion");
    }

    [Fact]
    public void TryCraft_ReinforcedArmorRecipe_ConsumesLeatherArmor_NotIronSword()
    {
        var player = MakePlayer(gold: 25);
        player.Inventory.Add(MakeLeatherArmor());
        player.Inventory.Add(MakeIronSword());

        var recipe = CraftingSystem.Recipes.First(r => r.Name == "Reinforced Armor");
        var (success, _) = CraftingSystem.TryCraft(player, recipe);

        success.Should().BeTrue();
        player.Inventory.Should().NotContain(i => i.Name == "Leather Armor");
        player.Inventory.Should().Contain(i => i.Name == "Iron Sword");
        player.Inventory.Should().Contain(i => i.Name == "Reinforced Armor");
    }
}
