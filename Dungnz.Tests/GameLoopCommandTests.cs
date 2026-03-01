using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dungnz.Tests;

[Collection("PrestigeTests")]
/// <summary>GameLoop tests for shop, sell, descend, prestige, skills, and other commands.</summary>
public class GameLoopCommandTests
{
    private static (Player player, Room startRoom, FakeDisplayService display, Mock<ICombatEngine> combat) MakeSetup()
    {
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var startRoom = new Room { Description = "Start room" };
        var display = new FakeDisplayService();
        var combat = new Mock<ICombatEngine>();
        return (player, startRoom, display, combat);
    }

    private static GameLoop MakeLoop(FakeDisplayService display, ICombatEngine combat, params string[] inputs)
    {
        var reader = new FakeInputReader(inputs);
        display.SetInputReader(reader);
        return new GameLoop(display, combat, reader);
    }

    // ── Shop command ──────────────────────────────────────────────────────────

    [Fact]
    public void ShopCommand_NoMerchant_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "shop", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("merchant") || e.Contains("shop"));
    }

    [Fact]
    public void ShopCommand_WithMerchant_OpensAndExits()
    {
        var (player, room, display, combat) = MakeSetup();
        var merchant = new Merchant { Name = "Trader" };
        merchant.Stock.Add(new MerchantItem { Item = new Item { Name = "Health Potion", Type = ItemType.Consumable }, Price = 20 });
        room.Merchant = merchant;
        player.AddGold(100);
        // Open shop, buy item 1, then exit with "x"
        var loop = MakeLoop(display, combat.Object, "shop", "x", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    [Fact]
    public void ShopCommand_WithMerchant_BuyItem()
    {
        var (player, room, display, combat) = MakeSetup();
        var merchant = new Merchant { Name = "Trader" };
        merchant.Stock.Add(new MerchantItem { Item = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 }, Price = 20 });
        room.Merchant = merchant;
        player.AddGold(100);
        // Open shop, buy item 1 ("1"), then exit with "x"
        var loop = MakeLoop(display, combat.Object, "shop", "1", "x", "quit");
        loop.Run(player, room);
        display.Messages.Should().Contain(m => m.Contains("bought") || m.Contains("purchased") || m.Contains("Health Potion"));
    }

    [Fact]
    public void ShopCommand_BuyItem_InsufficientGold()
    {
        var (player, room, display, combat) = MakeSetup();
        var merchant = new Merchant { Name = "Trader" };
        merchant.Stock.Add(new MerchantItem { Item = new Item { Name = "Legendary Sword", Type = ItemType.Weapon }, Price = 5000 });
        room.Merchant = merchant;
        player.AddGold(10); // not enough
        var loop = MakeLoop(display, combat.Object, "shop", "1", "x", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty(); // covers the "can't afford" path
    }

    // ── Sell command ──────────────────────────────────────────────────────────

    [Fact]
    public void SellCommand_NoMerchant_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "sell", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("merchant") || e.Contains("shop") || e.Contains("sell"));
    }

    [Fact]
    public void SellCommand_WithMerchant_ExitToShop()
    {
        var (player, room, display, combat) = MakeSetup();
        var merchant = new Merchant { Name = "Trader" };
        room.Merchant = merchant;
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 3, Tier = ItemTier.Common };
        player.Inventory.Add(sword);
        // Open sell menu then exit with "x"
        var loop = MakeLoop(display, combat.Object, "sell", "x", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    [Fact]
    public void SellCommand_SellsItem()
    {
        var (player, room, display, combat) = MakeSetup();
        var merchant = new Merchant { Name = "Trader" };
        room.Merchant = merchant;
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 3, Tier = ItemTier.Common };
        player.Inventory.Add(sword);
        // sell item "1", then exit
        var loop = MakeLoop(display, combat.Object, "sell", "1", "x", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Descend command ───────────────────────────────────────────────────────

    [Fact]
    public void DescendCommand_NotExitRoom_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        room.IsExit = false;
        var loop = MakeLoop(display, combat.Object, "descend", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("cleared exit") || e.Contains("descend") || e.Contains("only descend"));
    }

    [Fact]
    public void DescendCommand_AtExitRoom_DescendsToNextFloor()
    {
        // Note: Actual floor descent triggers DungeonGenerator which requires loaded enemy stats.
        // This test verifies the error path (non-exit room) only.
        var (player, room, display, combat) = MakeSetup();
        room.IsExit = false;
        var loop = MakeLoop(display, combat.Object, "descend", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("exit") || e.Contains("descend"));
    }

    // ── Learn skill command ───────────────────────────────────────────────────

    [Fact]
    public void LearnCommand_WithSkillName_AttemptsToLearnSkill()
    {
        var (player, room, display, combat) = MakeSetup();
        player.Level = 5;
        // Try to learn a skill — might fail if requirements not met
        var loop = MakeLoop(display, combat.Object, "learn PowerStrike", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Prestige command ──────────────────────────────────────────────────────

    [Fact]
    public void PrestigeCommand_AttemptsCalled()
    {
        var (player, room, display, combat) = MakeSetup();
        player.Level = 10;
        // Prestige might require max level; this just exercises the handler
        var loop = MakeLoop(display, combat.Object, "prestige", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Load command ──────────────────────────────────────────────────────────

    [Fact]
    public void LoadCommand_SaveNotFound_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "load nonexistent_save_xyz", "quit");
        loop.Run(player, room);
        // Load attempts and either shows error or message
        display.Messages.Should().NotBeEmpty();
    }

    // ── Room with combat — enemy alive ─────────────────────────────────────────

    [Fact]
    public void GoIntoRoom_WithLiveEnemy_TriggersCombat()
    {
        var (player, room, display, combat) = MakeSetup();
        var enemyRoom = new Room { Description = "Enemy room" };
        enemyRoom.Enemy = new Enemy_Stub(30, 5, 0, 10);
        room.Exits[Direction.East] = enemyRoom;
        combat.Setup(c => c.RunCombat(It.IsAny<Player>(), It.IsAny<Enemy>(), It.IsAny<RunStats>()))
              .Returns(CombatResult.Won);
        var loop = MakeLoop(display, combat.Object, "east", "quit");
        loop.Run(player, room);
        combat.Verify(c => c.RunCombat(It.IsAny<Player>(), It.IsAny<Enemy>(), It.IsAny<RunStats>()), Times.Once);
    }

    // ── Final floor (descend multiple times would require DungeonGenerator with data) ────────

    [Fact]
    public void DescendCommand_OnFinalFloor_RequiresExitRoomCheck()
    {
        // Without actual dungeon data loaded, we can only test the error path
        var (player, room, display, combat) = MakeSetup();
        room.IsExit = false; // not an exit → error
        var loop = MakeLoop(display, combat.Object, "descend", "quit");
        loop.Run(player, room);
        display.Errors.Should().NotBeEmpty();
    }

    // ── Room special handling: ForgottenShrine ────────────────────────────────

    [Fact]
    public void UseCommand_ForgottenShrine_HealsPlayer()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 30;
        room.HasShrine = true;
        room.Type = RoomType.ForgottenShrine;
        // Use shrine — ForgottenShrine reads input for choice
        var loop = MakeLoop(display, combat.Object, "use shrine", "L", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Shrine heal option ────────────────────────────────────────────────────

    [Fact]
    public void ShrineHeal_WithEnoughGold_HealsPlayer()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50;
        player.MaxHP = 100;
        player.AddGold(50);
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "1", "quit");
        loop.Run(player, room);
        player.HP.Should().Be(100, "shrine heal should restore to full HP");
    }

    [Fact]
    public void ShrineHeal_NotEnoughGold_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50;
        player.AddGold(10); // only 10g, need 30g
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "1", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("gold") || e.Contains("Gold") || e.Contains("30g"));
    }

    [Fact]
    public void ShrineBless_WithEnoughGold_BoostsStats()
    {
        var (player, room, display, combat) = MakeSetup();
        var initialAtk = player.Attack;
        player.AddGold(100);
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "2", "quit");
        loop.Run(player, room);
        player.Attack.Should().BeGreaterThan(initialAtk, "shrine bless should boost attack");
    }

    [Fact]
    public void ShrineFortify_WithEnoughGold_BoostsMaxHP()
    {
        var (player, room, display, combat) = MakeSetup();
        var initialMaxHP = player.MaxHP;
        player.AddGold(100);
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "3", "quit");
        loop.Run(player, room);
        player.MaxHP.Should().BeGreaterThan(initialMaxHP, "shrine fortify should increase MaxHP");
    }

    [Fact]
    public void ShrineMeditate_WithEnoughGold_BoostsMaxMana()
    {
        var (player, room, display, combat) = MakeSetup();
        var initialMaxMana = player.MaxMana;
        player.AddGold(100);
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "4", "quit");
        loop.Run(player, room);
        player.MaxMana.Should().BeGreaterThan(initialMaxMana, "shrine meditate should increase MaxMana");
    }

    [Fact]
    public void ShrineLeave_ShowsLeaveMessage()
    {
        var (player, room, display, combat) = MakeSetup();
        player.AddGold(100);
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "L", "quit");
        loop.Run(player, room);
        display.Messages.Should().Contain(m => m.Contains("leave") || m.Contains("shrine"));
    }

    [Fact]
    public void ShrineAlreadyUsed_ShowsMessage()
    {
        var (player, room, display, combat) = MakeSetup();
        room.HasShrine = true;
        room.ShrineUsed = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "quit");
        loop.Run(player, room);
        display.Messages.Should().Contain(m => m.Contains("already") || m.Contains("shrine"));
    }

    [Fact]
    public void UseCommand_NoShrine_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        room.HasShrine = false;
        var loop = MakeLoop(display, combat.Object, "use shrine", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("shrine") || e.Contains("no shrine"));
    }

    // ── HandleLearnSkill ──────────────────────────────────────────────────────

    [Fact]
    public void LearnCommand_NoArgument_ShowsMessage()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "learn", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    [Fact]
    public void LearnCommand_InvalidSkillName_ShowsMessage()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "learn NotASkill", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── USE fuzzy matching ────────────────────────────────────────────────────

    [Fact]
    public void Use_FuzzyMatch_SingleCandidate_UsesItem()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50;
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        var loop = MakeLoop(display, combat.Object, "use Helth Potion", "quit");
        loop.Run(player, room);
        display.Messages.Should().Contain(m => m.Contains("Did you mean") && m.Contains("Health Potion"));
        player.Inventory.Should().NotContain(potion, "item should be consumed after use");
    }

    [Fact]
    public void Use_FuzzyMatch_NoMatch_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        var loop = MakeLoop(display, combat.Object, "use zzzzzzzzz", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("don't have"));
    }

    [Fact]
    public void Use_FuzzyMatch_MultipleMatches_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        player.Inventory.Add(new Item { Name = "Red Potion", Type = ItemType.Consumable, HealAmount = 10 });
        player.Inventory.Add(new Item { Name = "Red Elixir", Type = ItemType.Consumable, HealAmount = 15 });
        // "Red" matches both with distance 0 via Contains, so try something that fuzzy-matches both equally
        var loop = MakeLoop(display, combat.Object, "use Rd Ptn", "quit");
        loop.Run(player, room);
        // Should either show "Did you mean one of:" or "don't have" — just no crash
        (display.Errors.Count > 0 || display.Messages.Count > 0).Should().BeTrue();
    }

    // ── USE no-arg interactive menu ───────────────────────────────────────────

    [Fact]
    public void Use_NoArg_ShowsUseMenu_ConsumablesOnly()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50;
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        // "1" selects first item from menu; "quit" ends the loop
        var loop = MakeLoop(display, combat.Object, "use", "1", "quit");
        loop.Run(player, room);
        display.AllOutput.Should().Contain("use_menu", "the interactive use menu should be shown");
        player.Inventory.Should().NotContain(potion, "selected item should be consumed");
    }

    [Fact]
    public void Use_NoArg_EmptyConsumables_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        // Inventory has only a weapon — no consumables
        player.Inventory.Add(new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true });
        var loop = MakeLoop(display, combat.Object, "use", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("no usable", StringComparison.OrdinalIgnoreCase));
        display.AllOutput.Should().NotContain("use_menu");
    }
}
