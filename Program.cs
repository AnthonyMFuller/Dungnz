using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

var display = new ConsoleDisplayService();
display.ShowTitle();

var prestige = PrestigeSystem.Load();
if (prestige.PrestigeLevel > 0)
{
    Console.WriteLine(PrestigeSystem.GetPrestigeDisplay(prestige));
}

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

// Class selection
display.ShowMessage("Choose your class:");
display.ShowMessage("[1] Warrior - High HP, defense, and attack bonus. Reduced mana.");
display.ShowMessage("[2] Mage - High mana and powerful spells. Reduced HP and defense.");
display.ShowMessage("[3] Rogue - Balanced with an attack bonus. Extra dodge chance.");
display.ShowCommandPrompt();
var classInput = Console.ReadLine()?.Trim() ?? "";
var chosenClassDef = classInput switch
{
    "2" => PlayerClassDefinition.Mage,
    "3" => PlayerClassDefinition.Rogue,
    _   => PlayerClassDefinition.Warrior
};
display.ShowMessage($"Class: {chosenClassDef.Name}");

var player = new Player { Name = name };
player.Class = chosenClassDef.Class;
player.Attack += chosenClassDef.BonusAttack;
player.Defense = Math.Max(0, player.Defense + chosenClassDef.BonusDefense);
player.MaxHP = Math.Max(1, player.MaxHP + chosenClassDef.BonusMaxHP);
player.HP = player.MaxHP;
player.MaxMana = Math.Max(0, player.MaxMana + chosenClassDef.BonusMaxMana);
player.Mana = player.MaxMana;
if (chosenClassDef.Class == PlayerClass.Rogue)
    player.ClassDodgeBonus = 0.10f;
EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");

// Apply prestige bonuses to starting stats
if (prestige.PrestigeLevel > 0)
{
    player.Attack += prestige.BonusStartAttack;
    player.Defense += prestige.BonusStartDefense;
    player.MaxHP += prestige.BonusStartHP;
    player.HP = player.MaxHP;
}
var generator = new DungeonGenerator(actualSeed);
var (startRoom, _) = generator.Generate(difficulty: difficultySettings);

var inputReader = new ConsoleInputReader();
var combat = new CombatEngine(display, inputReader);
var gameLoop = new GameLoop(display, combat, inputReader, seed: actualSeed, difficulty: difficultySettings);
gameLoop.Run(player, startRoom);
