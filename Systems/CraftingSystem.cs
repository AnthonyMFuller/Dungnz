namespace Dungnz.Systems;
using Dungnz.Models;

public class CraftingRecipe
{
    public string Name { get; init; } = "";
    public List<(string ItemName, int Count)> Ingredients { get; init; } = new();
    public Item Result { get; init; } = null!;
    public int GoldCost { get; init; }
}

public class CraftingSystem
{
    public static readonly List<CraftingRecipe> Recipes = new()
    {
        new CraftingRecipe {
            Name = "Health Elixir",
            Ingredients = new() { ("Health Potion", 2) },
            GoldCost = 0,
            Result = new Item { Name = "Health Elixir", Type = ItemType.Consumable, HealAmount = 75, Description = "Restores 75 HP." }
        },
        new CraftingRecipe {
            Name = "Reinforced Sword",
            Ingredients = new() { ("Iron Sword", 1) },
            GoldCost = 30,
            Result = new Item { Name = "Reinforced Sword", Type = ItemType.Weapon, AttackBonus = 8, IsEquippable = true, Description = "A stronger blade." }
        },
        new CraftingRecipe {
            Name = "Reinforced Armor",
            Ingredients = new() { ("Leather Armor", 1) },
            GoldCost = 25,
            Result = new Item { Name = "Reinforced Armor", Type = ItemType.Armor, DefenseBonus = 8, IsEquippable = true, Description = "Upgraded protection." }
        },
    };

    public static (bool success, string message) TryCraft(Player player, CraftingRecipe recipe)
    {
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
        player.Inventory.Add(recipe.Result);
        return (true, $"You crafted {recipe.Result.Name}!");
    }
}
