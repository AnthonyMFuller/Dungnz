namespace Dungnz.Engine;

using System;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;

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
    private int? _seed;
    private readonly DifficultySettings _difficulty;
    private Difficulty _difficultyLevel = Difficulty.Normal;
    private Player _player = null!;
    private Room _currentRoom = null!;
    private RunStats _stats = null!;
    private SessionStats _sessionStats = new();
    private DateTime _runStart;
    private Random _rng = new();
    private readonly AchievementSystem _achievements = new();
    private readonly EquipmentManager _equipment;
    private readonly InventoryManager _inventoryManager;
    private readonly IMenuNavigator? _navigator;
    private readonly IReadOnlyList<Item> _allItems = [];
    private readonly NarrationService _narration = new();
    private readonly ILogger<GameLoop> _logger;
    private int _currentFloor = 1;
    private bool _turnConsumed;

    /// <summary>Set to <see langword="true"/> when the run ends (win, death) to break the Run() loop.</summary>
    private bool _gameOver = false;

    private readonly Dictionary<CommandType, ICommandHandler> _handlers;
    private CommandContext _context = null!;

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
    /// <param name="allItems">
    /// The full item catalog used for shop and loot generation,
    /// or <see langword="null"/> to disable item-dependent features.
    /// </param>
    /// <param name="navigator">
    /// An optional menu navigator override; when <see langword="null"/> a default
    /// <see cref="Spectre.Console"/> selection prompt is used.
    /// </param>
    /// <param name="logger">
    /// Optional logger for structured event tracking. When <see langword="null"/> a no-op logger is used.
    /// </param>
    public GameLoop(IDisplayService display, ICombatEngine combat, IInputReader? input = null, GameEvents? events = null, int? seed = null, DifficultySettings? difficulty = null, IReadOnlyList<Item>? allItems = null, IMenuNavigator? navigator = null, ILogger<GameLoop>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(combat);
        _display = display;
        _combat = combat;
        _input = input ?? new ConsoleInputReader();
        _events = events;
        _seed = seed;
        _difficulty = difficulty ?? DifficultySettings.For(Difficulty.Normal);
        _equipment = new EquipmentManager(display);
        _inventoryManager = new InventoryManager(display);
        _allItems = allItems ?? [];
        _navigator = navigator;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GameLoop>.Instance;

        _handlers = new Dictionary<CommandType, ICommandHandler>
        {
            [CommandType.Go]          = new GoCommandHandler(),
            [CommandType.Look]        = new LookCommandHandler(),
            [CommandType.Examine]     = new ExamineCommandHandler(),
            [CommandType.Take]        = new TakeCommandHandler(),
            [CommandType.Use]         = new UseCommandHandler(),
            [CommandType.Inventory]   = new InventoryCommandHandler(),
            [CommandType.Stats]       = new StatsCommandHandler(),
            [CommandType.Help]        = new HelpCommandHandler(),
            [CommandType.Save]        = new SaveCommandHandler(),
            [CommandType.Load]        = new LoadCommandHandler(),
            [CommandType.ListSaves]   = new ListSavesCommandHandler(),
            [CommandType.Descend]     = new DescendCommandHandler(),
            [CommandType.Ascend]      = new AscendCommandHandler(),
            [CommandType.Back]        = new BackCommandHandler(),
            [CommandType.Map]         = new MapCommandHandler(),
            [CommandType.Shop]        = new ShopCommandHandler(),
            [CommandType.Sell]        = new SellCommandHandler(),
            [CommandType.Prestige]    = new PrestigeCommandHandler(),
            [CommandType.Skills]      = new SkillsCommandHandler(),
            [CommandType.Learn]       = new LearnCommandHandler(),
            [CommandType.Craft]       = new CraftCommandHandler(),
            [CommandType.Leaderboard] = new LeaderboardCommandHandler(),
            [CommandType.Compare]     = new CompareCommandHandler(),
            [CommandType.Equip]       = new EquipCommandHandler(),
            [CommandType.Unequip]     = new UnequipCommandHandler(),
            [CommandType.Equipment]   = new EquipmentCommandHandler(),
        };
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
        if (_display is Display.Spectre.SpectreLayoutDisplayService spectre)
            spectre.Reset();
        
        _player = player;
        _currentRoom = startRoom;
        _stats = new RunStats();
        _sessionStats = new SessionStats();
        _runStart = DateTime.UtcNow;
        _rng = _seed.HasValue ? new Random(_seed.Value) : new Random();
        _currentFloor = 1;
        _difficultyLevel = _difficulty.EnemyStatMultiplier < 1.0f ? Difficulty.Casual
                         : _difficulty.EnemyStatMultiplier > 1.0f ? Difficulty.Hard
                         : Difficulty.Normal;
        _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
        _display.ShowMessage($"Floor {_currentFloor}");
        _display.RefreshDisplay(player, _currentRoom, _currentFloor);
        _currentRoom.Visited = true;

        InitContext();
        _context.FloorHistory[1] = startRoom;
        _context.FloorEntranceRoom = startRoom;
        RunLoop();
    }

    /// <summary>
    /// Starts the game loop from a previously saved GameState, restoring the player's
    /// position, floor, seed, and all associated state.
    /// </summary>
    public void Run(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(state.Player);
        ArgumentNullException.ThrowIfNull(state.CurrentRoom);
        
        if (_display is Display.Spectre.SpectreLayoutDisplayService spectre)
            spectre.Reset();
        
        _player = state.Player;
        _currentRoom = state.CurrentRoom;
        _currentFloor = state.CurrentFloor;
        _seed = state.Seed;
        _difficultyLevel = state.Difficulty;
        _stats = new RunStats();
        _sessionStats = new SessionStats();
        _runStart = DateTime.UtcNow;
        _rng = _seed.HasValue ? new Random(_seed.Value) : new Random();

        _display.ShowMessage($"Loaded save — Floor {_currentFloor}");
        _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
        _currentRoom.Visited = true;

        InitContext();
        // Restore floor history from saved state
        foreach (var (floor, room) in state.FloorHistory)
            _context.FloorHistory[floor] = room;
        _context.FloorEntranceRoom = state.FloorEntranceRoom ?? state.CurrentRoom;
        RunLoop();
    }

    private void InitContext()
    {
        _context = new CommandContext
        {
            Player              = _player,
            CurrentRoom         = _currentRoom,
            Rng                 = _rng,
            Stats               = _stats,
            SessionStats        = _sessionStats,
            RunStart            = _runStart,
            Display             = _display,
            Combat              = _combat,
            Equipment           = _equipment,
            InventoryManager    = _inventoryManager,
            Narration           = _narration,
            Achievements        = _achievements,
            AllItems            = _allItems,
            Difficulty          = _difficulty,
            DifficultyLevel     = _difficultyLevel,
            Logger              = _logger,
            Events              = _events,
            Navigator           = _navigator,
            Seed                = _seed,
            CurrentFloor        = _currentFloor,
            TurnConsumed        = true,
            GameOver            = false,
            ExitRun             = cause => ExitRun(cause),
            RecordRunEnd        = (won, ov) => RecordRunEnd(won, ov),
            GetCurrentlyEquippedForItem = (player, item) => GetCurrentlyEquippedForItem(player, item),
            GetDifficultyName   = () => GetDifficultyName(),
            HandleShrine        = () => { _currentRoom = _context.CurrentRoom; HandleShrine(); },
            HandleContestedArmory = () => { _currentRoom = _context.CurrentRoom; HandleContestedArmory(); },
            HandlePetrifiedLibrary = () => { _currentRoom = _context.CurrentRoom; HandlePetrifiedLibrary(); },
            HandleTrapRoom      = () => { _currentRoom = _context.CurrentRoom; HandleTrapRoom(); },
        };

        _combat.DungeonFloor = _currentFloor;
    }

    private void RunLoop()
    {
        while (true)
        {
            _display.ShowCommandPrompt(_player);
            var input = _display.ReadCommandInput() ?? _input.ReadLine() ?? string.Empty;
            var cmd = CommandParser.Parse(input);
            _context.TurnConsumed = true;

            if (cmd.Type == CommandType.Quit)
            {
                _stats.FinalLevel = _player.Level;
                _stats.TimeElapsed = DateTime.UtcNow - _runStart;
                RecordRunEnd(won: false, outcomeOverride: "Quit");
                _display.ShowMessage("Thanks for playing!");
                Cleanup();
                return;
            }

            if (_handlers.TryGetValue(cmd.Type, out var handler))
                handler.Handle(cmd.Argument, _context);
            else if (cmd.Type == CommandType.Unknown)
                _display.ShowError("Unknown command. Type HELP for commands.");
            else
                _logger.LogWarning("No handler registered for command type {CommandType}", cmd.Type);

            // Sync back mutable context state to GameLoop fields
            _player        = _context.Player;
            _currentRoom   = _context.CurrentRoom;
            _currentFloor  = _context.CurrentFloor;
            _combat.DungeonFloor = _currentFloor;
            _seed          = _context.Seed;
            _runStart      = _context.RunStart;
            _rng           = _context.Rng;
            _stats         = _context.Stats;
            _sessionStats  = _context.SessionStats;
            _turnConsumed  = _context.TurnConsumed;
            _gameOver      = _context.GameOver;

            if (_turnConsumed) _stats.TurnsTaken++;
            if (_turnConsumed && !_gameOver) ApplyRoomHazard(_currentRoom, _player);
            if (_player.HP <= 0 && !_gameOver)
                ExitRun("environmental hazard");
            if (_gameOver) break;
        }

        Cleanup();
    }

    /// <summary>
    /// Performs end-of-run cleanup: clears event subscriptions and logs the session end.
    /// Called before every exit path in <see cref="RunLoop"/>.
    /// </summary>
    private void Cleanup()
    {
        _events?.ClearAll();
        _logger.LogInformation("Game loop cleanup complete — session ended");
    }

    private void ApplyRoomHazard(Room room, Player player)
    {
        switch (room.EnvironmentalHazard)
        {
            case RoomHazard.LavaSeam:
                player.TakeDamage(5);
                _stats.DamageTaken += 5;
                _display.ShowMessage("🔥 The lava seam sears you. (-5 HP)");
                _display.ShowPlayerStats(_player);
                break;
            case RoomHazard.CorruptedGround:
                player.TakeDamage(3);
                _stats.DamageTaken += 3;
                _display.ShowMessage("💀 The corrupted ground drains you. (-3 HP)");
                _display.ShowPlayerStats(_player);
                break;
            case RoomHazard.BlessedClearing:
                if (!room.BlessedHealApplied)
                {
                    room.BlessedHealApplied = true;
                    player.Heal(3);
                    _display.ShowMessage("✨ A blessed warmth flows through you. (+3 HP)");
                    _display.ShowPlayerStats(_player);
                }
                break;
        }
    }

    /// <summary>
    /// Returns the currently equipped item in the slot that <paramref name="item"/> would occupy.
    /// Logic mirrors Systems/EquipmentManager.DoEquip slot resolution.
    /// </summary>
    private Item? GetCurrentlyEquippedForItem(Player player, Item? item)
    {
        return item?.Type switch
        {
            ItemType.Weapon    => player.EquippedWeapon,
            ItemType.Armor     => player.GetArmorSlotItem(item.Slot == ArmorSlot.None ? ArmorSlot.Chest : item.Slot),
            ItemType.Accessory => player.EquippedAccessory,
            _                  => null
        };
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
            _display.ShowRoom(_currentRoom);
            return;
        }
        if (_currentRoom.ShrineUsed)
        {
            _display.ShowMessage("The shrine has already been used.");
            _display.ShowRoom(_currentRoom);
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
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
            }
            _player.SacredGroundActive = false;
        }

        // Scale shrine costs inversely by HealingMultiplier (higher healing = cheaper shrines)
        var shrineHealCost = Math.Max(5, (int)(30 / _difficulty.HealingMultiplier));
        var shrineBlessCost = Math.Max(10, (int)(50 / _difficulty.HealingMultiplier));
        var shrineMaxHPCost = Math.Max(15, (int)(75 / _difficulty.HealingMultiplier));
        var shrineMeditateCost = Math.Max(15, (int)(75 / _difficulty.HealingMultiplier));

        var choice = _display.ShowShrineMenuAndSelect(_player.Gold, shrineHealCost, shrineBlessCost, shrineMaxHPCost, shrineMeditateCost);
        switch (choice)
        {
            case 1: // Heal fully
                if (_player.Gold < shrineHealCost) { _display.ShowError($"Not enough gold (need {shrineHealCost}g)."); _display.ShowRoom(_currentRoom); return; }
                _player.SpendGold(shrineHealCost);
                _player.Heal(_player.MaxHP);
                _display.ShowMessage($"The shrine heals you fully! HP: {_player.HP}/{_player.MaxHP}");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantHeal));
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
                _display.ShowRoom(_currentRoom);
                break;
            case 2: // Bless
                if (_player.Gold < shrineBlessCost) { _display.ShowError($"Not enough gold (need {shrineBlessCost}g)."); _display.ShowRoom(_currentRoom); return; }
                _player.SpendGold(shrineBlessCost);
                _player.ModifyAttack(2);
                _player.ModifyDefense(2);
                _display.ShowMessage("The shrine blesses you! +2 ATK/DEF.");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantPower));
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
                _display.ShowRoom(_currentRoom);
                break;
            case 3: // Fortify
                if (_player.Gold < shrineMaxHPCost) { _display.ShowError($"Not enough gold (need {shrineMaxHPCost}g)."); _display.ShowRoom(_currentRoom); return; }
                _player.SpendGold(shrineMaxHPCost);
                _player.FortifyMaxHP(10);
                _display.ShowMessage($"The shrine fortifies you! MaxHP permanently +10. ({_player.MaxHP} MaxHP)");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantProtection));
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
                _display.ShowRoom(_currentRoom);
                break;
            case 4: // Meditate
                if (_player.Gold < shrineMeditateCost) { _display.ShowError($"Not enough gold (need {shrineMeditateCost}g)."); _display.ShowRoom(_currentRoom); return; }
                _player.SpendGold(shrineMeditateCost);
                _player.FortifyMaxMana(10);
                _display.ShowMessage($"The shrine expands your mind! MaxMana permanently +10. ({_player.MaxMana} MaxMana)");
                _currentRoom.ShrineUsed = true;
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantWisdom));
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
                _display.ShowRoom(_currentRoom);
                break;
            case 0: // Leave
                _display.ShowMessage("You leave the shrine.");
                _display.ShowMessage(_narration.Pick(Systems.ShrineNarration.GrantNothing));
                _display.ShowRoom(_currentRoom);
                break;
        }
    }

    private void HandleForgottenShrine()
    {
        var choice = _display.ShowForgottenShrineMenuAndSelect();
        switch (choice)
        {
            case 1: // Prayer of Restoration — full heal + cure all ailments
                _player.Heal(_player.MaxHP);
                _player.ActiveEffects.RemoveAll(e => e.IsDebuff);
                _display.ShowMessage($"Divine light washes over you. Fully restored! HP: {_player.HP}/{_player.MaxHP}. All ailments cured.");
                _currentRoom.SpecialRoomUsed = true;
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
                _display.ShowRoom(_currentRoom);
                break;
            case 2: // Prayer of Might — permanent +3 ATK
                _player.ModifyAttack(3);
                _display.ShowMessage("The shrine imbues your strikes with holy fury. Attack +3 (permanent).");
                _currentRoom.SpecialRoomUsed = true;
                _display.ShowRoom(_currentRoom);
                break;
            case 3: // Prayer of Resilience — permanent +3 DEF
                _player.ModifyDefense(3);
                _display.ShowMessage("Sacred stone hardens your body. Defense +3 (permanent).");
                _currentRoom.SpecialRoomUsed = true;
                _display.ShowRoom(_currentRoom);
                break;
            case 0:
                _display.ShowMessage("You leave the forgotten shrine.");
                _display.ShowRoom(_currentRoom);
                break;
        }
    }

    private void HandlePetrifiedLibrary()
    {
        var roll = _rng.Next(3);
        switch (roll)
        {
            case 0:
                _player.FortifyMaxHP(10);
                _display.ShowMessage("📜 Scroll of Fortitude: Ancient runes fortify your body. MaxHP +10!");
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
                break;
            case 1:
                _player.AddXP(15);
                _display.ShowMessage("📖 Tome of the Archivist: Forbidden knowledge floods your mind. +15 XP!");
                _display.RefreshDisplay(_player, _currentRoom, _currentFloor);
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
            _display.ShowRoom(_currentRoom);
            return;
        }
        if (_currentRoom.SpecialRoomUsed)
        {
            _display.ShowMessage("The armory has already been looted.");
            _display.ShowRoom(_currentRoom);
            return;
        }

        var armoryChoice = _display.ShowContestedArmoryMenuAndSelect(_player.Defense);
        switch (armoryChoice)
        {
            case 1:
                if (_player.Defense > 12)
                {
                    _display.ShowMessage("You carefully disarm the traps and claim a fine weapon.");
                    GiveArmoryLoot(uncommon: false);
                    _currentRoom.SpecialRoomUsed = true;
                    _display.ShowRoom(_currentRoom);
                }
                else
                {
                    _display.ShowMessage("You lack the fortitude to safely disarm the traps. Try a reckless grab or leave.");
                    _display.ShowRoom(_currentRoom);
                }
                break;
            case 2:
                var dmg = _rng.Next(15, 31);
                _player.TakeDamage(dmg);
                _stats.DamageTaken += dmg;
                _display.ShowMessage($"Blades nick you as you grab the weapon! -{dmg} HP. HP: {_player.HP}/{_player.MaxHP}");
                GiveArmoryLoot(uncommon: true);
                _currentRoom.SpecialRoomUsed = true;
                if (_player.HP <= 0)
                    ExitRun("an armory trap");
                else
                    _display.ShowRoom(_currentRoom);
                break;
            case 0:
                _display.ShowMessage("You leave the armory untouched.");
                _display.ShowRoom(_currentRoom);
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
        var rng = _rng;

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
                var avChoice = _display.ShowTrapChoiceAndSelect(
                    "⚠ [Trap Room: Arrow Volley]",
                    "Raise your shield — 70% chance to block (no damage); 30% chance to take 15 damage",
                    "Sprint through  — take 8 damage, but find a loot cache");
                if (avChoice == 1)
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
                        if (_player.HP <= 0) { ExitRun("a dungeon trap"); return; }
                    }
                    GiveTrapLoot(rng.NextDouble() < 0.5 ? Models.ItemTier.Uncommon : Models.ItemTier.Common,
                        "You spot a small cache behind the arrow slits.");
                    _display.ShowRoom(_currentRoom);
                }
                else if (avChoice == 2)
                {
                    _player.TakeDamage(8);
                    _stats.DamageTaken += 8;
                    _display.ShowMessage($"You sprint through, arrows grazing your side! -8 HP. HP: {_player.HP}/{_player.MaxHP}");
                    if (_player.HP <= 0) { ExitRun("a dungeon trap"); return; }
                    GiveTrapLoot(Models.ItemTier.Uncommon, "Your speed reveals a hidden loot cache!");
                    _display.ShowRoom(_currentRoom);
                }
                else
                {
                    _display.ShowMessage("You hesitate — the trap remains.");
                    _currentRoom.SpecialRoomUsed = false;
                    _display.ShowRoom(_currentRoom);
                    return;
                }
                break;

            case TrapVariant.PoisonGas:
                var pgChoice = _display.ShowTrapChoiceAndSelect(
                    "⚠ [Trap Room: Poison Gas]",
                    "Hold your breath and sprint — 60% escape unharmed; 40% get Poisoned (3 turns)",
                    "Find a bypass route    — always safe; 80% chance to find an Uncommon item");
                if (pgChoice == 1)
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
                    _display.ShowRoom(_currentRoom);
                }
                else if (pgChoice == 2)
                {
                    _display.ShowMessage("You take the long way round, tracing the walls for a safe passage.");
                    if (rng.NextDouble() < 0.80)
                        GiveTrapLoot(Models.ItemTier.Uncommon, "Your careful detour leads you past a forgotten cache.");
                    else
                        _display.ShowMessage("The bypass yields nothing but cobwebs.");
                    _display.ShowRoom(_currentRoom);
                }
                else
                {
                    _display.ShowMessage("You hesitate — the gas continues to seep.");
                    _currentRoom.SpecialRoomUsed = false;
                    _display.ShowRoom(_currentRoom);
                    return;
                }
                break;

            case TrapVariant.CollapsingFloor:
                var cfChoice = _display.ShowTrapChoiceAndSelect(
                    "⚠ [Trap Room: Collapsing Floor]",
                    "Leap across quickly     — 75% cross safely and find a Rare item; 25% take 20 damage",
                    "Cross carefully, test each step — 100% safe, no loot, slow");
                if (cfChoice == 1)
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
                        if (_player.HP <= 0) { ExitRun("a dungeon trap"); return; }
                    }
                    _display.ShowRoom(_currentRoom);
                }
                else if (cfChoice == 2)
                {
                    _display.ShowMessage("Inch by inch you test every stone. It takes an age, but you reach the other side safely.");
                    _display.ShowRoom(_currentRoom);
                }
                else
                {
                    _display.ShowMessage("You hesitate — the floor creaks ominously.");
                    _currentRoom.SpecialRoomUsed = false;
                    _display.ShowRoom(_currentRoom);
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

    /// <summary>
    /// Records the end of a run: persists to history, updates prestige, and (on win)
    /// evaluates and displays any newly unlocked achievements. Must be called after
    /// <see cref="RunStats.FinalLevel"/> and <see cref="RunStats.TimeElapsed"/> are set.
    /// </summary>
    private void RecordRunEnd(bool won, string? outcomeOverride = null)
    {
        RunStats.AppendToHistory(_stats, won);
        _sessionStats.FloorsCleared = _currentFloor;
        _sessionStats.DamageDealt = _stats.DamageDealt;
        var outcome = outcomeOverride ?? (won ? "Victory" : "Defeat");
        SessionLogger.LogBalanceSummary(_logger, _sessionStats, outcome);
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
    /// Single chokepoint for all player-death paths. Records stats, shows the game-over screen,
    /// and sets <see cref="_gameOver"/>. Every death path must call this instead of duplicating the sequence.
    /// </summary>
    /// <param name="killedBy">Name of the enemy or hazard that killed the player.</param>
    private void ExitRun(string killedBy)
    {
        _stats.FinalLevel = _player.Level;
        _stats.TimeElapsed = DateTime.UtcNow - _runStart;
        _display.ShowMessage($"Difficulty: {GetDifficultyName()}");
        if (_seed.HasValue) _display.ShowMessage($"Run seed: {_seed.Value}");
        _stats.Display(_display.ShowMessage);
        ShowGameOver(killedBy: killedBy);
        RecordRunEnd(won: false);
        _gameOver = true;
        if (_context is not null) _context.GameOver = true;
    }

    /// <summary>
    /// Delegates to <see cref="IDisplayService.ShowGameOver"/> to render the death screen.
    /// </summary>
    /// <param name="killedBy">Name of the enemy that killed the player, or <see langword="null"/> for non-combat deaths.</param>
    /// <param name="byTrap">
    /// <see langword="true"/> when the player was killed by an environmental hazard rather than a monster.
    /// </param>
    private void ShowGameOver(string? killedBy = null, bool byTrap = false)
    {
        _display.ShowGameOver(_player, killedBy, _stats);
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
