namespace Dungnz.Engine.Commands;

internal sealed class EquipCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        var equipped = context.Equipment.HandleEquip(context.Player, argument);
        if (!equipped)
        {
            context.TurnConsumed = false;
        }
        context.Display.ShowRoom(context.CurrentRoom);
    }
}

internal sealed class UnequipCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Equipment.HandleUnequip(context.Player, argument);
        context.Display.ShowRoom(context.CurrentRoom);
    }
}

internal sealed class EquipmentCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Equipment.ShowEquipment(context.Player);
        context.Display.ShowRoom(context.CurrentRoom);
    }
}
