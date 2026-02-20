namespace Dungnz.Engine;

using System;
using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

public class GameLoop
{
    private readonly IDisplayService _display;
    private readonly ICombatEngine _combat;
    private readonly IInputReader _input;
    private readonly GameEvents? _events;
    private Player _player = null!;
    private Room _currentRoom = null!;

    public GameLoop(IDisplayService display, ICombatEngine combat, IInputReader? input = null, GameEvents? events = null)
    {
        _display = display;
        _combat = combat;
        _input = input ?? new ConsoleInputReader();
        _events = events;
    }

    public void Run(Player player, Room startRoom)
    {
        _player = player;
        _currentRoom = startRoom;
        _display.ShowTitle();
        _display.ShowRoom(_currentRoom);
        _currentRoom.Visited = true;

        while (true)
        {
            _display.ShowCommandPrompt();
            var input = _input.ReadLine() ?? string.Empty;
            var cmd = CommandParser.Parse(input);

            switch (cmd.Type)
            {
                case CommandType.Go:
                    HandleGo(cmd.Argument);
                    break;
                case CommandType.Look:
                    HandleLook();
                    break;
                case CommandType.Examine:
                    HandleExamine(cmd.Argument);
                    break;
                case CommandType.Take:
                    HandleTake(cmd.Argument);
                    break;
                case CommandType.Use:
                    HandleUse(cmd.Argument);
                    break;
                case CommandType.Inventory:
                    _display.ShowInventory(_player);
                    break;
                case CommandType.Stats:
                    _display.ShowPlayerStats(_player);
                    break;
                case CommandType.Equip:
                    HandleEquip(cmd.Argument);
                    break;
                case CommandType.Unequip:
                    HandleUnequip(cmd.Argument);
                    break;
                case CommandType.Equipment:
                    HandleShowEquipment();
                    break;
                case CommandType.Help:
                    _display.ShowHelp();
                    break;
                case CommandType.Quit:
                    _display.ShowMessage("Thanks for playing!");
                    return;
                default:
                    _display.ShowError("Unknown command. Type HELP for commands.");
                    break;
            }
        }
    }

    private void HandleGo(string directionStr)
    {
        if (string.IsNullOrWhiteSpace(directionStr))
        {
            _display.ShowError("Go where? Specify a direction (north, south, east, west).");
            return;
        }

        Direction direction;
        switch (directionStr.ToLowerInvariant())
        {
            case "north":
            case "n":
                direction = Direction.North;
                break;
            case "south":
            case "s":
                direction = Direction.South;
                break;
            case "east":
            case "e":
                direction = Direction.East;
                break;
            case "west":
            case "w":
                direction = Direction.West;
                break;
            default:
                _display.ShowError($"Invalid direction: {directionStr}");
                return;
        }

        if (!_currentRoom.Exits.TryGetValue(direction, out var nextRoom))
        {
            _display.ShowError("You can't go that way.");
            return;
        }

        // Check if trying to exit with boss still alive
        if (nextRoom.IsExit && nextRoom.Enemy != null && nextRoom.Enemy.HP > 0)
        {
            _display.ShowError("The boss blocks your path! Defeat it first.");
            return;
        }

        // Move to new room
        var previousRoom = _currentRoom;
        _currentRoom = nextRoom;
        _display.ShowRoom(_currentRoom);
        _currentRoom.Visited = true;
        _events?.RaiseRoomEntered(_player, _currentRoom, previousRoom);

        // Check for enemy encounter
        if (_currentRoom.Enemy != null && _currentRoom.Enemy.HP > 0)
        {
            var result = _combat.RunCombat(_player, _currentRoom.Enemy);
            
            if (result == CombatResult.PlayerDied)
            {
                _display.ShowMessage("You have been defeated. Game over.");
                return;
            }
            
            if (result == CombatResult.Won)
            {
                _currentRoom.Enemy = null;
            }
        }

        // Check win condition
        if (_currentRoom.IsExit && _currentRoom.Enemy == null)
        {
            _display.ShowMessage("You escaped the dungeon! You win!");
            return;
        }
    }

    private void HandleLook()
    {
        _display.ShowRoom(_currentRoom);
    }

    private void HandleExamine(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            _display.ShowError("Examine what?");
            return;
        }

        var targetLower = target.ToLowerInvariant();

        // Check for enemy
        if (_currentRoom.Enemy != null && _currentRoom.Enemy.HP > 0 && 
            _currentRoom.Enemy.Name.ToLowerInvariant().Contains(targetLower))
        {
            _display.ShowMessage($"{_currentRoom.Enemy.Name} - HP: {_currentRoom.Enemy.HP}/{_currentRoom.Enemy.MaxHP}, Attack: {_currentRoom.Enemy.Attack}, Defense: {_currentRoom.Enemy.Defense}");
            return;
        }

