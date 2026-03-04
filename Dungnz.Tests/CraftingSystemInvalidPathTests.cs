using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for CraftingSystem invalid recipe paths (#950):
/// crafting with missing ingredients, wrong quantities, unknown recipe, full inventory.
/// </summary>
public class CraftingSystemInvalidPathTests
{
    private static Player MakePlayer(int gold = 100) => new PlayerBuilder().WithGold(gold).Build();

    private static Item MakeHealthPotion() => new Item
    {
        Id = "health-potion", Name = "Health Potion", Type = ItemType.Consumable
    };

    private static Item MakeIronSword() => new Item
    {
        Id = "iron-sword", Name = "Iron Sword", Type = ItemType.Weapon, IsEquippable = true
    };

    private static CraftingRecipe HealthElixirRecipe =>
        CraftingSystem.Recipes.First(r => r.Name == "Health Elixir");

    private static CraftingRecipe ReinforcedSwordRecipe =>
        CraftingSystem.Recipes.First(r => r.Name == "Reinforced Sword");

    // ── Missing ingredients entirely ──────────────────────────────────────────

    [Fact]
    public void TryCraft_NoIngredients_Fails()
    {
        var player = MakePlayer(gold: 0);

        var (success, message) = CraftingSystem.TryCraft(player, HealthElixirRecipe);

        success.Should().BeFalse();
        message.Should().Contain("Health Potion");
        player.Inventory.Should().BeEmpty();
    }

    // ── Wrong quantity of ingredients ──────────────────────────────────────────

    [Fact]
    public void TryCraft_OneOfTwoRequired_FailsWithQuantityMessage()
    {
        var player = MakePlayer(gold: 0);
        player.Inventory.Add(MakeHealthPotion()); // need 2, have 1

        var (success, message) = CraftingSystem.TryCraft(player, HealthElixirRecipe);

        success.Should().BeFalse();
        message.Should().Contain("2"); // Need 2x
        message.Should().Contain("1"); // have 1
        player.Inventory.Should().HaveCount(1); // ingredient not consumed
    }

    // ── Unknown/null recipe ───────────────────────────────────────────────────

    [Fact]
    public void TryCraft_NullRecipe_ReturnsFalseWithMessage()
    {
        var player = MakePlayer();

        var (success, message) = CraftingSystem.TryCraft(player, null!);

        success.Should().BeFalse();
        message.Should().Contain("Unknown");
    }

    [Fact]
    public void TryCraft_NullPlayer_ThrowsArgumentNullException()
    {
        var recipe = HealthElixirRecipe;

        var act = () => CraftingSystem.TryCraft(null!, recipe);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── Insufficient gold ─────────────────────────────────────────────────────

    [Fact]
    public void TryCraft_InsufficientGold_FailsAndKeepsIngredients()
    {
        var player = MakePlayer(gold: 5); // Reinforced Sword needs 30
        player.Inventory.Add(MakeIronSword());

        var (success, message) = CraftingSystem.TryCraft(player, ReinforcedSwordRecipe);

        success.Should().BeFalse();
        message.Should().Contain("gold");
        player.Inventory.Should().ContainSingle(i => i.Name == "Iron Sword");
        player.Gold.Should().Be(5);
    }

    // ── Full inventory ────────────────────────────────────────────────────────

    [Fact]
    public void TryCraft_FullInventory_Fails()
    {
        var player = MakePlayer(gold: 0);
        for (int i = 0; i < Player.MaxInventorySize; i++)
            player.Inventory.Add(new Item { Name = $"Junk{i}", Type = ItemType.Consumable });

        var (success, message) = CraftingSystem.TryCraft(player, HealthElixirRecipe);

        success.Should().BeFalse();
        message.Should().Contain("full");
    }

    // ── Successful craft consumes ingredients and gold ─────────────────────────

    [Fact]
    public void TryCraft_ValidIngredients_ConsumesIngredientsAndGold()
    {
        var player = MakePlayer(gold: 30);
        player.Inventory.Add(MakeIronSword());

        var (success, message) = CraftingSystem.TryCraft(player, ReinforcedSwordRecipe);

        success.Should().BeTrue();
        player.Gold.Should().Be(0);
        player.Inventory.Should().NotContain(i => i.Name == "Iron Sword");
        player.Inventory.Should().ContainSingle(i => i.Name == "Reinforced Sword");
    }

    // ── Crafting with zero gold cost succeeds without gold ─────────────────────

    [Fact]
    public void TryCraft_ZeroGoldCostRecipe_SucceedsWithNoGold()
    {
        var player = MakePlayer(gold: 0);
        player.Inventory.Add(MakeHealthPotion());
        player.Inventory.Add(MakeHealthPotion());

        var (success, _) = CraftingSystem.TryCraft(player, HealthElixirRecipe);

        success.Should().BeTrue();
        player.Gold.Should().Be(0);
    }

    // ── Crafting with extra ingredients only consumes required count ───────────

    [Fact]
    public void TryCraft_ExtraIngredients_OnlyConsumesRequired()
    {
        var player = MakePlayer(gold: 0);
        player.Inventory.Add(MakeHealthPotion());
        player.Inventory.Add(MakeHealthPotion());
        player.Inventory.Add(MakeHealthPotion()); // extra

        var (success, _) = CraftingSystem.TryCraft(player, HealthElixirRecipe);

        success.Should().BeTrue();
        player.Inventory.Count(i => i.Name == "Health Potion").Should().Be(1);
    }
}
