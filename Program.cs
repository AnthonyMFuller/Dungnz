using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding  = System.Text.Encoding.UTF8;

var prestige = PrestigeSystem.Load();
var inputReader = new ConsoleInputReader();
var navigator = new ConsoleMenuNavigator();
var display = new ConsoleDisplayService(inputReader, navigator);

var intro = new IntroSequence(display, inputReader);
var (player, actualSeed, chosenDifficulty) = intro.Run(prestige);

var difficultySettings = DifficultySettings.For(chosenDifficulty);
display.ShowMessage($"Run #{prestige.TotalRuns + 1} — Seed: {actualSeed} (share to replay)");

EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
StartupValidator.ValidateOrThrow();
CraftingSystem.Load("Data/crafting-recipes.json");
AffixRegistry.Load("Data/item-affixes.json");
var allItems = ItemConfig.Load("Data/item-stats.json").Select(ItemConfig.CreateItem).ToList();
var generator = new DungeonGenerator(actualSeed, allItems);
var (startRoom, _) = generator.Generate(difficulty: difficultySettings);

var combat = new CombatEngine(display, inputReader, navigator: navigator, difficulty: difficultySettings);
var gameLoop = new GameLoop(display, combat, inputReader, seed: actualSeed, difficulty: difficultySettings, allItems: allItems, navigator: navigator);
gameLoop.Run(player, startRoom);
