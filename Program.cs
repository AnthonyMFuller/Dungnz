using TextGame.Display;
using TextGame.Engine;
using TextGame.Models;

var display = new DisplayService();
display.ShowTitle();

var name = display.ReadPlayerName();

var player = new Player { Name = name };
var generator = new DungeonGenerator();
var (startRoom, _) = generator.Generate();

var combat = new CombatEngine(display);
var gameLoop = new GameLoop(display, combat);
gameLoop.Run(player, startRoom);
