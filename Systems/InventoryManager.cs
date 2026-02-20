namespace TextGame.Systems;
using TextGame.Models;
using TextGame.Display;

public class InventoryManager
{
    private readonly DisplayService _display;
    
    public InventoryManager(DisplayService display)
    {
        _display = display;
    }
    
    public bool TakeItem(Player player, Room room, string itemName)
    {
        var item = room.Items.FirstOrDefault(i => i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            _display.ShowError($"No '{itemName}' here.");
            return false;
        }
        
        room.Items.Remove(item);
        player.Inventory.Add(item);
        _display.ShowMessage($"You picked up {item.Name}.");
        return true;
    }
    
    public UseResult UseItem(Player player, string itemName)
    {
        var item = player.Inventory.FirstOrDefault(i => i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null) return UseResult.NotFound;
        
        switch (item.Type)
        {
            case ItemType.Consumable:
                player.HP = Math.Min(player.MaxHP, player.HP + item.HealAmount);
                _display.ShowMessage($"You used {item.Name}. HP restored to {player.HP}/{player.MaxHP}.");
                player.Inventory.Remove(item);
                return UseResult.Used;
            
            case ItemType.Weapon:
                player.Attack += item.AttackBonus;
                _display.ShowMessage($"You equipped {item.Name}. Attack +{item.AttackBonus}.");
                player.Inventory.Remove(item);
                return UseResult.Used;
            
            case ItemType.Armor:
                player.Defense += item.DefenseBonus;
                _display.ShowMessage($"You equipped {item.Name}. Defense +{item.DefenseBonus}.");
                player.Inventory.Remove(item);
                return UseResult.Used;
            
            default:
                return UseResult.NotUsable;
        }
    }
}
