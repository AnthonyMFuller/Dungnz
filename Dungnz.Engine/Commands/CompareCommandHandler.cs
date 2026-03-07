namespace Dungnz.Engine.Commands;

internal sealed class CompareCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            // Interactive selection: show equippable items only
            var equippable = context.Player.Inventory.Where(i => i.IsEquippable).ToList();
            if (equippable.Count == 0)
            {
                context.TurnConsumed = false;
                context.Display.ShowError("You have no equippable items to compare.");
                return;
            }

            var selected = context.Display.ShowEquipMenuAndSelect(equippable.AsReadOnly());
            if (selected == null)
            {
                context.TurnConsumed = false;
                context.Display.ShowRoom(context.CurrentRoom);
                return; // User cancelled
            }

            argument = selected.Name;
        }

        // Find item by name (case-insensitive contains match)
        var itemNameLower = argument.ToLowerInvariant();
        var item = context.Player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError($"You don't have '{argument}' in your inventory.");
            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        if (!item.IsEquippable)
        {
            context.TurnConsumed = false;
            context.Display.ShowError($"{item.Name} cannot be equipped, so there's nothing to compare.");
            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        // Get currently equipped item in target slot
        var currentlyEquipped = context.GetCurrentlyEquippedForItem(context.Player, item);

        // Show comparison
        context.Display.ShowEquipmentComparison(context.Player, currentlyEquipped, item);
        context.Display.ShowRoom(context.CurrentRoom);
    }
}
