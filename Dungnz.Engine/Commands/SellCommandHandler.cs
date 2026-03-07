namespace Dungnz.Engine.Commands;

using Dungnz.Models;
using Dungnz.Systems;

internal sealed class SellCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (context.CurrentRoom.Merchant == null)
        {
            context.Display.ShowError("There is no merchant here.");
            return;
        }

        while (true)
        {
            // Items in player.Inventory are already unequipped; exclude Gold-type items
            var sellable = context.Player.Inventory
                .Where(i => i.Type != ItemType.Gold)
                .ToList();

            if (!sellable.Any())
            {
                context.Display.ShowMessage(MerchantNarration.GetNoSell());
                break;
            }

            var idx = context.Display.ShowSellMenuAndSelect(sellable.Select(i => (i, MerchantInventoryConfig.ComputeSellPrice(i))), context.Player.Gold);
            if (idx == 0)
                break;

            var item = sellable[idx - 1];
            int price = MerchantInventoryConfig.ComputeSellPrice(item);

            if (!context.Display.ShowConfirmMenu($"Sell {item.Name} for {price}g?"))
            {
                context.Display.ShowMessage("Changed your mind.");
                continue;
            }

            context.Player.Inventory.Remove(item);
            context.Player.AddGold(price);
            context.Display.ShowMessage($"You sold {item.Name} for {price}g. Gold remaining: {context.Player.Gold}g");
            context.Display.ShowPlayerStats(context.Player);
            if (item.Tier == ItemTier.Legendary)
                context.Display.ShowMessage(MerchantNarration.GetLegendarySold());
            else
                context.Display.ShowMessage(MerchantNarration.GetAfterSale());
        }

        context.Display.ShowRoom(context.CurrentRoom);
    }
}
