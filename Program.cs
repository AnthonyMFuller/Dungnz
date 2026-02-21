using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;

var display = new ConsoleDisplayService();
display.ShowTitle();

var name = display.ReadPlayerName();

// Seed selection
display.ShowMessage("Enter a seed for reproducible runs (or press Enter for random):");
display.ShowCommandPrompt();
var seedInput = Console.ReadLine()?.Trim() ?? "";
int? seed = int.TryParse(seedInput, out var parsedSeed) ? parsedSeed : null;
var actualSeed = seed ?? new Random().Next(100000, 999999);
if (seed == null)
    display.ShowMessage($"Random seed: {actualSeed} (share this to replay the same run)");
else
    display.ShowMessage($"Using seed: {actualSeed}");

// Difficulty selection
display.ShowMessage("Choose difficulty: [1] Casual  [2] Normal  [3] Hard");
display.ShowCommandPrompt();
var diffInput = Console.ReadLine()?.Trim() ?? "";
var chosenDifficulty = diffInput switch
{
    "1" => Difficulty.Casual,
    "3" => Difficulty.Hard,
    _   => Difficulty.Normal
};
var difficultySettings = DifficultySettings.For(chosenDifficulty);
display.ShowMessage($"Difficulty: {chosenDifficulty}");

var player = new Player { Name = name };
EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
var generator = new DungeonGenerator(actualSeed);
var (startRoom, _) = generator.Generate(difficulty: difficultySettings);

var inputReader = new ConsoleInputReader();
var combat = new CombatEngine(display, inputReader);
var gameLoop = new GameLoop(display, combat, inputReader, seed: actualSeed, difficulty: difficultySettings);
gameLoop.Run(player, startRoom);
