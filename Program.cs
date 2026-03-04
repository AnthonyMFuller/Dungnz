using Dungnz.Display;
using Dungnz.Display.Tui;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;
using Serilog;
using Terminal.Gui;

bool useTui = args.Contains("--tui");

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding  = System.Text.Encoding.UTF8;

// Create log directory
var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dungnz", "Logs");
Directory.CreateDirectory(logDir);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(Path.Combine(logDir, "dungnz-.log"), rollingInterval: Serilog.RollingInterval.Day)
    .CreateLogger();

// Create logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Dungnz starting...");

var prestige = PrestigeSystem.Load();

if (useTui)
{
    // Terminal.Gui path
    Application.Init();
    
    var layout = new TuiLayout();
    var bridge = new GameThreadBridge();
    var inputReader = new TerminalGuiInputReader(bridge);
    IDisplayService display = new TerminalGuiDisplayService(layout);
    
    var startup = new StartupOrchestrator(display, inputReader, prestige);
    
    // Wire Ctrl+Q to quit
    layout.MainWindow.KeyPress += (e) =>
    {
        if (e.KeyEvent.Key == (Key.CtrlMask | Key.Q))
        {
            bridge.Complete();
            Application.RequestStop();
            e.Handled = true;
        }
    };
    
    // Wire Enter key in command input
    layout.CommandInput.KeyPress += (e) =>
    {
        if (e.KeyEvent.Key == Key.Enter)
        {
            var cmd = layout.CommandInput.Text.ToString();
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                bridge.PostCommand(cmd);
                layout.CommandInput.Text = string.Empty;
            }
            e.Handled = true;
        }
    };
    
    // Start game loop on background thread
    _ = Task.Run(() =>
    {
        try
        {
            var result = startup.Run();
            
            if (result is StartupResult.ExitGame)
            {
                Application.RequestStop();
                return;
            }
            
            // Initialize data
            EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
            StartupValidator.ValidateOrThrow();
            CraftingSystem.Load("Data/crafting-recipes.json");
            AffixRegistry.Load("Data/item-affixes.json");
            StatusEffectRegistry.Load("Data/status-effects.json");
            var allItems = ItemConfig.Load("Data/item-stats.json").Select(ItemConfig.CreateItem).ToList();
            
            switch (result)
            {
                case StartupResult.NewGame ng:
                {
                    var difficultySettings = DifficultySettings.For(ng.Difficulty);
                    display.ShowMessage($"Run #{prestige.TotalRuns + 1} — Seed: {ng.Seed} (share to replay)");
                    
                    var generator = new DungeonGenerator(ng.Seed, allItems);
                    var (startRoom, _) = generator.Generate(difficulty: difficultySettings);
                    
                    var combat = new CombatEngine(display, inputReader, difficulty: difficultySettings);
                    var gameLoop = new GameLoop(display, combat, inputReader, seed: ng.Seed,
                        difficulty: difficultySettings, allItems: allItems,
                        logger: loggerFactory.CreateLogger<GameLoop>());
                    gameLoop.Run(ng.Player, startRoom);
                    break;
                }
                
                case StartupResult.LoadedGame lg:
                {
                    var difficultySettings = DifficultySettings.For(lg.State.Difficulty);
                    var combat = new CombatEngine(display, inputReader, difficulty: difficultySettings);
                    var gameLoop = new GameLoop(display, combat, inputReader, seed: lg.State.Seed,
                        difficulty: difficultySettings, allItems: allItems,
                        logger: loggerFactory.CreateLogger<GameLoop>());
                    gameLoop.Run(lg.State);
                    break;
                }
            }
        }
        finally
        {
            // Game ended, stop the UI
            Application.RequestStop();
        }
    });
    
    // Run Terminal.Gui on main thread (blocks until RequestStop)
    Application.Run(layout.MainWindow);
    Application.Shutdown();
}
else
{
    // Spectre.Console path (unchanged)
    var inputReader = new ConsoleInputReader();
    IDisplayService display = new SpectreDisplayService();
    
    var startup = new StartupOrchestrator(display, inputReader, prestige);
    var result = startup.Run();
    
    if (result is StartupResult.ExitGame)
        return;
    
    // Initialize data (runs for all non-exit paths)
    EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
    StartupValidator.ValidateOrThrow();
    CraftingSystem.Load("Data/crafting-recipes.json");
    AffixRegistry.Load("Data/item-affixes.json");
    StatusEffectRegistry.Load("Data/status-effects.json");
    var allItems = ItemConfig.Load("Data/item-stats.json").Select(ItemConfig.CreateItem).ToList();
    
    switch (result)
    {
        case StartupResult.NewGame ng:
        {
            var difficultySettings = DifficultySettings.For(ng.Difficulty);
            display.ShowMessage($"Run #{prestige.TotalRuns + 1} — Seed: {ng.Seed} (share to replay)");
            
            var generator = new DungeonGenerator(ng.Seed, allItems);
            var (startRoom, _) = generator.Generate(difficulty: difficultySettings);
            
            var combat = new CombatEngine(display, inputReader, difficulty: difficultySettings);
            var gameLoop = new GameLoop(display, combat, inputReader, seed: ng.Seed,
                difficulty: difficultySettings, allItems: allItems,
                logger: loggerFactory.CreateLogger<GameLoop>());
            gameLoop.Run(ng.Player, startRoom);
            break;
        }
        
        case StartupResult.LoadedGame lg:
        {
            var difficultySettings = DifficultySettings.For(lg.State.Difficulty);
            var combat = new CombatEngine(display, inputReader, difficulty: difficultySettings);
            var gameLoop = new GameLoop(display, combat, inputReader, seed: lg.State.Seed,
                difficulty: difficultySettings, allItems: allItems,
                logger: loggerFactory.CreateLogger<GameLoop>());
            gameLoop.Run(lg.State);
            break;
        }
    }
}
