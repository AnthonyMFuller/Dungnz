namespace Dungnz.Engine.Commands;

using Dungnz.Models;
using Dungnz.Systems;

internal sealed class TakeCommandHandler : ICommandHandler
{
    private static readonly string[] _lootLines =
    {
        "Every bit helps down here.",
        "You tuck it away carefully.",
        "Useful. Or sellable. Either way, it's yours now.",
        "Into the pack it goes."
    };

    public void Handle(string argument, CommandContext context)
    {
        var roomItems = context.CurrentRoom.Items;

        if (string.IsNullOrWhiteSpace(argument))
        {
            if (roomItems.Count == 0)
            {
                context.TurnConsumed = false;
                context.Display.ShowError("There is nothing here to take.");
                return;
            }
            var selection = context.Display.ShowTakeMenuAndSelect(roomItems);
            if (selection == null) { context.TurnConsumed = false; return; }
            switch (selection)
            {
                case Models.TakeSelection.All:
                    TakeAllItems(context);
                    return;
                case Models.TakeSelection.Single s:
                    TakeSingleItem(s.Item, context);
                    return;
            }
        }

        // Typed argument — exact then fuzzy match
        var itemNameLower = argument.ToLowerInvariant();
        var item = roomItems.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            int tolerance = Math.Max(2, itemNameLower.Length / 2);
            var candidates = roomItems
                .Select(i => (Item: i, Distance: EquipmentManager.LevenshteinDistance(itemNameLower, i.Name.ToLowerInvariant())))
                .Where(x => x.Distance <= tolerance)
                .OrderBy(x => x.Distance)
                .ToList();
            if (candidates.Count > 0)
                item = candidates[0].Item;
        }

        if (item == null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError($"There is no '{argument}' here.");
            return;
        }

        TakeSingleItem(item, context);
    }

    private void TakeSingleItem(Item item, CommandContext context)
    {
        context.CurrentRoom.RemoveItem(item);
        if (!context.InventoryManager.TryAddItem(context.Player, item))
        {
            context.CurrentRoom.AddItem(item);
            context.TurnConsumed = false;
            context.Display.ShowError("❌ Inventory full!");
            return;
        }
        int slotsCurrent = context.Player.Inventory.Count;
        int weightCurrent = context.Player.Inventory.Sum(i => i.Weight);
        context.Display.ShowItemPickup(item, slotsCurrent, Player.MaxInventorySize, weightCurrent, InventoryManager.MaxWeight);
        context.Display.ShowMessage(context.Narration.Pick(_lootLines));
        context.Display.ShowMessage(ItemInteractionNarration.PickUp(item));
        context.Events?.RaiseItemPicked(context.Player, item, context.CurrentRoom);
        context.Stats.ItemsFound++;
        if (item.Type == ItemType.Gold) { context.Stats.GoldCollected += item.StatModifier; context.SessionStats.GoldEarned += item.StatModifier; }
    }

    private void TakeAllItems(CommandContext context)
    {
        var items = context.CurrentRoom.Items.ToList();
        if (items.Count == 0)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("There is nothing here to take.");
            return;
        }
        int taken = 0;
        foreach (var item in items)
        {
            context.CurrentRoom.RemoveItem(item);
            if (!context.InventoryManager.TryAddItem(context.Player, item))
            {
                context.CurrentRoom.AddItem(item);
                context.Display.ShowError($"❌ Inventory full! {item.Name} left behind.");
                break;
            }
            int slotsCurrent = context.Player.Inventory.Count;
            int weightCurrent = context.Player.Inventory.Sum(i => i.Weight);
            context.Display.ShowItemPickup(item, slotsCurrent, Player.MaxInventorySize, weightCurrent, InventoryManager.MaxWeight);
            context.Display.ShowMessage(ItemInteractionNarration.PickUp(item));
            context.Events?.RaiseItemPicked(context.Player, item, context.CurrentRoom);
            context.Stats.ItemsFound++;
            if (item.Type == ItemType.Gold) { context.Stats.GoldCollected += item.StatModifier; context.SessionStats.GoldEarned += item.StatModifier; }
            taken++;
        }
        if (taken > 0)
            context.Display.ShowMessage(context.Narration.Pick(_lootLines));
    }
}
