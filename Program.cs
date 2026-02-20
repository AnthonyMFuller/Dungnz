using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;

var display = new ConsoleDisplayService();
display.ShowTitle();

var name = display.ReadPlayerName();

var player = new Player { Name = name };
var generator = new DungeonGenerator();
var (startRoom, _) = generator.Generate();

var inputReader = new ConsoleInputReader();
var combat = new CombatEngine(display, inputReader);
var gameLoop = new GameLoop(display, combat, inputReader);
gameLoop.Run(player, startRoom);
