namespace Dungnz.Engine;

using System;
using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// The primary game loop that drives exploration, combat, inventory management, and
/// floor progression. It reads player commands each turn, delegates to specialised
/// handlers for each action, invokes the combat engine on enemy encounters, tracks
/// run statistics, evaluates achievements on game-end, and terminates the run on
/// victory, defeat, or player-quit.
/// </summary>
public class GameLoop
{
    private readonly IDisplayService _display;
    private readonly ICombatEngine _combat;
    private readonly IInputReader _input;
    private readonly GameEvents? _events;
    private readonly int? _seed;
    private readonly DifficultySettings _difficulty;
    private Player _player = null!;
    private Room _currentRoom = null!;
    private RunStats _stats = null!;
    private DateTime _runStart;
    private readonly AchievementSystem _achievements = new();
    private readonly EquipmentManager _equipment;
    private int _currentFloor = 1;

    /// <summary>
    /// Creates a new <see cref="GameLoop"/> wired to the specified display, combat,
    /// and input services.
    /// </summary>
    /// <param name="display">The display service used to render all game output.</param>
    /// <param name="combat">The combat engine invoked whenever the player enters a room with a live enemy.</param>
    /// <param name="input">
    /// The input reader used to receive exploration commands.
    /// Defaults to <see cref="ConsoleInputReader"/> when <see langword="null"/>.
    /// </param>
    /// <param name="events">
    /// Optional event bus for broadcasting room-entered, item-picked, and other game events.
    /// </param>
    /// <param name="seed">
    /// Optional RNG seed forwarded to the <see cref="DungeonGenerator"/> for reproducible
    /// dungeon layouts; displayed to the player on run end so they can replay the same layout.
    /// </param>
    /// <param name="difficulty">
    /// The difficulty settings to apply for this run. Defaults to <see cref="Difficulty.Normal"/>
    /// when <see langword="null"/>.
    /// </param>
    public GameLoop(IDisplayService display, ICombatEngine combat, IInputReader? input = null, GameEvents? events = null, int? seed = null, DifficultySettings? difficulty = null)
    {
        _display = display;
        _combat = combat;
        _input = input ?? new ConsoleInputReader();
        _events = events;
        _seed = seed;
        _difficulty = difficulty ?? DifficultySettings.For(Difficulty.Normal);
        _equipment = new EquipmentManager(display);
    }

    /// <summary>
    /// Starts the main command loop for the given player and dungeon, reading and
    /// dispatching commands until the player wins, dies, or quits. Also handles
    /// multi-floor descents, shrine interactions, and end-of-run stat/achievement display.
    /// </summary>
    /// <param name="player">The player character whose state is mutated throughout the run.</param>
    /// <param name="startRoom">The first room the player occupies when the loop begins.</param>
    public void Run(Player player, Room startRoom)
    {
        _player = player;
        _currentRoom = startRoom;
        _stats = new RunStats();
        _runStart = DateTime.UtcNow;
        _currentFloor = 1;
        _display.ShowTitle();
        _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
        _display.ShowMessage($"Floor {_currentFloor}");
        _display.ShowRoom(_currentRoom);
        _currentRoom.Visited = true;

        while (true)
        {
            _display.ShowCommandPrompt();
            var input = _input.ReadLine() ?? string.Empty;
            var cmd = CommandParser.Parse(input);
            _stats.TurnsTaken++;

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
                    _equipment.HandleEquip(_player, cmd.Argument);
                    break;
                case CommandType.Unequip:
                    _equipment.HandleUnequip(_player, cmd.Argument);
                    break;
                case CommandType.Equipment:
                    _equipment.ShowEquipment(_player);
                    break;
                case CommandType.Help:
                    _display.ShowHelp();
                    break;
                case CommandType.Save:
                    HandleSave(cmd.Argument);
                    break;
                case CommandType.Load:
                    HandleLoad(cmd.Argument);
                    break;
                case CommandType.ListSaves:
                    HandleListSaves();
                    break;
                case CommandType.Quit:
                    _display.ShowMessage("Thanks for playing!");
                    return;
                case CommandType.Descend:
                    HandleDescend();
                    break;
                case CommandType.Map:
                    _display.ShowMap(_currentRoom);
                    break;
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
                _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                _stats.Display(_display.ShowMessage);
                RunStats.AppendToHistory(_stats, won: false);
                return;
            }
            
            if (result == CombatResult.Won)
            {
                _currentRoom.Enemy = null;
                _stats.EnemiesDefeated++;
            }
        }

        // Check win/floor condition
        if (_currentRoom.IsExit && _currentRoom.Enemy == null)
        {
            const int finalFloor = 5;
            if (_currentFloor >= finalFloor)
            {
                _display.ShowMessage("You escaped the dungeon! You win!");
                _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                _stats.Display(_display.ShowMessage);
                RunStats.AppendToHistory(_stats, won: true);
                var unlocked = _achievements.Evaluate(_stats, _player, won: true);
                if (unlocked.Count > 0)
                {
                    _display.ShowMessage("=== ACHIEVEMENTS UNLOCKED ===");
                    foreach (var a in unlocked)
                        _display.ShowMessage($"🏆 {a.Name} — {a.Description}");
                }
                return;
            }
            else
            {
                var clearedVariant = DungeonVariant.ForFloor(_currentFloor);
                if (!string.IsNullOrEmpty(clearedVariant.ExitMessage))
                    _display.ShowMessage(clearedVariant.ExitMessage);
                _display.ShowMessage($"You cleared Floor {_currentFloor}! Type DESCEND to go deeper.");
            }
        }

