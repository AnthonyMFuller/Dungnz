using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Display.Avalonia;

/// <summary>
/// Avalonia implementation of <see cref="IDisplayService"/> (both <see cref="IGameDisplay"/> and <see cref="IGameInput"/>).
/// This is a stub scaffold for Phase 2 — all methods return defaults.
/// Actual rendering and input collection will be implemented in P3-P8.
/// </summary>
public class AvaloniaDisplayService : IDisplayService
{
    // ══════════════════════════════════════════════════════════════════════════
    // IGameDisplay Implementation (Output-only methods)
    // ══════════════════════════════════════════════════════════════════════════

    // TODO: P3-P8 implementation
    public void ShowTitle() { }
    public void ShowEnhancedTitle() { }
    public bool ShowIntroNarrative() => false;
    public void ShowPrestigeInfo(PrestigeData prestige) { }
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant) { }

    public void ShowRoom(Room room) { }
    public void ShowMap(Room currentRoom, int floor = 1) { }

    public void ShowCombat(string message) { }
    public void ShowCombatStatus(Player player, Enemy enemy, IReadOnlyList<ActiveEffect> playerEffects, IReadOnlyList<ActiveEffect> enemyEffects) { }
    public void ShowCombatMessage(string message) { }
    public void ShowColoredCombatMessage(string message, string color) { }
    public void ShowCombatStart(Enemy enemy) { }
    public void ShowCombatEntryFlags(Enemy enemy) { }
    public void ShowEnemyArt(Enemy enemy) { }
    public void ShowEnemyDetail(Enemy enemy) { }
    public void ShowCombatHistory() { }

    public void ShowPlayerStats(Player player) { }
    public void ShowInventory(Player player) { }
    public void ShowEquipment(Player player) { }
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem) { }
    public void ShowItemDetail(Item item) { }
    public void ShowLootDrop(Item item, Player player, bool isElite = false) { }
    public void ShowGoldPickup(int amount, int newTotal) { }
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax) { }

    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold) { }
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold) { }
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients) { }

    public void ShowMessage(string message) { }
    public void ShowError(string message) { }
    public void ShowHelp() { }
    public void ShowCommandPrompt(Player? player = null) { }
    public void ShowColoredMessage(string message, string color) { }
    public void ShowColoredStat(string label, string value, string valueColor) { }
    public void ShowLevelUpChoice(Player player) { }

    public void ShowVictory(Player player, int floorsCleared, RunStats stats) { }
    public void ShowGameOver(Player player, string? killedBy, RunStats stats) { }

    public void RefreshDisplay(Player player, Room room, int floor) { }
    public void UpdateCooldownDisplay(IReadOnlyList<(string name, int turnsRemaining)> cooldowns) { }

    // ══════════════════════════════════════════════════════════════════════════
    // IGameInput Implementation (Input-coupled methods)
    // ══════════════════════════════════════════════════════════════════════════

    // TODO: P3-P8 implementation
    public string? ReadCommandInput() => null;
    public string ReadPlayerName() => "Player";
    public int? ReadSeed() => null;

    public StartupMenuOption ShowStartupMenu(bool hasSaves) => StartupMenuOption.NewGame;
    public Difficulty SelectDifficulty() => Difficulty.Normal;
    public PlayerClassDefinition SelectClass(PrestigeData? prestige) => PlayerClassDefinition.Warrior;
    public string? SelectSaveToLoad(string[] saveNames) => null;

    public string ShowCombatMenuAndSelect(Player player, Enemy enemy) => "a";
    public Ability? ShowAbilityMenuAndSelect(IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities, IEnumerable<Ability> availableAbilities) => null;
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables) => null;
    public int ShowLevelUpChoiceAndSelect(Player player) => 1;

    public Item? ShowInventoryAndSelect(Player player) => null;
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable) => null;
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable) => null;
    public TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems) => null;

    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) => 0;
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold) => 0;
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) => 0;
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes) => 0;

    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75) => 0;
    public bool ShowConfirmMenu(string prompt) => false;
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2) => 1;
    public int ShowForgottenShrineMenuAndSelect() => 0;
    public int ShowContestedArmoryMenuAndSelect(int playerDefense) => 0;

    public Skill? ShowSkillTreeMenu(Player player) => null;
}
