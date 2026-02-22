using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

var prestige = PrestigeSystem.Load();
var inputReader = new ConsoleInputReader();
var display = new ConsoleDisplayService();

var intro = new IntroSequence(display, inputReader);
var (player, actualSeed, chosenDifficulty) = intro.Run(prestige);

var difficultySettings = DifficultySettings.For(chosenDifficulty);
display.ShowMessage($"Run #{prestige.TotalRuns + 1} — Seed: {actualSeed} (share to replay)");

EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
var generator = new DungeonGenerator(actualSeed);
var (startRoom, _) = generator.Generate(difficulty: difficultySettings);

var combat = new CombatEngine(display, inputReader);
var gameLoop = new GameLoop(display, combat, inputReader, seed: actualSeed, difficulty: difficultySettings);
gameLoop.Run(player, startRoom);
