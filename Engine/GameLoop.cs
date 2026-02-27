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
    private readonly IReadOnlyList<Item> _allItems = [];
    private readonly NarrationService _narration = new();
    private int _currentFloor = 1;
    private bool _turnConsumed;

    /// <summary>Set to <see langword="true"/> when the run ends (win, death) to break the Run() loop.</summary>
    private bool _gameOver = false;

    private const int FinalFloor = 8;

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
    public GameLoop(IDisplayService display, ICombatEngine combat, IInputReader? input = null, GameEvents? events = null, int? seed = null, DifficultySettings? difficulty = null, IReadOnlyList<Item>? allItems = null)
    {
        _display = display;
        _combat = combat;
        _input = input ?? new ConsoleInputReader();
        _events = events;
        _seed = seed;
        _difficulty = difficulty ?? DifficultySettings.For(Difficulty.Normal);
        _equipment = new EquipmentManager(display);
        _inventoryManager = new InventoryManager(display);
        _allItems = allItems ?? [];
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
                case CommandType.Sell:
                    HandleSell();
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
            if (_turnConsumed && !_gameOver) ApplyRoomHazard(_currentRoom, _player);
            if (_player.HP <= 0 && !_gameOver)
            {
                ShowGameOver(killedBy: "environmental hazard");
                _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                _stats.Display(_display.ShowMessage);
                RecordRunEnd(won: false);
                _gameOver = true;
            }
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
            _stats.DamageTaken += dmg;
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
                ShowGameOver(killedBy: "a dungeon trap");
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

        // Auto-trigger PetrifiedLibrary on first entry
        if (_currentRoom.Type == RoomType.PetrifiedLibrary && !_currentRoom.SpecialRoomUsed)
        {
            _currentRoom.SpecialRoomUsed = true;
            HandlePetrifiedLibrary();
        }

        // Auto-trigger TrapRoom on first entry
        if (_currentRoom.Type == RoomType.TrapRoom && !_currentRoom.SpecialRoomUsed)
            HandleTrapRoom();

        // Prompt for ContestedArmory
        if (_currentRoom.Type == RoomType.ContestedArmory && !_currentRoom.SpecialRoomUsed)
        {
            _display.ShowMessage("⚔ Trapped weapons line the walls. (USE ARMORY to approach)");
        }
    }

    private void HandleLook()
    {
        _display.ShowRoom(_currentRoom);
    }

    private void ApplyRoomHazard(Room room, Player player)
    {
        switch (room.EnvironmentalHazard)
        {
            case RoomHazard.LavaSeam:
                player.HP = Math.Max(0, player.HP - 5);
                _stats.DamageTaken += 5;
                _display.ShowMessage("🔥 The lava seam sears you. (-5 HP)");
                break;
            case RoomHazard.CorruptedGround:
                player.HP = Math.Max(0, player.HP - 3);
                _stats.DamageTaken += 3;
                _display.ShowMessage("💀 The corrupted ground drains you. (-3 HP)");
                break;
            case RoomHazard.BlessedClearing:
                if (!room.BlessedHealApplied)
                {
                    room.BlessedHealApplied = true;
                    player.HP = Math.Min(player.MaxHP, player.HP + 3);
                    _display.ShowMessage("✨ A blessed warmth flows through you. (+3 HP)");
                }
                break;
        }
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
        _display.ShowMessage(Systems.ItemInteractionNarration.PickUp(item));
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

        // Special: USE ARMORY
        if (itemName.Equals("armory", StringComparison.OrdinalIgnoreCase))
        {
            HandleContestedArmory();
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
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, healedAmount));
                }
                else if (item.ManaRestore > 0)
                {
                    var oldMana = _player.Mana;
                    _player.RestoreMana(item.ManaRestore);
                    var restoredMana = _player.Mana - oldMana;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name} and restore {restoredMana} mana. Mana: {_player.Mana}/{_player.MaxMana}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.AttackBonus > 0)
                {
                    _player.ModifyAttack(item.AttackBonus);
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name}. Attack permanently +{item.AttackBonus}. Attack: {_player.Attack}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.DefenseBonus > 0)
                {
                    _player.ModifyDefense(item.DefenseBonus);
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"You use {item.Name}. Defense permanently +{item.DefenseBonus}. Defense: {_player.Defense}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "bone_flute")
                {
                    _player.ActiveMinions.Add(new Models.Minion { Name = "Skeletal Ally", HP = 60, MaxHP = 60, ATK = 15, AttackFlavorText = "The Skeletal Ally rattles forward and strikes!" });
                    _player.Inventory.Remove(item);
                    _display.ShowMessage("The flute's hollow note summons a Skeletal Ally to fight alongside you!");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "dragonheart_elixir")
                {
                    _player.FortifyMaxHP(100);
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"Dragonheart warmth spreads through you. MaxHP +100! ({_player.MaxHP} MaxHP)");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "cure_all")
                {
                    _player.ActiveEffects.RemoveAll(e => e.IsDebuff);
                    _player.Heal(_player.MaxHP);
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"The Panacea purges all ailments and restores you to full health. HP: {_player.HP}/{_player.MaxHP}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "berserk_buff")
                {
                    int atkGain = Math.Max(1, _player.Attack / 2);
                    int defLoss = Math.Max(1, _player.Defense * 3 / 10);
                    _player.ModifyAttack(atkGain);
                    _player.TempAttackBonus += atkGain;
                    _player.ModifyDefense(-defLoss);
                    _player.TempDefenseBonus -= defLoss;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"Rage floods your veins. ATK +{atkGain}, DEF -{defLoss} until next floor. ATK: {_player.Attack}, DEF: {_player.Defense}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "stone_skin_buff")
                {
                    int defGain = Math.Max(1, _player.Defense * 2 / 5);
                    _player.ModifyDefense(defGain);
                    _player.TempDefenseBonus += defGain;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"Your skin hardens to granite. DEF +{defGain} until next floor. DEF: {_player.Defense}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
                }
                else if (item.PassiveEffectId == "swiftness_buff")
                {
                    int atkGain = Math.Max(1, _player.Attack / 4);
                    _player.ModifyAttack(atkGain);
                    _player.TempAttackBonus += atkGain;
                    _player.Inventory.Remove(item);
                    _display.ShowMessage($"The world slows; you do not. ATK +{atkGain} until next floor. ATK: {_player.Attack}");
                    _display.ShowMessage(Systems.ItemInteractionNarration.UseConsumable(item, 0));
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
        _stats.FloorsVisited = _currentFloor;
        if (_player.TempAttackBonus > 0) { _player.ModifyAttack(-_player.TempAttackBonus); _player.TempAttackBonus = 0; }
        if (_player.TempDefenseBonus != 0) { _player.ModifyDefense(-_player.TempDefenseBonus); _player.TempDefenseBonus = 0; }
        _player.WardingVeilActive = false;
        foreach (var line in FloorTransitionNarration.GetSequence(_currentFloor))
            _display.ShowMessage(line);
        _display.ShowMessage($"You descend deeper into the dungeon... Floor {_currentFloor}");

        float floorMult = 1.0f + (_currentFloor - 1) * 0.5f;
        var floorSeed = _seed.HasValue ? _seed.Value + _currentFloor : (int?)null;
        var gen = new DungeonGenerator(floorSeed, _allItems);
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
        bool isForgottenShrine = _currentRoom.Type == RoomType.ForgottenShrine;
        if (isForgottenShrine && !_currentRoom.SpecialRoomUsed) { HandleForgottenShrine(); return; }

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

        // SacredGroundActive auto-heal on shrine entry
        if (_player.SacredGroundActive)
        {
            var healed = _player.MaxHP - _player.HP;
            if (healed > 0)
            {
                _player.Heal(healed);
                _display.ShowMessage($"Sacred Ground pulses beneath your feet, restoring you to full health! HP: {_player.HP}/{_player.MaxHP}");
            }
            _player.SacredGroundActive = false;
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
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantHeal));
                break;
            case "B":
                if (_player.Gold < 50) { _display.ShowError("Not enough gold (need 50g)."); return; }
                _player.SpendGold(50);
                _player.ModifyAttack(2);
                _player.ModifyDefense(2);
                _display.ShowMessage("The shrine blesses you! +2 ATK/DEF.");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantPower));
                break;
            case "F":
                if (_player.Gold < 75) { _display.ShowError("Not enough gold (need 75g)."); return; }
                _player.SpendGold(75);
                _player.FortifyMaxHP(10);
                _display.ShowMessage($"The shrine fortifies you! MaxHP permanently +10. ({_player.MaxHP} MaxHP)");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantProtection));
                break;
            case "M":
                if (_player.Gold < 75) { _display.ShowError("Not enough gold (need 75g)."); return; }
                _player.SpendGold(75);
                _player.FortifyMaxMana(10);
                _display.ShowMessage($"The shrine expands your mind! MaxMana permanently +10. ({_player.MaxMana} MaxMana)");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantWisdom));
                break;
            case "L":
                _display.ShowMessage("You leave the shrine.");
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantNothing));
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

    private void HandleForgottenShrine()
    {
        _display.ShowColoredMessage("🕯 [Forgotten Shrine] — choose a blessing:", Systems.ColorCodes.Cyan);
        _display.ShowMessage("[1] Holy Strength   — +5 ATK (lasts until next floor)");
        _display.ShowMessage("[2] Sacred Ground   — Auto-heal at shrines");
        _display.ShowMessage("[3] Warding Veil    — 20% chance to deflect enemy attacks this floor");
        _display.ShowMessage("[L]eave");
        _display.ShowCommandPrompt();

        var choice = _input.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        switch (choice)
        {
            case "1":
                _player.TempAttackBonus += 5;
                _player.ModifyAttack(5);
                _display.ShowMessage("Holy light surges through your arms. Attack +5 until next floor!");
                _currentRoom.SpecialRoomUsed = true;
                break;
            case "2":
                _player.SacredGroundActive = true;
                _display.ShowMessage("The shrine's blessing lingers — shrines will restore you fully.");
                _currentRoom.SpecialRoomUsed = true;
                break;
            case "3":
                _player.WardingVeilActive = true;
                _display.ShowMessage("A shimmering veil of protection wraps around you. Enemy attacks have a 20% chance to miss this floor.");
                _currentRoom.SpecialRoomUsed = true;
                break;
            case "L":
                _display.ShowMessage("You leave the forgotten shrine.");
                break;
            default:
                _display.ShowError("Invalid choice.");
                break;
        }
    }

    private void HandlePetrifiedLibrary()
    {
        var roll = new Random().Next(3);
        switch (roll)
        {
            case 0:
                _player.FortifyMaxHP(10);
                _display.ShowMessage("📜 Scroll of Fortitude: Ancient runes fortify your body. MaxHP +10!");
                break;
            case 1:
                _player.AddXP(15);
                _display.ShowMessage("📖 Tome of the Archivist: Forbidden knowledge floods your mind. +15 XP!");
                break;
            default:
                var exitRoom = FindExitRoom();
                if (exitRoom != null)
                {
                    _display.ShowMessage("📙 Codex of Names: The pages reveal the guardian ahead...");
                    if (exitRoom.Enemy != null)
                        _display.ShowMessage($"  ⚠ Boss: {exitRoom.Enemy.Name} — HP {exitRoom.Enemy.HP}, ATK {exitRoom.Enemy.Attack}, DEF {exitRoom.Enemy.Defense}");
                    else
                        _display.ShowMessage("  The exit room appears to be clear of guardians.");
                }
                else
                {
                    _display.ShowMessage("📙 Codex of Names: The pages crumble — no guardian is recorded.");
                }
                break;
        }
    }

    private void HandleContestedArmory()
    {
        if (_currentRoom.Type != RoomType.ContestedArmory)
        {
            _display.ShowError("There is no armory here.");
            return;
        }
        if (_currentRoom.SpecialRoomUsed)
        {
            _display.ShowMessage("The armory has already been looted.");
            return;
        }

        _display.ShowColoredMessage("⚔ [Contested Armory] — how do you approach?", Systems.ColorCodes.Cyan);
        _display.ShowMessage($"[1] Careful approach — disarm traps (requires DEF > 12, yours: {_player.Defense})");
        _display.ShowMessage("[2] Reckless grab   — take what you can (15-30 damage)");
        _display.ShowMessage("[L]eave");
        _display.ShowCommandPrompt();

        var armoryChoice = _input.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        switch (armoryChoice)
        {
            case "1":
                if (_player.Defense > 12)
                {
                    _display.ShowMessage("You carefully disarm the traps and claim a fine weapon.");
                    GiveArmoryLoot(uncommon: false);
                    _currentRoom.SpecialRoomUsed = true;
                }
                else
                {
                    _display.ShowMessage("You lack the fortitude to safely disarm the traps. Try a reckless grab or leave.");
                }
                break;
            case "2":
                var dmg = new Random().Next(15, 31);
                _player.TakeDamage(dmg);
                _stats.DamageTaken += dmg;
                _display.ShowMessage($"Blades nick you as you grab the weapon! -{dmg} HP. HP: {_player.HP}/{_player.MaxHP}");
                GiveArmoryLoot(uncommon: true);
                _currentRoom.SpecialRoomUsed = true;
                if (_player.HP <= 0)
                {
                    ShowGameOver(killedBy: "an armory trap");
                    _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
                    if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
                    _stats.FinalLevel = _player.Level;
                    _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                    _stats.Display(_display.ShowMessage);
                    RecordRunEnd(won: false);
                    _gameOver = true;
                }
                break;
            case "L":
                _display.ShowMessage("You leave the armory untouched.");
                break;
            default:
                _display.ShowError("Invalid choice.");
                break;
        }
    }

    private void GiveArmoryLoot(bool uncommon)
    {
        var tier = uncommon ? Models.ItemTier.Uncommon : Models.ItemTier.Rare;
        var loot = Models.LootTable.RollArmorTier(tier);
        if (loot != null)
        {
            if (_player.Inventory.Count >= Player.MaxInventorySize)
                _display.ShowMessage($"Your inventory is full! You leave {loot.Name} behind.");
            else
            {
                _player.Inventory.Add(loot);
                _display.ShowMessage($"You obtained: {loot.Name}!");
            }
        }
        else
        {
            _display.ShowMessage("The armory yields only dust.");
        }
    }

    private void HandleTrapRoom()
    {
        var variant = _currentRoom.Trap ?? TrapVariant.ArrowVolley;
        var rng = new Random();

        // Show dramatic room description
        string roomDesc = variant switch
        {
            TrapVariant.ArrowVolley     => "⚠ The walls bristle with arrow slits. Pressure plates line the floor.",
            TrapVariant.PoisonGas       => "⚠ Yellow-green mist seeps from vents in the ceiling.",
            TrapVariant.CollapsingFloor => "⚠ The floor ahead is riddled with cracks. Each step could be the last.",
            _                           => "⚠ A dangerous trap room."
        };
        _display.ShowColoredMessage(roomDesc, Systems.ColorCodes.Yellow);

        switch (variant)
        {
            case TrapVariant.ArrowVolley:
                _display.ShowMessage("[1] Raise your shield — 70% chance to block (no damage); 30% chance to take 15 damage");
                _display.ShowMessage("[2] Sprint through  — take 8 damage, but find a loot cache");
                _display.ShowCommandPrompt();
                var avChoice = _input.ReadLine()?.Trim() ?? "";
                if (avChoice == "1")
                {
                    if (rng.NextDouble() < 0.70)
                    {
                        _display.ShowMessage("You raise your shield and the arrows clatter harmlessly off it. You pass unscathed.");
                    }
                    else
                    {
                        _player.TakeDamage(15);
                        _stats.DamageTaken += 15;
                        _display.ShowMessage($"An arrow slips past your guard! -15 HP. HP: {_player.HP}/{_player.MaxHP}");
                        if (_player.HP <= 0) { ShowGameOver(killedBy: "a dungeon trap"); _display.ShowMessage($"Difficulty: {GetDifficultyName()}"); if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}"); _stats.FinalLevel = _player.Level; _stats.TimeElapsed = DateTime.UtcNow - _runStart; _stats.Display(_display.ShowMessage); RecordRunEnd(won: false); _gameOver = true; return; }
                    }
                    GiveTrapLoot(rng.NextDouble() < 0.5 ? Models.ItemTier.Uncommon : Models.ItemTier.Common,
                        "You spot a small cache behind the arrow slits.");
                }
                else if (avChoice == "2")
                {
                    _player.TakeDamage(8);
                    _stats.DamageTaken += 8;
                    _display.ShowMessage($"You sprint through, arrows grazing your side! -8 HP. HP: {_player.HP}/{_player.MaxHP}");
                    if (_player.HP <= 0) { ShowGameOver(killedBy: "a dungeon trap"); _display.ShowMessage($"Difficulty: {GetDifficultyName()}"); if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}"); _stats.FinalLevel = _player.Level; _stats.TimeElapsed = DateTime.UtcNow - _runStart; _stats.Display(_display.ShowMessage); RecordRunEnd(won: false); _gameOver = true; return; }
                    GiveTrapLoot(Models.ItemTier.Uncommon, "Your speed reveals a hidden loot cache!");
                }
                else
                {
                    _display.ShowMessage("You hesitate — the trap remains.");
                    _currentRoom.SpecialRoomUsed = false;
                    return;
                }
                break;

            case TrapVariant.PoisonGas:
                _display.ShowMessage("[1] Hold your breath and sprint — 60% escape unharmed; 40% get Poisoned (3 turns)");
                _display.ShowMessage("[2] Find a bypass route    — always safe; 80% chance to find an Uncommon item");
                _display.ShowCommandPrompt();
                var pgChoice = _input.ReadLine()?.Trim() ?? "";
                if (pgChoice == "1")
                {
                    if (rng.NextDouble() < 0.60)
                    {
                        _display.ShowMessage("You dash through the mist, lungs burning — but you make it clean!");
                    }
                    else
                    {
                        _player.ActiveEffects.Add(new Models.ActiveEffect(Models.StatusEffect.Poison, 3));
                        _display.ShowMessage("The gas floods your lungs. You have been Poisoned for 3 turns!");
                    }
                }
                else if (pgChoice == "2")
                {
                    _display.ShowMessage("You take the long way round, tracing the walls for a safe passage.");
                    if (rng.NextDouble() < 0.80)
                        GiveTrapLoot(Models.ItemTier.Uncommon, "Your careful detour leads you past a forgotten cache.");
                    else
                        _display.ShowMessage("The bypass yields nothing but cobwebs.");
                }
                else
                {
                    _display.ShowMessage("You hesitate — the gas continues to seep.");
                    _currentRoom.SpecialRoomUsed = false;
                    return;
                }
                break;

            case TrapVariant.CollapsingFloor:
                _display.ShowMessage("[1] Leap across quickly     — 75% cross safely and find a Rare item; 25% take 20 damage");
                _display.ShowMessage("[2] Cross carefully, test each step — 100% safe, no loot, slow");
                _display.ShowCommandPrompt();
                var cfChoice = _input.ReadLine()?.Trim() ?? "";
                if (cfChoice == "1")
                {
                    if (rng.NextDouble() < 0.75)
                    {
                        _display.ShowMessage("You leap with precision — the floor crumbles behind you as you land safely!");
                        GiveTrapLoot(Models.ItemTier.Rare, "In the far corner, a rare item waits for you.");
                    }
                    else
                    {
                        _player.TakeDamage(20);
                        _stats.DamageTaken += 20;
                        _display.ShowMessage($"The floor gives way! You plummet into rubble! -20 HP. HP: {_player.HP}/{_player.MaxHP}");
                        if (_player.HP <= 0) { ShowGameOver(killedBy: "a dungeon trap"); _display.ShowMessage($"Difficulty: {GetDifficultyName()}"); if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}"); _stats.FinalLevel = _player.Level; _stats.TimeElapsed = DateTime.UtcNow - _runStart; _stats.Display(_display.ShowMessage); RecordRunEnd(won: false); _gameOver = true; return; }
                    }
                }
                else if (cfChoice == "2")
                {
                    _display.ShowMessage("Inch by inch you test every stone. It takes an age, but you reach the other side safely.");
                }
                else
                {
                    _display.ShowMessage("You hesitate at the edge.");
                    _currentRoom.SpecialRoomUsed = false;
                    return;
                }
                break;
        }

        _currentRoom.SpecialRoomUsed = true;
    }

    private void GiveTrapLoot(Models.ItemTier tier, string message)
    {
        var loot = Models.LootTable.RollTier(tier);
        if (loot == null) return;
        _display.ShowMessage(message);
        if (_player.Inventory.Count >= Player.MaxInventorySize)
        {
            _display.ShowMessage($"Your inventory is full! You leave {loot.Name} behind.");
        }
        else
        {
            _player.Inventory.Add(loot);
            _display.ShowMessage($"You obtained: {loot.Name}!");
        }
    }

    private Room? FindExitRoom()
    {
        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(_currentRoom);
        visited.Add(_currentRoom);
        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            if (room.IsExit) return room;
            foreach (var next in room.Exits.Values)
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }
        return null;
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
        _display.ShowMessage(Systems.MerchantNarration.GetFloorGreeting(_currentFloor));
        _display.ShowShop(merchant.Stock.Select(mi => (mi.Item, mi.Price)), _player.Gold);
        _display.ShowColoredMessage("  💰 Type SELL to sell items to the merchant.", Systems.ColorCodes.Gray);
        _display.ShowCommandPrompt();

        var input = _input.ReadLine()?.Trim() ?? "";
        if (input.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            _display.ShowMessage("You leave the shop.");
            _display.ShowMessage(_narration.Pick(Systems.MerchantNarration.NoBuy));
            return;
        }

        if (int.TryParse(input, out var choice) && choice >= 1 && choice <= merchant.Stock.Count)
        {
            var selected = merchant.Stock[choice - 1];
            if (_player.Gold < selected.Price)
            {
                _display.ShowMessage(Systems.MerchantNarration.GetCantAfford());
            }
            else
            {
                _player.SpendGold(selected.Price);
                if (!_inventoryManager.TryAddItem(_player, selected.Item))
                {
                    _player.AddGold(selected.Price); // refund — inventory was full or too heavy
                    _display.ShowMessage(Systems.MerchantNarration.GetInventoryFull());
                }
                else
                {
                    merchant.Stock.RemoveAt(choice - 1);
                    _display.ShowMessage($"You bought {selected.Item.Name} for {selected.Price}g. Gold remaining: {_player.Gold}g");
                    _display.ShowMessage(_narration.Pick(Systems.MerchantNarration.AfterPurchase));
                }
            }
        }
        else
        {
            _display.ShowMessage("Leaving the shop.");
            _display.ShowMessage(_narration.Pick(Systems.MerchantNarration.NoBuy));
        }
    }

    private void HandleSell()
    {
        if (_currentRoom.Merchant == null)
        {
            _display.ShowError("There is no merchant here.");
            return;
        }

        // Items in _player.Inventory are already unequipped; exclude Gold-type items
        var sellable = _player.Inventory
            .Where(i => i.Type != ItemType.Gold)
            .ToList();

        if (!sellable.Any())
        {
            _display.ShowMessage(MerchantNarration.GetNoSell());
            return;
        }

        _display.ShowSellMenu(sellable.Select(i => (i, MerchantInventoryConfig.ComputeSellPrice(i))), _player.Gold);
        _display.ShowCommandPrompt();

        var input = _input.ReadLine()?.Trim() ?? "";
        if (input.Equals("x", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(input))
            return;

        if (!int.TryParse(input, out int idx) || idx < 1 || idx > sellable.Count)
        {
            _display.ShowError("Invalid selection.");
            return;
        }

        var item = sellable[idx - 1];
        int price = MerchantInventoryConfig.ComputeSellPrice(item);

        _display.ShowMessage($"Sell {item.Name} for {price}g? [Y/N]");
        var confirm = _input.ReadLine()?.Trim() ?? "";
        if (!confirm.Equals("y", StringComparison.OrdinalIgnoreCase) &&
            !confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            _display.ShowMessage("Changed your mind.");
            return;
        }

        _player.Inventory.Remove(item);
        _player.AddGold(price);
        _display.ShowMessage($"You sold {item.Name} for {price}g. Gold remaining: {_player.Gold}g");
        if (item.Tier == Models.ItemTier.Legendary)
            _display.ShowMessage(Systems.MerchantNarration.GetLegendarySold());
        else
            _display.ShowMessage(MerchantNarration.GetAfterSale());
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
                        $"{ing.Count}x {ing.DisplayName}",
                        _player.Inventory.Count(i => i.Name.Equals(ing.DisplayName, StringComparison.OrdinalIgnoreCase)) >= ing.Count
                    ))
                    .ToList();
                _display.ShowCraftRecipe(r.Name, r.Result.ToItem(), ingredientsWithAvailability);
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