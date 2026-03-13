
namespace Dungnz.Models;

/// <summary>
/// Input-coupled display operations. Methods in this interface block the game
/// thread to collect user input (menu selections, text entry, confirmations).
/// Extracted from IDisplayService as part of the Avalonia migration (Phase 0).
/// </summary>
/// <remarks>
/// <para>These methods combine display rendering with user-input collection. This
/// coupling is intentional — in a GUI, each menu method becomes a ViewModel that
/// exposes options and a <c>TaskCompletionSource&lt;T&gt;</c> for the result.</para>
/// <para>New code should depend on this interface when it needs to collect player
/// input via menus or text prompts.</para>
/// </remarks>
public interface IGameInput
{
    // ── Text Entry ──

    /// <summary>
    /// Reads a command line input from the player during normal exploration.
    /// In implementations with a live rendering loop (e.g., Spectre.Console Live),
    /// this method pauses the render thread, yields the terminal to the user,
    /// reads the input, then resumes the render loop to prevent race conditions.
    /// </summary>
    /// <returns>The command string entered by the player, or null if input is unavailable.</returns>
    string? ReadCommandInput();

    /// <summary>
    /// Prompts the player to enter their character's name at game start and
    /// returns the value they typed.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    /// <returns>
    /// The name entered by the player, or a default fallback name if none was provided.
    /// </returns>
    string ReadPlayerName();

    /// <summary>Prompts for a 6-digit seed (100000–999999). Returns the seed or null if cancelled.</summary>
    int? ReadSeed();

    // ── Startup Flow ──

    /// <summary>Shows the main startup menu. When <paramref name="hasSaves"/> is false, the Load Save option is omitted.</summary>
    StartupMenuOption ShowStartupMenu(bool hasSaves);

    /// <summary>Shows colored difficulty cards with mechanical context and returns the player's validated choice.</summary>
    Difficulty SelectDifficulty();

    /// <summary>Shows class cards with ASCII stat bars and inline prestige bonuses, returns the player's validated choice.</summary>
    PlayerClassDefinition SelectClass(PrestigeData? prestige);

    /// <summary>Shows the save selection list and returns the chosen save name, or null if cancelled.</summary>
    string? SelectSaveToLoad(string[] saveNames);

    // ── Combat Menus ──

    /// <summary>
    /// Displays an arrow-key navigable combat action menu and returns the player's choice
    /// as a single uppercase letter: "A" (Attack), "B" (Ability), or "F" (Flee).
    /// Falls back to numbered text input when arrow-key input is unavailable.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    string ShowCombatMenuAndSelect(Player player, Enemy enemy);

    /// <summary>
    /// Presents the ability menu. Unavailable abilities shown as info lines.
    /// Returns selected Ability, or null for cancel.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities);

    /// <summary>
    /// Presents an arrow-key navigable consumable item selection menu during combat.
    /// Returns the selected <see cref="Item"/>, or <c>null</c> if the player cancels.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    /// <param name="consumables">The list of consumable items the player can use.</param>
    Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables);

    /// <summary>
    /// Displays an arrow-key navigable level-up stat choice menu and returns the
    /// player's selection as a 1-based index (1 = +5 Max HP, 2 = +2 Attack, 3 = +2 Defense).
    /// Falls back to numbered text input when arrow-key input is unavailable.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    int ShowLevelUpChoiceAndSelect(Player player);

    // ── Inventory / Equipment Menus ──

    /// <summary>
    /// Displays the inventory list, then presents an arrow-key navigable menu for selecting an item.
    /// Returns the selected item, or null if the player cancels.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    /// <param name="player">The player whose inventory to display and select from.</param>
    Item? ShowInventoryAndSelect(Player player);

    /// <summary>
    /// Presents an arrow-key navigable menu of equippable items from the player's inventory.
    /// Returns the selected <see cref="Item"/>, or <c>null</c> if the player cancels.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    /// <param name="equippable">The list of equippable items to display.</param>
    Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable);

    /// <summary>
    /// Presents an arrow-key navigable menu of usable (consumable) items from the player's inventory.
    /// Returns the selected <see cref="Item"/>, or <c>null</c> if the player cancels.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    /// <param name="usable">The list of usable items to display.</param>
    Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable);

    /// <summary>Presents room items as an arrow-key menu. Returns a <see cref="TakeSelection"/> or null for cancel.</summary>
    TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems);

    // ── Shop / Craft Menus ──

    /// <summary>
    /// Renders the shop and handles arrow-key selection. Returns the 1-based index of the
    /// selected item, or 0 if the player cancels.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold);

    /// <summary>
    /// Renders the sell menu and handles arrow-key selection. Returns the 1-based index of the
    /// selected item, or 0 if the player cancels.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold);

    /// <summary>
    /// Presents the shop menu with merchant stock, a Sell Items option, and Leave.
    /// Returns the selected item index (1-based for buying), -1 for Sell, or 0 to Leave.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold);

    /// <summary>
    /// Displays an arrow-key navigable crafting recipe selection menu and returns the
    /// 1-based index of the chosen recipe, or 0 if the player cancels.
    /// Falls back to numbered text input when arrow-key input is unavailable.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes);

    // ── Special Room Menus ──

    /// <summary>Presents the Shrine blessing choices as an arrow-key menu and returns 1–4 or 0 (leave).</summary>
    int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75);

    /// <summary>Presents a Yes/No confirmation menu. Returns true if Yes selected.</summary>
    bool ShowConfirmMenu(string prompt);

    /// <summary>Presents a two-option trap room choice as an arrow-key menu and returns 1, 2, or 0 (leave).</summary>
    int ShowTrapChoiceAndSelect(string header, string option1, string option2);

    /// <summary>Presents the Forgotten Shrine blessing choices and returns 1–3 or 0 (leave).</summary>
    int ShowForgottenShrineMenuAndSelect();

    /// <summary>Presents the Contested Armory approach choices and returns 1, 2, or 0 (leave).</summary>
    int ShowContestedArmoryMenuAndSelect(int playerDefense);

    // ── Skill Tree ──

    /// <summary>
    /// Presents an arrow-key navigable skill tree menu showing all skills available to the player's class.
    /// Locked skills (level requirement not met) are displayed but cannot be selected.
    /// Already-unlocked skills are excluded entirely.
    /// Returns the <see cref="Skill"/> the player wants to learn, or <c>null</c> if cancelled.
    /// </summary>
    /// <remarks>Input-coupled: combines display with user selection. Targeted for separation in the HUD/Dashboard refactor.</remarks>
    /// <param name="player">The player viewing the skill tree.</param>
    Skill? ShowSkillTreeMenu(Player player);
}