        // Prompt for shrine if present and not yet used
        if (_currentRoom.HasShrine && !_currentRoom.ShrineUsed)
        {
            _display.ShowMessage("✨ There is a Shrine in this room. Type USE SHRINE to interact.");
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
        _stats.ItemsFound++;
        if (item.Type == ItemType.Gold) _stats.GoldCollected += item.StatModifier;
    }

    private void HandleUse(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _display.ShowError("Use what?");
            return;
        }

        // Special: USE SHRINE
        if (itemName.Equals("shrine", StringComparison.OrdinalIgnoreCase))
        {
            HandleShrine();
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

    private void HandleSave(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            _display.ShowError("Save as what? Usage: SAVE <name>");
            return;
        }
        SaveSystem.SaveGame(new GameState(_player, _currentRoom), saveName);
        _display.ShowMessage($"Game saved as '{saveName}'.");
    }

    private void HandleLoad(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            _display.ShowError("Load which save? Usage: LOAD <name>");
            return;
        }
        var state = SaveSystem.LoadGame(saveName);
        _player = state.Player;
        _currentRoom = state.CurrentRoom;
        _display.ShowMessage($"Loaded save '{saveName}'.");
        _display.ShowRoom(_currentRoom);
    }

    private void HandleListSaves()
    {
        var saves = SaveSystem.ListSaves();
        if (saves.Length == 0)
        {
            _display.ShowMessage("No saved games found.");
            return;
        }
        _display.ShowMessage("=== Saved Games ===");
        foreach (var s in saves)
            _display.ShowMessage($"  {s}");
    }

    private void HandleDescend()
    {
        if (!_currentRoom.IsExit || _currentRoom.Enemy != null)
        {
            _display.ShowError("You can only descend at a cleared exit room.");
            return;
        }

        _currentFloor++;
        _display.ShowMessage($"You descend deeper into the dungeon... Floor {_currentFloor}");

        float floorMult = 1.0f + (_currentFloor - 1) * 0.5f;
        var floorSeed = _seed.HasValue ? _seed.Value + _currentFloor : (int?)null;
        var gen = new DungeonGenerator(floorSeed);
        var (newStart, _) = gen.Generate(floorMultiplier: floorMult, difficulty: _difficulty);
        _currentRoom = newStart;
        _currentRoom.Visited = true;
        _display.ShowMessage($"Floor {_currentFloor}");
        var descendVariant = DungeonVariant.ForFloor(_currentFloor);
        _display.ShowMessage($"=== {descendVariant.Name} ===");
        _display.ShowMessage(descendVariant.EntryMessage);
        _display.ShowRoom(_currentRoom);
    }

    /// <summary>Returns a display-friendly label for the current difficulty setting.</summary>
    private string GetDifficultyName()
    {
        if (_difficulty.EnemyStatMultiplier < 1.0f) return "Casual";
        if (_difficulty.EnemyStatMultiplier > 1.0f) return "Hard";
        return "Normal";
    }

    private void HandleShrine()
    {
        if (!_currentRoom.HasShrine)
        {
            _display.ShowError("There is no shrine here.");
            return;
        }
        if (_currentRoom.ShrineUsed)
        {
            _display.ShowMessage("The shrine has already been used.");
            return;
        }

        _display.ShowMessage("=== Shrine ===");
        _display.ShowMessage($"[H]eal fully       - 30g  (Your gold: {_player.Gold})");
        _display.ShowMessage("[B]less            - 50g  (+2 ATK/DEF for 5 rooms)");
        _display.ShowMessage("[F]ortify          - 75g  (MaxHP +10, permanent)");
        _display.ShowMessage("[M]editate         - 75g  (MaxMana +10, permanent)");
        _display.ShowMessage("[L]eave");
        _display.ShowCommandPrompt();

        var choice = _input.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        switch (choice)
        {
            case "H":
                if (_player.Gold < 30) { _display.ShowError("Not enough gold (need 30g)."); return; }
                _player.SpendGold(30);
                _player.Heal(_player.MaxHP);
                _display.ShowMessage($"The shrine heals you fully! HP: {_player.HP}/{_player.MaxHP}");
                _currentRoom.ShrineUsed = true;
                break;
            case "B":
                if (_player.Gold < 50) { _display.ShowError("Not enough gold (need 50g)."); return; }
                _player.SpendGold(50);
                _player.ModifyAttack(2);
                _player.ModifyDefense(2);
                _display.ShowMessage("The shrine blesses you! +2 ATK/DEF.");
                _currentRoom.ShrineUsed = true;
                break;
            case "F":
                if (_player.Gold < 75) { _display.ShowError("Not enough gold (need 75g)."); return; }
                _player.SpendGold(75);
                _player.FortifyMaxHP(10);
                _display.ShowMessage($"The shrine fortifies you! MaxHP permanently +10. ({_player.MaxHP} MaxHP)");
                _currentRoom.ShrineUsed = true;
                break;
            case "M":
                if (_player.Gold < 75) { _display.ShowError("Not enough gold (need 75g)."); return; }
                _player.SpendGold(75);
                _player.FortifyMaxMana(10);
                _display.ShowMessage($"The shrine expands your mind! MaxMana permanently +10. ({_player.MaxMana} MaxMana)");
                _currentRoom.ShrineUsed = true;
                break;
            case "L":
                _display.ShowMessage("You leave the shrine.");
                break;
            default:
                _display.ShowError("Invalid choice.");
                break;
        }
    }
}
