using Dungnz.Display;
using Dungnz.Display.Spectre;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;
using Serilog;

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

// Start Spectre Live rendering loop on a background task
var displayService = new SpectreLayoutDisplayService();
_ = displayService.StartAsync();

// Give the Live loop time to initialize before the game starts
await Task.Delay(200);

var inputReader = new ConsoleInputReader();
IDisplayService display = displayService;

var startup = new StartupOrchestrator(display, inputReader, prestige);
var result = startup.Run();

if (result is StartupResult.ExitGame)
{
    displayService.StopLive();
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

displayService.StopLive();

