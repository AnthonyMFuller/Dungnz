namespace Dungnz.Engine.Commands;

internal sealed class EquipCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Equipment.HandleEquip(context.Player, argument);
    }
}

internal sealed class UnequipCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Equipment.HandleUnequip(context.Player, argument);
    }
}

internal sealed class EquipmentCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Equipment.ShowEquipment(context.Player);
    }
}
