using Dungnz.Engine;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Dungnz.Tests;

/// <summary>
/// #1369 — Full coverage suite for <see cref="LoadCommandHandler"/>.
/// Each test uses a private temp save directory so the real AppData folder is never touched.
/// </summary>
[Collection("save-system")]
public class LoadCommandHandlerTests : IDisposable
{
    private readonly string _saveDir;

    public LoadCommandHandlerTests()
    {
        _saveDir = Path.Combine(Path.GetTempPath(), $"dungnz_load_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_saveDir);
        SaveSystem.OverrideSaveDirectory(_saveDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_saveDir))
            Directory.Delete(_saveDir, recursive: true);
    }

    private static CommandContext MakeContext(FakeDisplayService? display = null)
    {
        var d = display ?? new FakeDisplayService();
        return new CommandContext
        {
            Player           = new PlayerBuilder().WithHP(50).Build(),
            CurrentRoom      = new Room { Description = "Initial room" },
            Rng              = new Random(42),
            Stats            = new RunStats(),
            SessionStats     = new SessionStats(),
            RunStart         = DateTime.UtcNow,
            Display          = d,
            Combat           = new Mock<ICombatEngine>().Object,
            Equipment        = new EquipmentManager(d),
            InventoryManager = new InventoryManager(d),
            Narration        = new NarrationService(new Random(42)),
            Achievements     = new AchievementSystem(),
            AllItems         = new List<Item>(),
            Difficulty       = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel  = Difficulty.Normal,
            Logger           = new Mock<ILogger>().Object,
            TurnConsumed     = true,
            GameOver         = false,
            ExitRun          = _ => { },
            RecordRunEnd     = (_, _) => { },
            GetCurrentlyEquippedForItem = (_, _) => null,
            GetDifficultyName = () => "Normal",
            HandleShrine           = () => { },
            HandleContestedArmory  = () => { },
            HandlePetrifiedLibrary = () => { },
            HandleTrapRoom         = () => { },
        };
    }

    private static GameState MakeSaveableState(
        string playerName = "Adventurer",
        int    hp         = 75,
        int    floor      = 2,
        int?   seed       = 99999)
    {
        var player = new Player { Name = playerName, HP = hp, MaxHP = hp };
        var room   = new Room { Description = "A carved stone passage." };
        return new GameState(player, room, currentFloor: floor, seed: seed);
    }

    // Test 1: Valid save restores player, room, floor, seed
    [Fact]
    public void Handle_ValidSave_RestoresPlayerRoomFloorAndSeed()
    {
        var state = MakeSaveableState(playerName: "TestHero", hp: 80, floor: 3, seed: 12345);
        SaveSystem.SaveGame(state, "valid_restore");

        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        handler.Handle("valid_restore", ctx);

        ctx.Player.Name.Should().Be("TestHero");
        ctx.Player.HP.Should().Be(80);
        ctx.CurrentRoom.Description.Should().Be("A carved stone passage.");
        ctx.CurrentFloor.Should().Be(3);
        ctx.Seed.Should().Be(12345);
        ctx.TurnConsumed.Should().BeTrue();
    }

    // Test 2: Blank argument — error, turn not consumed
    [Fact]
    public void Handle_BlankArgument_ShowsUsageError_TurnNotConsumed()
    {
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        handler.Handle("   ", ctx);

        ctx.TurnConsumed.Should().BeFalse();
        display.Errors.Should().Contain(e => e.Contains("Usage"));
    }

    // Test 3: Corrupt JSON — error, no crash, turn not consumed
    [Fact]
    public void Handle_CorruptJson_ShowsError_DoesNotCrash_TurnNotConsumed()
    {
        File.WriteAllText(Path.Combine(_saveDir, "corrupt.json"), "{ NOT valid json ::::");
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        var act = () => handler.Handle("corrupt", ctx);

        act.Should().NotThrow();
        ctx.TurnConsumed.Should().BeFalse();
        display.Errors.Should().NotBeEmpty();
    }

    // Test 4: Legacy save (no Seed field) — loads cleanly, Seed = null
    [Fact]
    public void Handle_LegacySaveWithNoSeedField_LoadsSuccessfully_SeedIsNull()
    {
        var state = MakeSaveableState(playerName: "OldSave", floor: 1, seed: 77);
        SaveSystem.SaveGame(state, "legacy_noseed");

        var legacyJsonPath = Path.Combine(_saveDir, "legacy_noseed.json");
        var originalJson   = File.ReadAllText(legacyJsonPath);
        using var doc      = JsonDocument.Parse(originalJson);
        var stripped       = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
            if (!prop.Name.Equals("Seed", StringComparison.OrdinalIgnoreCase))
                stripped[prop.Name] = prop.Value;
        File.WriteAllText(legacyJsonPath, JsonSerializer.Serialize(stripped));

        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        var act = () => handler.Handle("legacy_noseed", ctx);

        act.Should().NotThrow();
        ctx.Seed.Should().BeNull();
        ctx.Rng.Should().NotBeNull();
        display.Errors.Should().BeEmpty();
    }

    // Test 5: ShowRoom called exactly once after load
    [Fact]
    public void Handle_ValidSave_CallsShowRoomExactlyOnce()
    {
        var state = MakeSaveableState();
        SaveSystem.SaveGame(state, "showroom_check");
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        handler.Handle("showroom_check", ctx);

        display.ShowRoomCallCount.Should().Be(1,
            "ShowRoom must be called exactly once after loading");
    }

    // Test 6: Non-existent save — error + turn not consumed
    [Fact]
    public void Handle_NonExistentSave_ShowsNotFoundError_TurnNotConsumed()
    {
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        handler.Handle("phantom_save_xyz", ctx);

        ctx.TurnConsumed.Should().BeFalse();
        display.Errors.Should().Contain(e => e.Contains("not found"));
    }

    // Test 7: Successful load resets RunStats
    [Fact]
    public void Handle_ValidSave_ResetsRunStats()
    {
        var state = MakeSaveableState();
        SaveSystem.SaveGame(state, "stats_reset");
        var ctx = MakeContext();
        ctx.Stats.EnemiesDefeated = 99;
        var handler = new LoadCommandHandler();

        handler.Handle("stats_reset", ctx);

        ctx.Stats.EnemiesDefeated.Should().Be(0);
    }

    // Test 8: Successful load shows confirmation message
    [Fact]
    public void Handle_ValidSave_ShowsLoadedConfirmationMessage()
    {
        var state = MakeSaveableState();
        SaveSystem.SaveGame(state, "confirm_msg");
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        var handler = new LoadCommandHandler();

        handler.Handle("confirm_msg", ctx);

        display.Messages.Should().Contain(m => m.Contains("confirm_msg"));
    }
}