        // Check items in room
        var roomItem = _currentRoom.Items.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(targetLower));
        if (roomItem != null)
        {
            _display.ShowMessage($"{roomItem.Name}: {roomItem.Description}");
            return;
        }

        // Check items in inventory
        var invItem = _player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(targetLower));
        if (invItem != null)
        {
            _display.ShowMessage($"{invItem.Name}: {invItem.Description}");
            return;
        }

        _display.ShowError($"You don't see any '{target}' here.");
    }

    private void HandleTake(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _display.ShowError("Take what?");
            return;
        }

        var itemNameLower = itemName.ToLowerInvariant();
        var item = _currentRoom.Items.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            _display.ShowError($"There is no '{itemName}' here.");
            return;
        }

        _currentRoom.Items.Remove(item);
        _player.Inventory.Add(item);
        _display.ShowMessage($"You take the {item.Name}.");
        _events?.RaiseItemPicked(_player, item, _currentRoom);
    }

    private void HandleUse(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _display.ShowError("Use what?");
            return;
        }

        var itemNameLower = itemName.ToLowerInvariant();
        var item = _player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            _display.ShowError($"You don't have '{itemName}'.");
            return;
        }

        switch (item.Type)
        {
            case ItemType.Consumable:
                if (item.HealAmount > 0)
                {
                    var oldHP = _player.HP;
                    _player.Heal(item.HealAmount);
                    var healedAmount = _player.HP - oldHP;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name} and restore {healedAmount} HP. Current HP: {_player.HP}/{_player.MaxHP}");
                }
                else
                {
                    _display.ShowError($"You can't use {item.Name} right now.");
                }
                break;

            case ItemType.Weapon:
            case ItemType.Armor:
            case ItemType.Accessory:
                _display.ShowError($"Use 'EQUIP {item.Name}' to equip this item.");
                break;

            default:
                _display.ShowError($"You can't use {item.Name}.");
                break;
        }
    }

    private void HandleEquip(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _display.ShowError("Equip what? Specify an item name.");
            return;
        }

        var itemNameLower = itemName.ToLowerInvariant();
        var item = _player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            _display.ShowError($"You don't have '{itemName}' in your inventory.");
            return;
        }

        if (!item.IsEquippable)
        {
            _display.ShowError($"{item.Name} cannot be equipped.");
            return;
        }

        try
        {
            _player.EquipItem(item);
            _display.ShowMessage($"You equip {item.Name}. Attack: {_player.Attack}, Defense: {_player.Defense}");
        }
        catch (ArgumentException ex)
        {
            _display.ShowError(ex.Message);
        }
    }

    private void HandleUnequip(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName))
        {
            _display.ShowError("Unequip what? Specify WEAPON, ARMOR, or ACCESSORY.");
            return;
        }

        try
        {
            var item = _player.UnequipItem(slotName);
            _display.ShowMessage($"You unequip {item!.Name} and return it to your inventory.");
        }
        catch (InvalidOperationException ex)
        {
            _display.ShowError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _display.ShowError(ex.Message);
        }
    }

    private void HandleShowEquipment()
    {
        _display.ShowMessage("=== EQUIPMENT ===");
        
        if (_player.EquippedWeapon != null)
        {
            var w = _player.EquippedWeapon;
            _display.ShowMessage($"Weapon: {w.Name} (Attack +{w.AttackBonus})");
        }
        else
        {
            _display.ShowMessage("Weapon: (empty)");
        }

        if (_player.EquippedArmor != null)
        {
            var a = _player.EquippedArmor;
            _display.ShowMessage($"Armor: {a.Name} (Defense +{a.DefenseBonus})");
        }
        else
        {
            _display.ShowMessage("Armor: (empty)");
        }

        if (_player.EquippedAccessory != null)
        {
            var acc = _player.EquippedAccessory;
            var stats = new List<string>();
            if (acc.AttackBonus != 0) stats.Add($"Attack +{acc.AttackBonus}");
            if (acc.DefenseBonus != 0) stats.Add($"Defense +{acc.DefenseBonus}");
            if (acc.StatModifier != 0) stats.Add($"HP +{acc.StatModifier}");
            _display.ShowMessage($"Accessory: {acc.Name} ({string.Join(", ", stats)})");
        }
        else
        {
            _display.ShowMessage("Accessory: (empty)");
        }
    }

}
