namespace Dungnz.Engine.Commands;

using Dungnz.Systems;

internal sealed class ShopCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (context.CurrentRoom.Merchant == null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("There is no merchant here.");
            return;
        }

        var merchant = context.CurrentRoom.Merchant;

        // Keep the shop open in a loop so the player can sell then buy (or vice versa)
        while (true)
        {
            if (merchant.Stock.Count == 0)
            {
                context.Display.ShowMessage("The merchant has nothing for sale.");
                context.Display.ShowMessage(context.Narration.Pick(MerchantNarration.NoBuy));
                return;
            }
            
            context.Display.ShowMessage($"=== MERCHANT SHOP ({merchant.Name}) ===");
            context.Display.ShowMessage(MerchantNarration.GetFloorGreeting(context.CurrentFloor));
            var shopChoice = context.Display.ShowShopWithSellAndSelect(
                merchant.Stock.Select(mi => (mi.Item, mi.Price)), context.Player.Gold);

            if (shopChoice == 0)  // Leave
            {
                context.Display.ShowMessage("You leave the shop.");
                context.Display.ShowMessage(context.Narration.Pick(MerchantNarration.NoBuy));
                return;
            }

            if (shopChoice == -1)  // Sell
            {
                new SellCommandHandler().Handle(string.Empty, context);
                continue;
            }

            // shopChoice is 1-based item index
            if (shopChoice >= 1 && shopChoice <= merchant.Stock.Count)
            {
                var selected = merchant.Stock[shopChoice - 1];
                if (context.Player.Gold < selected.Price)
                {
                    context.Display.ShowMessage(MerchantNarration.GetCantAfford());
                }
                else
                {
                    context.Player.SpendGold(selected.Price);
                    if (!context.InventoryManager.TryAddItem(context.Player, selected.Item))
                    {
                        context.Player.AddGold(selected.Price); // refund — inventory was full or too heavy
                        context.Display.ShowMessage(MerchantNarration.GetInventoryFull());
                    }
                    else
                    {
                        merchant.Stock.RemoveAt(shopChoice - 1);
                        context.Display.ShowMessage($"You bought {selected.Item.Name} for {selected.Price}g. Gold remaining: {context.Player.Gold}g");
                        context.Display.ShowPlayerStats(context.Player);
                        context.Display.ShowMessage(context.Narration.Pick(MerchantNarration.AfterPurchase));
                    }
                }
                // Re-display shop after buying so player can continue shopping
                continue;
            }
        }
    }
}
