using Dungnz.Display;
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
var inputReader = new ConsoleInputReader();
IDisplayService display = new SpectreDisplayService();

var intro = new IntroSequence(display, inputReader);
var (player, actualSeed, chosenDifficulty) = intro.Run(prestige);

var difficultySettings = DifficultySettings.For(chosenDifficulty);
display.ShowMessage($"Run #{prestige.TotalRuns + 1} — Seed: {actualSeed} (share to replay)");

EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
StartupValidator.ValidateOrThrow();
CraftingSystem.Load("Data/crafting-recipes.json");
AffixRegistry.Load("Data/item-affixes.json");
StatusEffectRegistry.Load("Data/status-effects.json");
var allItems = ItemConfig.Load("Data/item-stats.json").Select(ItemConfig.CreateItem).ToList();
var generator = new DungeonGenerator(actualSeed, allItems);
var (startRoom, _) = generator.Generate(difficulty: difficultySettings);

var combat = new CombatEngine(display, inputReader, difficulty: difficultySettings);
var gameLoop = new GameLoop(display, combat, inputReader, seed: actualSeed, difficulty: difficultySettings, allItems: allItems, logger: loggerFactory.CreateLogger<GameLoop>());
gameLoop.Run(player, startRoom);
