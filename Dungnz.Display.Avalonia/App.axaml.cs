using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Display.Avalonia.Views;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dungnz.Display.Avalonia;

/// <summary>
/// Avalonia application entry point.
/// </summary>
public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            
            // Load prestige
            var prestige = PrestigeSystem.Load();
            
            // Create main window and ViewModel
            var mainVM = new MainWindowViewModel();
            var displayService = new AvaloniaDisplayService(mainVM);
            
            var mainWindow = new MainWindow
            {
                DataContext = mainVM
            };
            desktop.MainWindow = mainWindow;
            
            // Start game loop on background thread after window is shown
            mainWindow.Opened += async (s, e) =>
            {
                // TODO(P3-P8): Full startup flow (StartupOrchestrator, SelectClass, SelectDifficulty)
                // For P2: launch with default player for smoke test
                
                var defaultDiff = DifficultySettings.For(Difficulty.Normal);
                
                EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
                StartupValidator.ValidateOrThrow();
                CraftingSystem.Load("Data/crafting-recipes.json");
                AffixRegistry.Load("Data/item-affixes.json");
                StatusEffectRegistry.Load("Data/status-effects.json");
                var allItems = ItemConfig.Load("Data/item-stats.json")
                    .Select(ItemConfig.CreateItem).ToList();
                
                var generator = new DungeonGenerator(seed: 12345, allItems);
                var (startRoom, _) = generator.Generate(difficulty: defaultDiff);
                
                var player = new Player { Name = "Adventurer" };
                player.Class = PlayerClass.Warrior;
                player.SetHPDirect(player.MaxHP);
                
                // Bridge Avalonia TextBox input to the game thread
                var inputReader = new AvaloniaInputReader(mainVM.Input);
                var combat = new CombatEngine(displayService, inputReader, difficulty: defaultDiff);
                var gameLoop = new GameLoop(displayService, combat, inputReader,
                    seed: 12345, difficulty: defaultDiff, allItems: allItems,
                    logger: loggerFactory.CreateLogger<GameLoop>());
                
                // Run game on background thread
                await Task.Run(() => gameLoop.Run(player, startRoom));
                
                // Game ended — close window
                mainWindow.Close();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
