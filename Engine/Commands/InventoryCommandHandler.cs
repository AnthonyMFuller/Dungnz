namespace Dungnz.Engine.Commands;

internal sealed class InventoryCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
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
    }
}
