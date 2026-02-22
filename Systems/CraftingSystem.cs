namespace Dungnz.Systems;
using Dungnz.Models;

/// <summary>Defines a single crafting recipe, specifying the required ingredients, gold cost, and the item produced.</summary>
public class CraftingRecipe
{
    /// <summary>Gets the display name of this recipe.</summary>
    public string Name { get; init; } = "";

    /// <summary>Gets the list of ingredients required, each as a tuple of item name and required count.</summary>
    public List<(string ItemName, int Count)> Ingredients { get; init; } = new();

    /// <summary>Gets the item that is produced when this recipe is crafted successfully.</summary>
    public Item Result { get; init; } = null!;

    /// <summary>Gets the gold cost that must be paid in addition to providing the required ingredients.</summary>
    public int GoldCost { get; init; }
}

/// <summary>
/// Provides the static list of available crafting recipes and the logic for attempting
/// to craft an item from a player's inventory and gold.
/// </summary>
public class CraftingSystem
{
    /// <summary>Gets all crafting recipes available in the game.</summary>
    public static readonly List<CraftingRecipe> Recipes = new()
    {
        new CraftingRecipe {
            Name = "Health Elixir",
            Ingredients = new() { ("Health Potion", 2) },
            GoldCost = 0,
            Result = new Item { Name = "Health Elixir", Type = ItemType.Consumable, HealAmount = 75, Description = "Two potions rendered down into something stronger. The colour is wrong, but the effect is not." }
        },
        new CraftingRecipe {
            Name = "Reinforced Sword",
            Ingredients = new() { ("Iron Sword", 1) },
            GoldCost = 30,
            Result = new Item { Name = "Reinforced Sword", Type = ItemType.Weapon, AttackBonus = 8, IsEquippable = true, Description = "The iron has been retempered and the edge reground. It bites deeper now." }
        },
        new CraftingRecipe {
            Name = "Reinforced Armor",
            Ingredients = new() { ("Leather Armor", 1) },
            GoldCost = 25,
            Result = new Item { Name = "Reinforced Armor", Type = ItemType.Armor, DefenseBonus = 8, IsEquippable = true, Description = "Extra plates riveted over the weak points. Heavier, but considerably harder to kill through." }
        },
    };

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
        foreach (var (itemName, count) in recipe.Ingredients)
        {
            var held = player.Inventory.Count(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (held < count)
                return (false, $"Need {count}x {itemName} (have {held}).");
        }
        // Check gold
        if (player.Gold < recipe.GoldCost)
            return (false, $"Need {recipe.GoldCost} gold (have {player.Gold}).");

        // Consume ingredients
        foreach (var (itemName, count) in recipe.Ingredients)
        {
            int removed = 0;
            for (int i = player.Inventory.Count - 1; i >= 0 && removed < count; i--)
            {
                if (player.Inventory[i].Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
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
        // Add result — clone so the shared recipe definition is never mutated
        player.Inventory.Add(recipe.Result.Clone());
        return (true, $"You crafted {recipe.Result.Name}!");
    }
}
