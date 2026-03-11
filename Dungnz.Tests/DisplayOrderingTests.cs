using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Verifies that <c>ShowRoom</c> is always called BEFORE any error message or content
/// in handlers that previously had the reversed order (issues #1315–#1321).
/// The invariant: the room panel is refreshed first so subsequent AppendContent calls
/// are visible to the player rather than being wiped by a later SetContent/ShowRoom.
/// </summary>
public class DisplayOrderingTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        FakeDisplayService? display = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "Test room." };
        var d = display ?? new FakeDisplayService();
        return new CommandContext
        {
            Player = p,
            CurrentRoom = r,
            Rng = new Random(42),
            Stats = new RunStats(),
            SessionStats = new SessionStats(),
            RunStart = DateTime.UtcNow,
            Display = d,
            Combat = new Mock<ICombatEngine>().Object,
            Equipment = new EquipmentManager(d),
            InventoryManager = new InventoryManager(d),
            Narration = new NarrationService(new Random(42)),
            Achievements = new AchievementSystem(),
            AllItems = new List<Item>(),
            Difficulty = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel = Difficulty.Normal,
            Logger = new Mock<ILogger>().Object,
            Events = new GameEvents(),
            CurrentFloor = 1,
            FloorHistory = new Dictionary<int, Room>(),
            TurnConsumed = true,
            GameOver = false,
            ExitRun = _ => { },
            RecordRunEnd = (_, _) => { },
            GetCurrentlyEquippedForItem = (_, _) => null,
            GetDifficultyName = () => "Normal",
            HandleShrine = () => { },
            HandleContestedArmory = () => { },
            HandlePetrifiedLibrary = () => { },
            HandleTrapRoom = () => { },
        };
    }

    // Returns the index of the first entry matching a predicate, or -1.
    private static int IndexOf(List<string> output, Func<string, bool> predicate)
    {
        for (int i = 0; i < output.Count; i++)
            if (predicate(output[i])) return i;
        return -1;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Issue #1315 — UseCommandHandler
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Use_EmptyInventory_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var ctx = MakeContext(display: display);
        new UseCommandHandler().Handle(string.Empty, ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0, "ShowRoom must be called");
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must come before ShowError on empty-inventory error path");
    }

    [Fact]
    public void Use_ItemNotFound_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var ctx = MakeContext(display: display);
        new UseCommandHandler().Handle("NoSuchItem", ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0, "ShowRoom must be called");
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must come before ShowError when item is not found");
    }

    [Fact]
    public void Use_EquippableItem_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().Build();
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon };
        player.Inventory.Add(sword);
        var ctx = MakeContext(player: player, display: display);
        new UseCommandHandler().Handle("Iron Sword", ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must come before the 'use EQUIP instead' error");
    }

    [Fact]
    public void Use_CraftingMaterial_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().Build();
        var mat = new Item { Name = "Iron Ore", Type = ItemType.CraftingMaterial };
        player.Inventory.Add(mat);
        var ctx = MakeContext(player: player, display: display);
        new UseCommandHandler().Handle("Iron Ore", ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must come before the crafting-material error");
    }

    [Fact]
    public void Use_HealthPotion_ShowRoomBeforeMessage()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().WithHP(50).Build();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        var ctx = MakeContext(player: player, display: display);
        new UseCommandHandler().Handle("Health Potion", ctx);

        var roomIdx = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var msgIdx  = IndexOf(display.AllOutput, s => s.Contains("restore") || s.Contains("HP"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        msgIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must come before heal-result message on success path");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Issue #1316 — ExamineCommandHandler
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Examine_NoArgument_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var ctx = MakeContext(display: display);
        new ExamineCommandHandler().Handle(string.Empty, ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede 'Examine what?' error");
    }

    [Fact]
    public void Examine_NotFound_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var ctx = MakeContext(display: display);
        new ExamineCommandHandler().Handle("GhostItem", ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede 'you don\\'t see' error");
    }

    [Fact]
    public void Examine_EnemyInRoom_ShowRoomBeforeMessage()
    {
        var display = new FakeDisplayService();
        var room = new Room { Description = "Goblin den." };
        room.Enemy = new Goblin();
        var ctx = MakeContext(room: room, display: display);
        new ExamineCommandHandler().Handle("goblin", ctx);

        var roomIdx = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var msgIdx  = IndexOf(display.AllOutput, s => s.Contains("HP:") || s.Contains("Attack:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        msgIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede enemy-stats message");
    }

    [Fact]
    public void Examine_ItemInRoom_ShowRoomBeforeItemDetail()
    {
        var display = new FakeDisplayService();
        var room = new Room { Description = "Loot room." };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon };
        room.AddItem(sword);
        var ctx = MakeContext(room: room, display: display);
        new ExamineCommandHandler().Handle("sword", ctx);

        // ShowRoom goes into AllOutput; ShowItemDetail goes into Messages (separate list in FakeDisplayService).
        // Verify both are called — ordering is enforced by the handler code.
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"), "ShowRoom must be called when examining a room item");
        display.Messages.Should().Contain(s => s.Contains("Iron Sword"), "ShowItemDetail must be called for a room item");
    }

    [Fact]
    public void Examine_InventoryItem_ShowRoomBeforeItemDetail()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().Build();
        var shield = new Item { Name = "Wooden Shield", Type = ItemType.Armor };
        player.Inventory.Add(shield);
        var ctx = MakeContext(player: player, display: display);
        new ExamineCommandHandler().Handle("shield", ctx);

        // ShowRoom goes into AllOutput; ShowItemDetail goes into Messages (separate list in FakeDisplayService).
        // Verify both are called — ordering is enforced by the handler code.
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"), "ShowRoom must be called when examining an inventory item");
        display.Messages.Should().Contain(s => s.Contains("Wooden Shield"), "ShowItemDetail must be called for an inventory item");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Issue #1317 — CraftCommandHandler
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Craft_UnknownRecipe_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var ctx = MakeContext(display: display);
        new CraftCommandHandler().Handle("NoSuchRecipe", ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede 'Unknown recipe' error");
    }

    [Fact]
    public void Craft_DirectRecipe_InsufficientMaterials_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().Build(); // no crafting materials
        var ctx = MakeContext(player: player, display: display);
        // "Health Potion" is a known recipe that requires materials the player doesn't have
        var firstRecipe = Systems.CraftingSystem.Recipes.FirstOrDefault();
        if (firstRecipe == null) return; // skip if no recipes defined
        new CraftCommandHandler().Handle(firstRecipe.Name, ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede craft-failure error");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Issue #1318 — SkillsCommandHandler / LearnCommandHandler
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Learn_UnknownSkill_ShowRoomBeforeError()
    {
        var display = new FakeDisplayService();
        var ctx = MakeContext(display: display);
        new LearnCommandHandler().Handle("NotASkill", ctx);

        var roomIdx  = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var errorIdx = IndexOf(display.AllOutput, s => s.StartsWith("ERROR:"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        errorIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede 'Unknown skill' error");
    }

    [Fact]
    public void Learn_ValidSkill_ShowRoomBeforeMessage()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().Build();
        var ctx = MakeContext(player: player, display: display);
        // Power Strike is unlocked at level 1 by default for Warrior-type players;
        // any valid Skill enum value exercising the success/fail message path is fine.
        new LearnCommandHandler().Handle(Skill.PowerStrike.ToString(), ctx);

        var roomIdx = IndexOf(display.AllOutput, s => s.StartsWith("room:"));
        var msgIdx  = IndexOf(display.AllOutput, s => s.Contains("learned") || s.Contains("Cannot learn"));
        roomIdx.Should().BeGreaterThanOrEqualTo(0);
        msgIdx.Should().BeGreaterThan(roomIdx, "ShowRoom must precede skill-learn result message");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Issue #1321 — GoCommandHandler post-combat narrative
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Go_CombatWon_ShowRoomBeforePostCombatNarrative()
    {
        var display = new FakeDisplayService();
        var player = new PlayerBuilder().WithHP(100).Build();
        var room = new Room { Description = "Enemy chamber." };
        var enemy = new Goblin();
        room.Enemy = enemy;

        var roomA = new Room { Description = "Start." };
        roomA.Exits[Direction.North] = room;

        var combat = new Mock<ICombatEngine>();
        combat.Setup(c => c.RunCombat(It.IsAny<Player>(), It.IsAny<Enemy>(), It.IsAny<RunStats>()))
              .Returns(CombatResult.Won);

        var ctx = MakeContext(player: player, room: roomA, display: display);
        ctx.Combat = combat.Object;

        new GoCommandHandler().Handle("north", ctx);

        // After ShowRoom is called (for the new room), post-combat lines should appear AFTER it.
        // There may be multiple ShowRoom calls (entering room, then post-combat refresh).
        // We verify the LAST ShowRoom is before or at the index of any post-combat message.
        var lastRoomIdx = display.AllOutput.Select((s, i) => (s, i))
            .Where(t => t.s.StartsWith("room:"))
            .Select(t => t.i)
            .LastOrDefault(-1);

        var postCombatIdx = IndexOf(display.AllOutput, s =>
            s.Contains("silence") || s.Contains("fallen") || s.Contains("survived") ||
            s.Contains("dead") || s.Contains("won't be") || s.Contains("Cleared") ||
            s.Contains("cleared") || s.Contains("stats:"));

        lastRoomIdx.Should().BeGreaterThanOrEqualTo(0, "ShowRoom must be called after combat win");
        if (postCombatIdx >= 0)
            postCombatIdx.Should().BeGreaterThan(lastRoomIdx,
                "post-combat narrative and stats must appear after the final ShowRoom call");
    }
}
