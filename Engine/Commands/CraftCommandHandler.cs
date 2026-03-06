namespace Dungnz.Engine.Commands;

using Dungnz.Systems;

internal sealed class CraftCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            context.TurnConsumed = false;

            // Build (recipeName, canCraft) pairs for the selection menu
            var menuEntries = CraftingSystem.Recipes.Select(r =>
            {
                bool canCraft = r.Ingredients.All(ing =>
                    context.Player.Inventory.Count(i => i.Name.Equals(ing.DisplayName, StringComparison.OrdinalIgnoreCase)) >= ing.Count);
                return (r.Name, canCraft);
            }).ToList();

            int selectedIndex = context.Display.ShowCraftMenuAndSelect(menuEntries);
            if (selectedIndex == 0)
            {
                context.Display.ShowRoom(context.CurrentRoom);
                return; // cancelled
            }

            // Show the full recipe card for the selected recipe before crafting
            var chosen = CraftingSystem.Recipes[selectedIndex - 1];
            var ingredientsWithAvailability = chosen.Ingredients
                .Select(ing => (
                    $"{ing.Count}x {ing.DisplayName}",
                    context.Player.Inventory.Count(i => i.Name.Equals(ing.DisplayName, StringComparison.OrdinalIgnoreCase)) >= ing.Count
                ))
                .ToList();
            context.Display.ShowCraftRecipe(chosen.Name, chosen.Result.ToItem(), ingredientsWithAvailability);

            var (success, msg) = CraftingSystem.TryCraft(context.Player, chosen);
            if (success)
            {
                context.Display.ShowMessage(msg);
                context.Display.ShowPlayerStats(context.Player);
            }
            else context.Display.ShowError(msg);
            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        var recipe = CraftingSystem.Recipes.FirstOrDefault(r =>
            r.Name.Contains(argument, StringComparison.OrdinalIgnoreCase));
        if (recipe == null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError($"Unknown recipe: {argument}");
            return;
        }

        var (success2, msg2) = CraftingSystem.TryCraft(context.Player, recipe);
        if (success2)
        {
            context.Display.ShowMessage(msg2);
            context.Display.ShowPlayerStats(context.Player);
        }
        else context.Display.ShowError(msg2);
    }
}
