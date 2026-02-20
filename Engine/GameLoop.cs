namespace TextGame.Engine;

using System;
using TextGame.Display;
using TextGame.Models;

public class GameLoop
{
    private readonly DisplayService _display;
    private readonly ICombatEngine _combat;
    private Player _player = null!;
    private Room _currentRoom = null!;

    public GameLoop(DisplayService display, ICombatEngine combat)
    {
        _display = display;
        _combat = combat;
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
            Console.Write("> ");
            var input = Console.ReadLine() ?? string.Empty;
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
        _currentRoom = nextRoom;
        _display.ShowRoom(_currentRoom);
        _currentRoom.Visited = true;

        // Check for enemy encounter
        if (_currentRoom.Enemy != null && _currentRoom.Enemy.HP > 0)
        {
            var result = _combat.RunCombat(_player, _currentRoom.Enemy);
            
            if (result == CombatResult.PlayerDied)
            {
                _display.ShowMessage("You have been defeated. Game over.");
                return;
            }
        }

        // Check win condition
        if (_currentRoom.IsExit && (_currentRoom.Enemy == null || _currentRoom.Enemy.HP <= 0))
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
                    var healedAmount = Math.Min(item.HealAmount, _player.MaxHP - _player.HP);
                    _player.HP += healedAmount;
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
                _player.Attack += item.AttackBonus;
                _player.Defense += item.DefenseBonus;
                _player.Inventory.Remove(item);
                _display.ShowMessage($"You equip {item.Name}. Attack: {_player.Attack}, Defense: {_player.Defense}");
                break;

            default:
                _display.ShowError($"You can't use {item.Name}.");
                break;
        }
    }
}
