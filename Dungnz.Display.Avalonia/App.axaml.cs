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
                await Task.Run(() =>
                {
                    // Bridge Avalonia TextBox input to the game thread
                    var inputReader = new AvaloniaInputReader(mainVM.Input);

                    // Run the startup orchestrator to get user choices
                    var startup = new StartupOrchestrator(displayService, inputReader, prestige);
                    var result = startup.Run();

                    if (result is StartupResult.ExitGame)
                    {
                        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => mainWindow.Close());
                        return;
                    }

                    // Initialize data systems
                    EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
                    StartupValidator.ValidateOrThrow();
                    CraftingSystem.Load("Data/crafting-recipes.json");
                    AffixRegistry.Load("Data/item-affixes.json");
                    StatusEffectRegistry.Load("Data/status-effects.json");
                    var allItems = ItemConfig.Load("Data/item-stats.json")
                        .Select(ItemConfig.CreateItem).ToList();

                    switch (result)
                    {
                        case StartupResult.NewGame ng:
                        {
                            var difficultySettings = DifficultySettings.For(ng.Difficulty);
                            displayService.ShowMessage(
                                $"Run #{prestige.TotalRuns + 1} — Seed: {ng.Seed} (share to replay)");

                            var generator = new DungeonGenerator(ng.Seed, allItems);
                            var (startRoom, _) = generator.Generate(difficulty: difficultySettings);

                            var combat = new CombatEngine(displayService, inputReader,
                                difficulty: difficultySettings);
                            var gameLoop = new GameLoop(displayService, combat, inputReader,
                                seed: ng.Seed, difficulty: difficultySettings, allItems: allItems,
                                logger: loggerFactory.CreateLogger<GameLoop>());

                            gameLoop.Run(ng.Player, startRoom);
                            break;
                        }

                        case StartupResult.LoadedGame lg:
                        {
                            var difficultySettings = DifficultySettings.For(lg.State.Difficulty);
                            var combat = new CombatEngine(displayService, inputReader,
                                difficulty: difficultySettings);
                            var gameLoop = new GameLoop(displayService, combat, inputReader,
                                seed: lg.State.Seed ?? 0, difficulty: difficultySettings,
                                allItems: allItems,
                                logger: loggerFactory.CreateLogger<GameLoop>());

                            gameLoop.Run(lg.State);
                            break;
                        }
                    }
                });

                // Game ended — close window
                mainWindow.Close();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
