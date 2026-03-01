using Dungnz.Models;
using Dungnz.Systems;
using Spectre.Console;

namespace Dungnz.Display;

/// <summary>
/// Spectre.Console-backed implementation of IDisplayService.
/// Replaces hand-rolled ANSI with Spectre widgets.
/// </summary>
public sealed class SpectreDisplayService : IDisplayService
{
    /// <inheritdoc/>
    public void ShowTitle() =>
        throw new NotImplementedException("SpectreDisplayService.ShowTitle not yet implemented");

    /// <inheritdoc/>
    public void ShowRoom(Room room) =>
        throw new NotImplementedException("SpectreDisplayService.ShowRoom not yet implemented");

    /// <inheritdoc/>
    public void ShowCombat(string message) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombat not yet implemented");

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombatStatus not yet implemented");

    /// <inheritdoc/>
    public void ShowCombatMessage(string message) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombatMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowPlayerStats not yet implemented");

    /// <inheritdoc/>
    public void ShowInventory(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowInventory not yet implemented");

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false) =>
        throw new NotImplementedException("SpectreDisplayService.ShowLootDrop not yet implemented");

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal) =>
        throw new NotImplementedException("SpectreDisplayService.ShowGoldPickup not yet implemented");

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax) =>
        throw new NotImplementedException("SpectreDisplayService.ShowItemPickup not yet implemented");

    /// <inheritdoc/>
    public void ShowItemDetail(Item item) =>
        throw new NotImplementedException("SpectreDisplayService.ShowItemDetail not yet implemented");

    /// <inheritdoc/>
    public void ShowMessage(string message) =>
        throw new NotImplementedException("SpectreDisplayService.ShowMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowError(string message) =>
        throw new NotImplementedException("SpectreDisplayService.ShowError not yet implemented");

    /// <inheritdoc/>
    public void ShowHelp() =>
        throw new NotImplementedException("SpectreDisplayService.ShowHelp not yet implemented");

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCommandPrompt not yet implemented");

    /// <inheritdoc/>
    public void ShowMap(Room currentRoom) =>
        throw new NotImplementedException("SpectreDisplayService.ShowMap not yet implemented");

    /// <inheritdoc/>
    public string ReadPlayerName() =>
        throw new NotImplementedException("SpectreDisplayService.ReadPlayerName not yet implemented");

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color) =>
        throw new NotImplementedException("SpectreDisplayService.ShowColoredMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color) =>
        throw new NotImplementedException("SpectreDisplayService.ShowColoredCombatMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor) =>
        throw new NotImplementedException("SpectreDisplayService.ShowColoredStat not yet implemented");

    /// <inheritdoc/>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem) =>
        throw new NotImplementedException("SpectreDisplayService.ShowEquipmentComparison not yet implemented");

    /// <inheritdoc/>
    public void ShowEnhancedTitle() =>
        throw new NotImplementedException("SpectreDisplayService.ShowEnhancedTitle not yet implemented");

    /// <inheritdoc/>
    public bool ShowIntroNarrative() =>
        throw new NotImplementedException("SpectreDisplayService.ShowIntroNarrative not yet implemented");

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige) =>
        throw new NotImplementedException("SpectreDisplayService.ShowPrestigeInfo not yet implemented");

    /// <inheritdoc/>
    public Difficulty SelectDifficulty() =>
        throw new NotImplementedException("SpectreDisplayService.SelectDifficulty not yet implemented");

    /// <inheritdoc/>
    public PlayerClassDefinition SelectClass(PrestigeData? prestige) =>
        throw new NotImplementedException("SpectreDisplayService.SelectClass not yet implemented");

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowShop not yet implemented");

    /// <inheritdoc/>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowShopAndSelect not yet implemented");

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowSellMenu not yet implemented");

    /// <inheritdoc/>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowSellMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCraftRecipe not yet implemented");

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombatStart not yet implemented");

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombatEntryFlags not yet implemented");

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowLevelUpChoice not yet implemented");

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant) =>
        throw new NotImplementedException("SpectreDisplayService.ShowFloorBanner not yet implemented");

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy) =>
        throw new NotImplementedException("SpectreDisplayService.ShowEnemyDetail not yet implemented");

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats) =>
        throw new NotImplementedException("SpectreDisplayService.ShowVictory not yet implemented");

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats) =>
        throw new NotImplementedException("SpectreDisplayService.ShowGameOver not yet implemented");

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy) =>
        throw new NotImplementedException("SpectreDisplayService.ShowEnemyArt not yet implemented");

    /// <inheritdoc/>
    public int ShowLevelUpChoiceAndSelect(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowLevelUpChoiceAndSelect not yet implemented");

    /// <inheritdoc/>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombatMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCraftMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75) =>
        throw new NotImplementedException("SpectreDisplayService.ShowShrineMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowShopWithSellAndSelect not yet implemented");

    /// <inheritdoc/>
    public bool ShowConfirmMenu(string prompt) =>
        throw new NotImplementedException("SpectreDisplayService.ShowConfirmMenu not yet implemented");

    /// <inheritdoc/>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2) =>
        throw new NotImplementedException("SpectreDisplayService.ShowTrapChoiceAndSelect not yet implemented");

    /// <inheritdoc/>
    public int ShowForgottenShrineMenuAndSelect() =>
        throw new NotImplementedException("SpectreDisplayService.ShowForgottenShrineMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense) =>
        throw new NotImplementedException("SpectreDisplayService.ShowContestedArmoryMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities) =>
        throw new NotImplementedException("SpectreDisplayService.ShowAbilityMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCombatItemMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable) =>
        throw new NotImplementedException("SpectreDisplayService.ShowEquipMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable) =>
        throw new NotImplementedException("SpectreDisplayService.ShowUseMenuAndSelect not yet implemented");

    /// <inheritdoc/>
    public Item? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems) =>
        throw new NotImplementedException("SpectreDisplayService.ShowTakeMenuAndSelect not yet implemented");
}
