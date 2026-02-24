namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

// Internal DTO used only for deserialising the top-level JSON wrapper.
internal record RecipeConfigData
{
    public List<CraftingRecipe> Recipes { get; init; } = new();
}

/// <summary>
/// Provides the list of available crafting recipes and the logic for attempting
/// to craft an item from a player's inventory and gold. Recipes are loaded from
/// <c>Data/crafting-recipes.json</c> via <see cref="Load"/>; a set of built-in
/// defaults is used as a fallback when the file has not been loaded.
/// </summary>
public static class CraftingSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Gets all crafting recipes currently available in the game.</summary>
    public static List<CraftingRecipe> Recipes { get; private set; } = BuildDefaultRecipes();

    /// <summary>
    /// Replaces <see cref="Recipes"/> with the recipes defined in the specified JSON file.
    /// If the file does not exist, the existing recipes are left unchanged.
    /// </summary>
    /// <param name="path">Path to the crafting-recipes.json file.</param>
    public static void Load(string path = "Data/crafting-recipes.json")
    {
        if (!File.Exists(path))
            return;

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<RecipeConfigData>(json, JsonOptions);
        if (data?.Recipes is { Count: > 0 })
            Recipes = data.Recipes;
    }

    /// <summary>
    /// Attempts to craft the given recipe for the specified player, consuming ingredients
    /// and gold from the player's inventory if all requirements are met.
    /// </summary>
    /// <param name="player">The player attempting the craft.</param>
    /// <param name="recipe">The recipe to craft.</param>
    /// <returns>
    /// A tuple of <c>success</c> (<see langword="true"/> if crafting succeeded) and a
    /// <c>message</c> describing the outcome or the reason for failure.
    /// </returns>
    public static (bool success, string message) TryCraft(Player player, CraftingRecipe recipe)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (recipe == null) return (false, "Unknown recipe.");

        // Check inventory capacity
        if (player.Inventory.Count >= Player.MaxInventorySize)
            return (false, "Your inventory is full.");

        // Check ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            var held = player.Inventory.Count(i => i.Name.Equals(ingredient.DisplayName, StringComparison.OrdinalIgnoreCase));
            if (held < ingredient.Count)
                return (false, $"Need {ingredient.Count}x {ingredient.DisplayName} (have {held}).");
        }
        // Check gold
        if (player.Gold < recipe.GoldCost)
            return (false, $"Need {recipe.GoldCost} gold (have {player.Gold}).");

        // Consume ingredients
        foreach (var ingredient in recipe.Ingredients)
        {
            int removed = 0;
            for (int i = player.Inventory.Count - 1; i >= 0 && removed < ingredient.Count; i--)
            {
                if (player.Inventory[i].Name.Equals(ingredient.DisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    player.Inventory.RemoveAt(i);
                    removed++;
                }
            }
        }
        // Spend gold
        if (recipe.GoldCost > 0)
            player.SpendGold(recipe.GoldCost);

        // Add result
        player.Inventory.Add(recipe.Result.ToItem());
        return (true, $"You crafted {recipe.Result.Name}!");
    }

    private static List<CraftingRecipe> BuildDefaultRecipes() => new()
    {
        new CraftingRecipe
        {
            Id = "health-elixir",
            Name = "Health Elixir",
            Ingredients = new() { new RecipeIngredient { ItemId = "health-potion", DisplayName = "Health Potion", Count = 2 } },
            GoldCost = 0,
            Result = new RecipeResult { ItemId = "health-elixir", Name = "Health Elixir", Type = "Consumable", Tier = "Uncommon", HealAmount = 75, Description = "Two potions rendered down into something stronger. The colour is wrong, but the effect is not." }
        },
        new CraftingRecipe
        {
            Id = "reinforced-sword",
            Name = "Reinforced Sword",
            Ingredients = new() { new RecipeIngredient { ItemId = "iron-sword", DisplayName = "Iron Sword", Count = 1 } },
            GoldCost = 30,
            Result = new RecipeResult { ItemId = "reinforced-sword", Name = "Reinforced Sword", Type = "Weapon", Tier = "Rare", AttackBonus = 8, IsEquippable = true, Description = "The iron has been retempered and the edge reground. It bites deeper now." }
        },
        new CraftingRecipe
        {
            Id = "reinforced-armor",
            Name = "Reinforced Armor",
            Ingredients = new() { new RecipeIngredient { ItemId = "leather-armor", DisplayName = "Leather Armor", Count = 1 } },
            GoldCost = 25,
            Result = new RecipeResult { ItemId = "reinforced-armor", Name = "Reinforced Armor", Type = "Armor", Tier = "Uncommon", DefenseBonus = 8, IsEquippable = true, Description = "Extra plates riveted over the weak points. Heavier, but considerably harder to kill through." }
        },
    };
}
