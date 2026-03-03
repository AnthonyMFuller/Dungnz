using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests.Engine;

/// <summary>
/// Tests for StartupOrchestrator: menu flow, cancellation, save loading, and error handling.
/// </summary>
[Collection("save-system")]
public class StartupOrchestratorTests : IDisposable
{
    private readonly string _testSaveDir;

    public StartupOrchestratorTests()
    {
        // Isolate test save files
        _testSaveDir = Path.Combine(Path.GetTempPath(), "DungnzTests_StartupOrchestrator_" + Guid.NewGuid());
        Directory.CreateDirectory(_testSaveDir);
        SaveSystem.OverrideSaveDirectory(_testSaveDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testSaveDir))
            Directory.Delete(_testSaveDir, recursive: true);
    }

    [Fact]
    public void Run_WhenPlayerSelectsExit_ReturnsExitGame()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.Exit);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        var result = orchestrator.Run();

        result.Should().BeOfType<StartupResult.ExitGame>();
        display.ShowEnhancedTitleCalled.Should().BeTrue();
    }

    [Fact]
    public void Run_WhenPlayerSelectsNewGame_ReturnsNewGameWithPlayerAndSeed()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.NewGame);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        var result = orchestrator.Run();

        result.Should().BeOfType<StartupResult.NewGame>();
        var newGame = (StartupResult.NewGame)result;
        newGame.Player.Should().NotBeNull();
        newGame.Player.Name.Should().Be("TestPlayer");
        newGame.Seed.Should().BeInRange(100000, 999999);
        newGame.Difficulty.Should().Be(Difficulty.Normal);
    }

    [Fact]
    public void Run_WhenPlayerSelectsNewGameWithSeed_ReturnsNewGameWithSpecifiedSeed()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.NewGameWithSeed);
        display.EnqueueSeedInput(123456);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        var result = orchestrator.Run();

        result.Should().BeOfType<StartupResult.NewGame>();
        var newGame = (StartupResult.NewGame)result;
        newGame.Seed.Should().Be(123456);
        newGame.Player.Should().NotBeNull();
    }

    [Fact]
    public void Run_WhenSeedInputIsCancelled_ReShowsMenuAndExitsOnSecondChoice()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.NewGameWithSeed);
        display.EnqueueSeedInput(null); // Cancel seed input
        display.EnqueueMenuChoice(StartupMenuOption.Exit);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        var result = orchestrator.Run();

        result.Should().BeOfType<StartupResult.ExitGame>();
        display.MenuCallCount.Should().Be(2, "menu should be shown twice: once before seed cancel, once after");
    }

    [Fact]
    public void Run_WhenPlayerSelectsLoadSave_ReturnsLoadedGameWithCorrectState()
    {
        // Arrange: Create a test save file
        var player = new Player { Name = "SavedPlayer", HP = 50, MaxHP = 100, Attack = 10, Level = 5 };
        var room = new Room { Description = "Test Room", IsExit = false };
        var state = new GameState(player, room, currentFloor: 3, seed: 999888);
        SaveSystem.SaveGame(state, "testsave");

        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.LoadSave);
        display.EnqueueSaveSelection("testsave");
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        // Act
        var result = orchestrator.Run();

        // Assert
        result.Should().BeOfType<StartupResult.LoadedGame>();
        var loadedGame = (StartupResult.LoadedGame)result;
        loadedGame.State.Should().NotBeNull();
        loadedGame.State.Player.Name.Should().Be("SavedPlayer");
        loadedGame.State.Player.Level.Should().Be(5);
        loadedGame.State.CurrentFloor.Should().Be(3);
        loadedGame.State.Seed.Should().Be(999888);
    }

    [Fact]
    public void Run_WhenLoadSaveIsCancelled_ReShowsMenuAndExitsOnSecondChoice()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.LoadSave);
        display.EnqueueSaveSelection(null); // Cancel save selection
        display.EnqueueMenuChoice(StartupMenuOption.Exit);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        var result = orchestrator.Run();

        result.Should().BeOfType<StartupResult.ExitGame>();
        display.MenuCallCount.Should().Be(2, "menu should be shown twice: once before cancel, once after");
    }

    [Fact]
    public void Run_WhenLoadSaveFileDoesNotExist_ShowsErrorAndReShowsMenu()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.LoadSave);
        display.EnqueueSaveSelection("nonexistent_save");
        display.EnqueueMenuChoice(StartupMenuOption.Exit);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        var result = orchestrator.Run();

        result.Should().BeOfType<StartupResult.ExitGame>();
        display.Errors.Should().HaveCountGreaterThan(0, "error should be shown when save file not found");
        display.Errors[0].Should().Contain("not found");
        display.MenuCallCount.Should().Be(2, "menu should be shown again after error");
    }

    [Fact]
    public void Run_WhenNoSavesExist_CallsShowStartupMenuWithHasSavesFalse()
    {
        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.Exit);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        orchestrator.Run();

        display.HasSavesFlags.Should().ContainSingle().Which.Should().BeFalse("hasSaves should be false when no saves exist");
    }

    [Fact]
    public void Run_WhenSavesExist_CallsShowStartupMenuWithHasSavesTrue()
    {
        // Create a save file
        var player = new Player { Name = "TestPlayer" };
        var room = new Room { Description = "Test", IsExit = false };
        var state = new GameState(player, room);
        SaveSystem.SaveGame(state, "existingsave");

        var display = new StartupTestDisplayService();
        display.EnqueueMenuChoice(StartupMenuOption.Exit);
        var input = new FakeInputReader();
        var prestige = new PrestigeData();
        var orchestrator = new StartupOrchestrator(display, input, prestige);

        orchestrator.Run();

        display.HasSavesFlags.Should().ContainSingle().Which.Should().BeTrue("hasSaves should be true when saves exist");
    }

    /// <summary>
    /// Test-specific display service that allows queueing controlled return values
    /// for startup menu methods.
    /// </summary>
    private class StartupTestDisplayService : TestDisplayService
    {
        private readonly Queue<StartupMenuOption> _menuChoices = new();
        private readonly Queue<string?> _saveSelections = new();
        private readonly Queue<int?> _seedInputs = new();

        public int MenuCallCount { get; private set; }
        public List<bool> HasSavesFlags { get; } = new();

        public void EnqueueMenuChoice(StartupMenuOption choice) => _menuChoices.Enqueue(choice);
        public void EnqueueSaveSelection(string? saveName) => _saveSelections.Enqueue(saveName);
        public void EnqueueSeedInput(int? seed) => _seedInputs.Enqueue(seed);

        public override StartupMenuOption ShowStartupMenu(bool hasSaves)
        {
            MenuCallCount++;
            HasSavesFlags.Add(hasSaves);
            return _menuChoices.Count > 0 
                ? _menuChoices.Dequeue() 
                : StartupMenuOption.Exit;
        }

        public override string? SelectSaveToLoad(string[] saveNames)
        {
            return _saveSelections.Count > 0 
                ? _saveSelections.Dequeue() 
                : null;
        }

        public override int? ReadSeed()
        {
            return _seedInputs.Count > 0 
                ? _seedInputs.Dequeue() 
                : null;
        }
    }
}
