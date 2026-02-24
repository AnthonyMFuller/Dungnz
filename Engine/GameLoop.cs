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
    private readonly InventoryManager _inventoryManager;
    private readonly NarrationService _narration = new();
    private int _currentFloor = 1;
    private bool _turnConsumed;

    /// <summary>Set to <see langword="true"/> when the run ends (win, death) to break the Run() loop.</summary>
    private bool _gameOver = false;

    private const int FinalFloor = 5;

    private static readonly string[] _postCombatLines =
    {
        "The room falls silent. Nothing moves but the dust settling around the fallen {0}.",
        "You stand over {0}'s body, catching your breath. The dungeon feels momentarily less hostile.",
        "The echo of combat fades. {0} is dead. You survived.",
        "Silence returns. {0} won't be troubling anyone else."
    };

    private static readonly string[] _spikeHazardLines =
    {
        "Pressure plates click underfoot. Razor spikes lance from the walls! ({0} damage)",
        "The floor drops a half-inch — then a volley of iron spikes erupts from the stone! ({0} damage)"
    };

    private static readonly string[] _poisonHazardLines =
    {
        "A hissing sound, then green mist floods the chamber. Your lungs burn! ({0} damage)",
        "Pressure triggers a vial of alchemical poison — the fumes are immediate and agonising. ({0} damage)"
    };

    private static readonly string[] _fireHazardLines =
    {
        "A gout of magical fire roars from runes on the floor — you're caught in the blast! ({0} damage)",
        "The floor glows red. Then the fire trap activates with a WHOMP that singes your eyebrows. ({0} damage)"
    };

    private static readonly string[] _lootLines =
    {
        "Every bit helps down here.",
        "You tuck it away carefully.",
        "Useful. Or sellable. Either way, it's yours now.",
        "Into the pack it goes."
    };

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
        _inventoryManager = new InventoryManager(display);
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
        _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
        _display.ShowMessage($"Floor {_currentFloor}");
        _display.ShowRoom(_currentRoom);
        _currentRoom.Visited = true;

        while (true)
        {
            _display.ShowCommandPrompt();
            var input = _input.ReadLine() ?? string.Empty;
            var cmd = CommandParser.Parse(input);
            _turnConsumed = true;

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
                    _display.ShowMessage($"Floor: {_currentFloor} / {FinalFloor}");
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
                    _stats.FinalLevel = _player.Level;
                    _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                    RecordRunEnd(won: false);
                    _display.ShowMessage("Thanks for playing!");
                    return;
                case CommandType.Descend:
                    HandleDescend();
                    break;
                case CommandType.Map:
                    _display.ShowMap(_currentRoom);
                    break;
                case CommandType.Shop:
                    HandleShop();
                    break;
                case CommandType.Prestige:
                    HandlePrestige();
                    break;
                case CommandType.Skills:
                    HandleSkills();
                    break;
                case CommandType.Learn:
                    HandleLearnSkill(cmd.Argument);
                    break;
                case CommandType.Craft:
                    HandleCraft(cmd.Argument);
                    break;
                case CommandType.Leaderboard:
                    HandleLeaderboard();
                    break;
                default:
                    _display.ShowError("Unknown command. Type HELP for commands.");
                    break;
            }
            if (_turnConsumed) _stats.TurnsTaken++;
            if (_gameOver) break;
        }
    }

    private void HandleGo(string directionStr)
    {
        if (string.IsNullOrWhiteSpace(directionStr))
        {
            _turnConsumed = false;
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
                _turnConsumed = false;
                _display.ShowError($"Invalid direction: {directionStr}");
                return;
        }

        if (!_currentRoom.Exits.TryGetValue(direction, out var nextRoom))
        {
            _turnConsumed = false;
            _display.ShowError("You can't go that way.");
            return;
        }

        // Move to new room
        var previousRoom = _currentRoom;
        _currentRoom = nextRoom;

        // ~15% chance of a brief atmospheric flavor message before the room description
        if (_narration.Chance(0.15))
            _display.ShowMessage(_narration.Pick(AmbientEvents.ForFloor(_currentFloor)));

        // Show revisit flavor when returning to an already-explored room
        if (_currentRoom.Visited)
        {
            _display.ShowMessage(_narration.Pick(RoomStateNarration.RevisitedRoom));
            _currentRoom.State = RoomState.Revisited;
        }

        _display.ShowRoom(_currentRoom);
        _currentRoom.Visited = true;
        _events?.RaiseRoomEntered(_player, _currentRoom, previousRoom);

        // Apply environmental hazard damage
        if (_currentRoom.Hazard != HazardType.None)
        {
            var dmg = _currentRoom.Hazard switch {
                HazardType.Spike => 5,
                HazardType.Poison => 3,
                HazardType.Fire => 7,
                _ => 0
            };
            _player.TakeDamage(dmg);
            string hazardMsg = _currentRoom.Hazard switch
            {
                HazardType.Spike  => _narration.Pick(_spikeHazardLines, dmg),
                HazardType.Poison => _narration.Pick(_poisonHazardLines, dmg),
                HazardType.Fire   => _narration.Pick(_fireHazardLines, dmg),
                _                 => $"You trigger a hazard and take {dmg} damage!"
            };
            _display.ShowMessage($"⚠ {hazardMsg} HP: {_player.HP}/{_player.MaxHP}");
            if (_player.HP <= 0)
            {
                ShowGameOver(byTrap: true);
                _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                _stats.Display(_display.ShowMessage);
                RecordRunEnd(won: false);
                _gameOver = true;
                return;
            }
        }

        // Notify about merchant if present
        if (_currentRoom.Merchant != null)
        {
            _display.ShowMessage("🛒 A merchant is here! Type SHOP to browse wares.");
        }

        // Check for enemy encounter
        if (_currentRoom.Enemy != null && _currentRoom.Enemy.HP > 0)
        {
            var killerName = _currentRoom.Enemy.Name;
            var result = _combat.RunCombat(_player, _currentRoom.Enemy, _stats);
            
            if (result == CombatResult.PlayerDied)
            {
                ShowGameOver(killedBy: killerName);
                _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                _stats.Display(_display.ShowMessage);
                RecordRunEnd(won: false);
                _gameOver = true;
                return;
            }
            
            if (result == CombatResult.Won)
            {
                var enemyName = _currentRoom.Enemy!.Name;
                _currentRoom.Enemy = null;
                _display.ShowMessage(_narration.Pick(_postCombatLines, enemyName));
                _currentRoom.State = RoomState.Cleared;
                _display.ShowMessage(_narration.Pick(RoomStateNarration.ClearedRoom));
            }

            if (result == CombatResult.Fled)
            {
                _display.ShowMessage("You flee back to the previous room!");
                _currentRoom = previousRoom;
                return;
            }
        }

        // Check win/floor condition
        if (_currentRoom.IsExit && _currentRoom.Enemy == null)
        {
            if (_currentFloor >= FinalFloor)
            {
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                ShowVictory();
                _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                _stats.Display(_display.ShowMessage);
                RecordRunEnd(won: true);
                _gameOver = true;
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
            _turnConsumed = false;
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
            _display.ShowItemDetail(roomItem);
            return;
        }

        // Check items in inventory
        var invItem = _player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(targetLower));
        if (invItem != null)
        {
            _display.ShowItemDetail(invItem);
            return;
        }

        _turnConsumed = false;
        _display.ShowError($"You don't see any '{target}' here.");
    }

    private void HandleTake(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _turnConsumed = false;
            _display.ShowError("Take what?");
            return;
        }

        var itemNameLower = itemName.ToLowerInvariant();
        var item = _currentRoom.Items.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            _turnConsumed = false;
            _display.ShowError($"There is no '{itemName}' here.");
            return;
        }

        _currentRoom.Items.Remove(item);
        if (!_inventoryManager.TryAddItem(_player, item))
        {
            _currentRoom.Items.Add(item);
            _turnConsumed = false;
            _display.ShowMessage($"{Systems.ColorCodes.Red}❌ Inventory full!{Systems.ColorCodes.Reset}");
            return;
        }
        int slotsCurrent = _player.Inventory.Count;
        int weightCurrent = _player.Inventory.Sum(i => i.Weight);
        _display.ShowItemPickup(item, slotsCurrent, Player.MaxInventorySize, weightCurrent, Systems.InventoryManager.MaxWeight);
        _display.ShowMessage(_narration.Pick(_lootLines));
        _events?.RaiseItemPicked(_player, item, _currentRoom);
        _stats.ItemsFound++;
        if (item.Type == ItemType.Gold) _stats.GoldCollected += item.StatModifier;
    }

    private void HandleUse(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _turnConsumed = false;
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
            _turnConsumed = false;
            _display.ShowError($"You don't have '{itemName}'.");
            return;
        }

        switch (item.Type)
        {
            case ItemType.Consumable:
                if (!string.IsNullOrEmpty(item.Description))
                    _display.ShowMessage(item.Description);
                if (item.HealAmount > 0)
                {
                    var oldHP = _player.HP;
                    _player.Heal(item.HealAmount);
                    var healedAmount = _player.HP - oldHP;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name} and restore {healedAmount} HP. Current HP: {_player.HP}/{_player.MaxHP}");
                }
                else if (item.ManaRestore > 0)
                {
                    var oldMana = _player.Mana;
                    _player.RestoreMana(item.ManaRestore);
                    var restoredMana = _player.Mana - oldMana;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name} and restore {restoredMana} mana. Mana: {_player.Mana}/{_player.MaxMana}");
                }
                else if (item.AttackBonus > 0)
                {
                    _player.ModifyAttack(item.AttackBonus);
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name}. Attack permanently +{item.AttackBonus}. Attack: {_player.Attack}");
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
            _turnConsumed = false;
            _display.ShowError("Save as what? Usage: SAVE <name>");
            return;
        }
        SaveSystem.SaveGame(new GameState(_player, _currentRoom, _currentFloor), saveName);
        _display.ShowMessage($"Game saved as '{saveName}'.");
    }

    private void HandleLoad(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            _turnConsumed = false;
            _display.ShowError("Load which save? Usage: LOAD <name>");
            return;
        }
        try
        {
            var state = SaveSystem.LoadGame(saveName);
            _player = state.Player;
            _currentRoom = state.CurrentRoom;
            _currentFloor = state.CurrentFloor;
            _runStart = DateTime.UtcNow;
            _display.ShowMessage($"Loaded save '{saveName}'.");
            _display.ShowRoom(_currentRoom);
        }
        catch (FileNotFoundException)
        {
            _turnConsumed = false;
            _display.ShowError($"Save '{saveName}' not found.");
        }
        catch (Exception ex)
        {
            _turnConsumed = false;
            _display.ShowError($"Failed to load save: {ex.Message}");
        }
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
            _turnConsumed = false;
            _display.ShowError("You can only descend at a cleared exit room.");
            return;
        }

        if (_currentFloor >= FinalFloor)
        {
            _stats.FinalLevel = _player.Level;
            _stats.TimeElapsed = DateTime.UtcNow - _runStart;
            ShowVictory();
            _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
            if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
            _stats.Display(_display.ShowMessage);
            RecordRunEnd(won: true);
            _gameOver = true;
            return;
        }

        _currentFloor++;
        foreach (var line in FloorTransitionNarration.GetSequence(_currentFloor))
            _display.ShowMessage(line);
        _display.ShowMessage($"You descend deeper into the dungeon... Floor {_currentFloor}");

        float floorMult = 1.0f + (_currentFloor - 1) * 0.5f;
        var floorSeed = _seed.HasValue ? _seed.Value + _currentFloor : (int?)null;
        var gen = new DungeonGenerator(floorSeed);
        var (newStart, _) = gen.Generate(floorMultiplier: floorMult, difficulty: _difficulty, floor: _currentFloor);
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

        _display.ShowColoredMessage("✨ [Shrine Menu] — press H/B/F/M or L to leave.", Systems.ColorCodes.Cyan);
        _display.ShowMessage($"[H]eal fully       - 30g  (Your gold: {_player.Gold})");
        _display.ShowMessage("[B]less            - 50g  (+2 ATK/DEF permanently)");
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

    private void HandlePrestige()
    {
        var data = PrestigeSystem.Load();
        _display.ShowMessage("=== PRESTIGE STATUS ===");
        _display.ShowMessage($"Prestige Level: {data.PrestigeLevel}");
        _display.ShowMessage($"Total Wins: {data.TotalWins} | Total Runs: {data.TotalRuns}");
        if (data.PrestigeLevel > 0)
            _display.ShowMessage($"Bonuses: +{data.BonusStartAttack} Attack, +{data.BonusStartDefense} Defense, +{data.BonusStartHP} Max HP");
        else
            _display.ShowMessage("Win 3 runs to earn your first Prestige level!");
    }

    private void HandleShop()
    {
        if (_currentRoom.Merchant == null)
        {
            _turnConsumed = false;
            _display.ShowError("There is no merchant here.");
            return;
        }

        var merchant = _currentRoom.Merchant;
        _display.ShowMessage($"=== MERCHANT SHOP ({merchant.Name}) ===");
        _display.ShowShop(merchant.Stock.Select(mi => (mi.Item, mi.Price)), _player.Gold);
        _display.ShowCommandPrompt();

        var input = _input.ReadLine()?.Trim() ?? "";
        if (input.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            _display.ShowMessage("You leave the shop.");
            return;
        }

        if (int.TryParse(input, out var choice) && choice >= 1 && choice <= merchant.Stock.Count)
        {
            var selected = merchant.Stock[choice - 1];
            if (_player.Gold < selected.Price)
            {
                _display.ShowMessage("Not enough gold.");
            }
            else
            {
                _player.SpendGold(selected.Price);
                if (!_inventoryManager.TryAddItem(_player, selected.Item))
                {
                    _player.AddGold(selected.Price); // refund — inventory was full or too heavy
                    _display.ShowMessage("Your inventory is full. You can't carry that item.");
                }
                else
                {
                    merchant.Stock.RemoveAt(choice - 1);
                    _display.ShowMessage($"You bought {selected.Item.Name} for {selected.Price}g. Gold remaining: {_player.Gold}g");
                }
            }
        }
        else
        {
            _display.ShowMessage("Leaving the shop.");
        }
    }

    private void HandleSkills()
    {
        _display.ShowMessage("=== SKILL TREE ===");
        _display.ShowMessage($"Your level: {_player.Level}");
        foreach (Skill skill in Enum.GetValues<Skill>())
        {
            var unlocked = _player.Skills.IsUnlocked(skill);
            var minLevel = skill switch {
                Skill.PowerStrike => 3, Skill.IronSkin => 3, Skill.Swiftness => 5,
                Skill.ManaFlow => 4, Skill.BattleHardened => 6, _ => 1
            };
            var status = unlocked ? "✅ Unlocked" : $"Locked (need Lv{minLevel})";
            _display.ShowMessage($"  {skill}: {SkillTree.GetDescription(skill)} [{status}]");
        }
        _display.ShowMessage("Type LEARN <skill> to unlock a skill.");
    }

    private void HandleLearnSkill(string skillName)
    {
        if (!Enum.TryParse<Skill>(skillName, ignoreCase: true, out var skill))
        {
            _turnConsumed = false;
            _display.ShowError($"Unknown skill: {skillName}");
            return;
        }
        if (_player.Skills.TryUnlock(_player, skill))
            _display.ShowMessage($"You learned {skill}! {SkillTree.GetDescription(skill)}");
        else if (_player.Skills.IsUnlocked(skill))
        {
            _turnConsumed = false;
            _display.ShowError($"You already know {skill}.");
        }
        else
        {
            _turnConsumed = false;
            _display.ShowError($"You need to be higher level to learn {skill}.");
        }
    }
    private void HandleCraft(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
        {
            _turnConsumed = false;
            _display.ShowMessage("=== CRAFTING RECIPES ===");
            foreach (var r in CraftingSystem.Recipes)
            {
                var ingredientsWithAvailability = r.Ingredients
                    .Select(ing => (
                        $"{ing.Count}x {ing.ItemName}",
                        _player.Inventory.Count(i => i.Name.Equals(ing.ItemName, StringComparison.OrdinalIgnoreCase)) >= ing.Count
                    ))
                    .ToList();
                _display.ShowCraftRecipe(r.Name, r.Result, ingredientsWithAvailability);
            }
            _display.ShowMessage("Type CRAFT <recipe name> to craft.");
            return;
        }

        var recipe = CraftingSystem.Recipes.FirstOrDefault(r =>
            r.Name.Contains(recipeName, StringComparison.OrdinalIgnoreCase));
        if (recipe == null)
        {
            _turnConsumed = false;
            _display.ShowError($"Unknown recipe: {recipeName}");
            return;
        }

        var (success, msg) = CraftingSystem.TryCraft(_player, recipe);
        if (success) _display.ShowMessage(msg);
        else _display.ShowError(msg);
    }

    /// <summary>
    /// Records the end of a run: persists to history, updates prestige, and (on win)
    /// evaluates and displays any newly unlocked achievements. Must be called after
    /// <see cref="RunStats.FinalLevel"/> and <see cref="RunStats.TimeElapsed"/> are set.
    /// </summary>
    private void RecordRunEnd(bool won)
    {
        RunStats.AppendToHistory(_stats, won);
        PrestigeSystem.RecordRun(won);
        if (won)
        {
            var unlocked = _achievements.Evaluate(_stats, _player, won: true);
            if (unlocked.Count > 0)
            {
                _display.ShowMessage("=== ACHIEVEMENTS UNLOCKED ===");
                foreach (var a in unlocked)
                    _display.ShowMessage($"🏆 {a.Name} — {a.Description}");
            }
        }
    }

    /// <summary>
    /// Displays a contextual death banner, a floor-specific opening line, a cause-of-death
    /// line (trap or named killer), and a class-specific epitaph. Stats and achievements
    /// should be displayed by the caller after this method returns.
    /// </summary>
    /// <param name="killedBy">Name of the enemy that killed the player, or <see langword="null"/> for non-combat deaths.</param>
    /// <param name="byTrap">
    /// <see langword="true"/> when the player was killed by an environmental hazard rather than a monster.
    /// </param>
    private void ShowGameOver(string? killedBy = null, bool byTrap = false)
    {
        _display.ShowGameOver(_player, killedBy, _stats);
    }

    private void HandleLeaderboard()
    {
        _display.ShowMessage("=== TOP RUNS ===");
        var top = RunStats.GetTopRuns(5);
        if (top.Count == 0)
        {
            _display.ShowMessage("No completed runs yet. Be the first!");
            return;
        }
        for (int i = 0; i < top.Count; i++)
        {
            var r = top[i];
            var won = r.Won ? "✅" : "💀";
            _display.ShowMessage($"#{i + 1} {won} Level {r.FinalLevel} | {r.EnemiesDefeated} enemies | {r.GoldCollected}g");
        }
    }

    /// <summary>
    /// Displays the class-aware victory banner and run summary when the player wins the run.
    /// Must be called after <see cref="RunStats.FinalLevel"/> and <see cref="RunStats.TimeElapsed"/>
    /// have been set so that the summary line reflects accurate stats.
    /// </summary>
    private void ShowVictory()
    {
        _display.ShowVictory(_player, _currentFloor, _stats);
    }

}