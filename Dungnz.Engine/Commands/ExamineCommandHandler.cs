namespace Dungnz.Engine.Commands;

internal sealed class ExamineCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            context.TurnConsumed = false;
            context.Display.ShowError("Examine what?");
            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        var targetLower = argument.ToLowerInvariant();

        // Check for enemy
        if (context.CurrentRoom.Enemy != null && !context.CurrentRoom.Enemy.IsDead &&
            context.CurrentRoom.Enemy.Name.ToLowerInvariant().Contains(targetLower))
        {
            context.Display.ShowMessage($"{context.CurrentRoom.Enemy.Name} - HP: {context.CurrentRoom.Enemy.HP}/{context.CurrentRoom.Enemy.MaxHP}, Attack: {context.CurrentRoom.Enemy.Attack}, Defense: {context.CurrentRoom.Enemy.Defense}");
            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        // Check items in room
        var roomItem = context.CurrentRoom.Items.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(targetLower));
        if (roomItem != null)
        {
            context.Display.ShowItemDetail(roomItem);
            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        // Check items in inventory
        var invItem = context.Player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(targetLower));
        if (invItem != null)
        {
            context.Display.ShowItemDetail(invItem);

            // If equippable, show comparison vs. currently equipped
            if (invItem.IsEquippable)
            {
                var currentlyEquipped = context.GetCurrentlyEquippedForItem(context.Player, invItem);
                context.Display.ShowEquipmentComparison(context.Player, currentlyEquipped, invItem);
            }

            context.Display.ShowRoom(context.CurrentRoom);
            return;
        }

        context.TurnConsumed = false;
        context.Display.ShowError($"You don't see any '{argument}' here.");
        context.Display.ShowRoom(context.CurrentRoom);
    }
}
