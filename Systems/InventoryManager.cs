namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;

/// <summary>
/// Handles player inventory operations: taking items from rooms, using items, and
/// enforcing slot/weight capacity limits.
/// </summary>
public class InventoryManager
{
    /// <summary>Maximum total carry weight a player's inventory may hold.</summary>
    public const int MaxWeight = 50;

    private readonly IDisplayService _display;

    /// <summary>Initialises a new <see cref="InventoryManager"/> with the given display service.</summary>
    /// <param name="display">The display service used to show messages and errors.</param>
    public InventoryManager(IDisplayService display)
    {
        _display = display;
    }

    /// <summary>
    /// Attempts to add <paramref name="item"/> to the player's inventory,
    /// respecting both the slot limit (<see cref="Player.MaxInventorySize"/>) and the weight limit (<see cref="MaxWeight"/>).
    /// </summary>
    /// <param name="player">The player receiving the item.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added successfully;
    /// <see langword="false"/> if the inventory is full (slots or weight exceeded).
    /// </returns>
    public bool TryAddItem(Player player, Item item)
    {
        if (IsFull(player))
            return false;

        var currentWeight = player.Inventory.Sum(i => i.Weight);
        if (currentWeight + item.Weight > MaxWeight)
            return false;

        player.Inventory.Add(item);
        return true;
    }

    /// <summary>
    /// Attempts to remove the first item whose name matches <paramref name="itemName"/>
    /// (case-insensitive) from the player's inventory.
    /// </summary>
    /// <param name="player">The player whose inventory to search.</param>
    /// <param name="itemName">The name (or partial name) of the item to remove.</param>
    /// <returns>
    /// <see langword="true"/> if a matching item was found and removed;
    /// <see langword="false"/> if no match was found.
    /// </returns>
    public bool TryRemoveItem(Player player, string itemName)
    {
        var item = player.Inventory.FirstOrDefault(i =>
            i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null)
            return false;

        player.Inventory.Remove(item);
        return true;
    }

    /// <summary>
    /// Determines whether the player's inventory contains an item whose name matches
    /// <paramref name="itemName"/> (case-insensitive).
    /// </summary>
    /// <param name="player">The player whose inventory to search.</param>
    /// <param name="itemName">The name (or partial name) to look for.</param>
    /// <returns><see langword="true"/> if a matching item exists; otherwise <see langword="false"/>.</returns>
    public bool HasItem(Player player, string itemName) =>
        player.Inventory.Any(i => i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Determines whether the player's inventory has reached the maximum slot count
    /// (<see cref="Player.MaxInventorySize"/>).
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns><see langword="true"/> if the inventory is at or above the slot limit; otherwise <see langword="false"/>.</returns>
    public bool IsFull(Player player) => player.Inventory.Count >= Player.MaxInventorySize;

    /// <summary>
    /// Moves an item matching <paramref name="itemName"/> from the <paramref name="room"/>
    /// into the player's inventory, enforcing slot and weight limits.
    /// </summary>
    /// <param name="player">The player picking up the item.</param>
    /// <param name="room">The room the player is currently in.</param>
    /// <param name="itemName">The name (or partial name) of the item to take.</param>
    /// <returns>
    /// <see langword="true"/> if the item was found and moved;
    /// <see langword="false"/> if the item was not found or inventory is full.
    /// </returns>
    public bool TakeItem(Player player, Room room, string itemName)
    {
        var item = room.Items.FirstOrDefault(i => i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            _display.ShowError($"No '{itemName}' here.");
            return false;
        }

        if (IsFull(player))
        {
            _display.ShowError("Your inventory is full.");
            return false;
        }

        var currentWeight = player.Inventory.Sum(i => i.Weight);
        if (currentWeight + item.Weight > MaxWeight)
        {
            _display.ShowError("That item is too heavy to carry.");
            return false;
        }

        room.Items.Remove(item);
        player.Inventory.Add(item);
        _display.ShowMessage($"You picked up {item.Name}.");
        return true;
    }

    /// <summary>
    /// Uses an item from the player's inventory, applying its effect (healing, equipping, etc.).
    /// The item is removed from the inventory on successful use.
    /// </summary>
    /// <param name="player">The player using the item.</param>
    /// <param name="itemName">The name (or partial name) of the item to use.</param>
    /// <returns>
    /// <see cref="UseResult.Used"/> on success,
    /// <see cref="UseResult.NotFound"/> if the item is not in the inventory, or
    /// <see cref="UseResult.NotUsable"/> if the item type cannot be used directly.
    /// </returns>
    public UseResult UseItem(Player player, string itemName)
    {
        var item = player.Inventory.FirstOrDefault(i => i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null) return UseResult.NotFound;

        switch (item.Type)
        {
            case ItemType.Consumable:
                player.Heal(item.HealAmount);
                _display.ShowMessage($"You used {item.Name}. HP restored to {player.HP}/{player.MaxHP}.");
                player.Inventory.Remove(item);
                return UseResult.Used;

            case ItemType.Weapon:
                player.ModifyAttack(item.AttackBonus);
                _display.ShowMessage($"You equipped {item.Name}. Attack +{item.AttackBonus}.");
                player.Inventory.Remove(item);
                return UseResult.Used;

            case ItemType.Armor:
                player.ModifyDefense(item.DefenseBonus);
                _display.ShowMessage($"You equipped {item.Name}. Defense +{item.DefenseBonus}.");
                player.Inventory.Remove(item);
                return UseResult.Used;

            case ItemType.Accessory:
                player.EquipItem(item);
                _display.ShowMessage($"You equipped {item.Name}.");
                return UseResult.Used;

            default:
                return UseResult.NotUsable;
        }
    }
}
