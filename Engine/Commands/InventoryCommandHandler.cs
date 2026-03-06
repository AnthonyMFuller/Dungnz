namespace Dungnz.Engine.Commands;

internal sealed class InventoryCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (context.Player.Inventory.Count == 0)
        {
            context.Display.ShowMessage("Your inventory is empty.");
            context.TurnConsumed = false;
            return;
        }
        
        var selectedItem = context.Display.ShowInventoryAndSelect(context.Player);
        context.TurnConsumed = false;
        if (selectedItem != null)
        {
            context.Display.ShowItemDetail(selectedItem);
            if (selectedItem.IsEquippable)
            {
                var equipped = context.GetCurrentlyEquippedForItem(context.Player, selectedItem);
                context.Display.ShowEquipmentComparison(context.Player, equipped, selectedItem);
            }
        }
        else
        {
            context.Display.ShowRoom(context.CurrentRoom);
        }
    }
}
