using Dungnz.Models;

namespace Dungnz.Display;

/// <summary>
/// Defines all output (and one input) operations the game uses to communicate
/// with the player, separating presentation logic from game logic so that the
/// console implementation can be swapped for a test stub or a GUI renderer.
/// </summary>
public interface IDisplayService
{
    /// <summary>
    /// Renders the game's title screen / splash banner to the player.
    /// </summary>
    void ShowTitle();

    /// <summary>
    /// Describes the current room to the player, including its exits,
    /// any enemies present, and any items lying on the floor.
    /// </summary>
    /// <param name="room">The room whose details should be displayed.</param>
    void ShowRoom(Room room);

    /// <summary>
    /// Displays a combat-start or combat-event headline (e.g. "A Goblin attacks!").
    /// </summary>
    /// <param name="message">The headline message to show.</param>
    void ShowCombat(string message);

    /// <summary>
    /// Shows a one-line HP status bar for both combatants so the player can
    /// assess the state of the fight at a glance.
    /// </summary>
    /// <param name="player">The player whose current and maximum HP is shown.</param>
    /// <param name="enemy">The enemy whose current and maximum HP is shown.</param>
    /// <param name="playerEffects">Active status effects on the player.</param>
    /// <param name="enemyEffects">Active status effects on the enemy.</param>
    void ShowCombatStatus(Player player, Enemy enemy, 
        IReadOnlyList<ActiveEffect> playerEffects, 
        IReadOnlyList<ActiveEffect> enemyEffects);

    /// <summary>
    /// Displays a single line of flavour or mechanical text that describes what
    /// just happened in combat (e.g. hit/miss/dodge/crit messages).
    /// </summary>
    /// <param name="message">The combat narrative line to display.</param>
    void ShowCombatMessage(string message);

    /// <summary>
    /// Renders a formatted summary of all the player's current statistics,
    /// including HP, attack, defense, gold, XP, and level.
    /// </summary>
    /// <param name="player">The player whose stats should be displayed.</param>
    void ShowPlayerStats(Player player);

    /// <summary>
    /// Lists every item currently in the player's inventory, including item
    /// type annotations to help the player decide what to use or equip.
    /// </summary>
    /// <param name="player">The player whose inventory should be displayed.</param>
    void ShowInventory(Player player);

    /// <summary>
    /// Announces that an enemy dropped an item after being defeated, rendered as a
    /// box-drawn card with type icon and primary stat.
    /// </summary>
    /// <param name="item">The item that was dropped.</param>
    /// <param name="player">The player receiving the loot (used for equipped-weapon comparison).</param>
    /// <param name="isElite">When true, shows an elite callout header.</param>
    void ShowLootDrop(Item item, Player player, bool isElite = false);

    /// <summary>
    /// Displays a gold pickup notification showing the amount gained and the new running total.
    /// </summary>
    /// <param name="amount">The amount of gold picked up.</param>
    /// <param name="newTotal">The player's gold total after the pickup.</param>
    void ShowGoldPickup(int amount, int newTotal);

    /// <summary>
    /// Displays a pickup confirmation for an item, including its primary stat and
    /// the player's updated slot and weight usage.
    /// </summary>
    /// <param name="item">The item that was picked up.</param>
    /// <param name="slotsCurrent">Items currently in inventory after pickup.</param>
    /// <param name="slotsMax">Maximum inventory slot capacity.</param>
    /// <param name="weightCurrent">Total carry weight after pickup.</param>
    /// <param name="weightMax">Maximum carry weight.</param>
    void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax);

    /// <summary>
    /// Renders a full stat card for an item when the player uses the EXAMINE command.
    /// </summary>
    /// <param name="item">The item to examine.</param>
    void ShowItemDetail(Item item);

    /// <summary>
    /// Displays an informational message to the player (e.g. narrative text,
    /// confirmations, and general-purpose game output).
    /// </summary>
    /// <param name="message">The message to display.</param>
    void ShowMessage(string message);

    /// <summary>
    /// Displays an error or warning message to inform the player that their
    /// last action could not be completed (e.g. invalid direction, missing item).
    /// </summary>
    /// <param name="message">The error description to display.</param>
    void ShowError(string message);

    /// <summary>
    /// Prints the full list of available player commands and their aliases.
    /// </summary>
    void ShowHelp();

    /// <summary>
    /// Renders the standard input prompt symbol that invites the player to
    /// type a command during normal exploration.
    /// </summary>
    /// <param name="player">Optional player for displaying mini status bar in prompt.</param>
    void ShowCommandPrompt(Player? player = null);

    /// <summary>
    /// Renders an ASCII mini-map of the dungeon centred on <paramref name="currentRoom"/>,
    /// using BFS to discover all reachable rooms and infer their grid positions from
    /// exit directions. The map shows fog-of-war for unvisited rooms and distinct
    /// symbols for the current room, boss rooms, shrines, enemies, and cleared rooms.
    /// </summary>
    /// <param name="currentRoom">
    /// The room the player currently occupies; rendered at grid origin (0,0) and
    /// displayed with the <c>[*]</c> symbol.
    /// </param>
    void ShowMap(Room currentRoom);

    /// <summary>
    /// Prompts the player to enter their character's name at game start and
    /// returns the value they typed.
    /// </summary>
    /// <returns>
    /// The name entered by the player, or a default fallback name if none was provided.
    /// </returns>
    string ReadPlayerName();

    /// <summary>
    /// Displays a message with the specified ANSI color applied.
    /// </summary>
    /// <param name="message">The message text to display.</param>
    /// <param name="color">The ANSI color code to apply (from <see cref="Systems.ColorCodes"/>).</param>
    void ShowColoredMessage(string message, string color);

    /// <summary>
    /// Displays a combat message with the specified ANSI color applied, using
    /// the standard combat message indentation.
    /// </summary>
    /// <param name="message">The combat message text to display.</param>
    /// <param name="color">The ANSI color code to apply (from <see cref="Systems.ColorCodes"/>).</param>
    void ShowColoredCombatMessage(string message, string color);

    /// <summary>
    /// Displays a stat label and value pair where the value is colorized.
    /// </summary>
    /// <param name="label">The stat label (e.g. "HP:", "Mana:").</param>
    /// <param name="value">The stat value to display.</param>
    /// <param name="valueColor">The ANSI color code to apply to the value.</param>
    void ShowColoredStat(string label, string value, string valueColor);

    /// <summary>
    /// Displays a side-by-side comparison of equipment showing before/after stats
    /// with color-coded deltas (+X green, -X red, no change gray).
    /// </summary>
    /// <param name="player">The player for stat context.</param>
    /// <param name="oldItem">The currently equipped item (null if none).</param>
    /// <param name="newItem">The item being equipped.</param>
    void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem);

    /// <summary>Renders the enhanced ASCII art title screen with colors.</summary>
    void ShowEnhancedTitle();

    /// <summary>Displays the atmospheric lore introduction paragraph. Returns true if the player skipped it.</summary>
    bool ShowIntroNarrative();

    /// <summary>Displays prestige level card. Only called when prestige.PrestigeLevel > 0.</summary>
    void ShowPrestigeInfo(Dungnz.Systems.PrestigeData prestige);

    /// <summary>Shows colored difficulty cards with mechanical context and returns the player's validated choice.</summary>
    Dungnz.Models.Difficulty SelectDifficulty();

    /// <summary>Shows class cards with ASCII stat bars and inline prestige bonuses, returns the player's validated choice.</summary>
    Dungnz.Models.PlayerClassDefinition SelectClass(Dungnz.Systems.PrestigeData? prestige);

    /// <summary>
    /// Renders a box-drawn card for each shop item with type icon, tier-colored name,
    /// tier badge, primary stat, weight, and price (green = affordable, red = too expensive).
    /// </summary>
    /// <param name="stock">The merchant's available stock as (item, price) pairs.</param>
    /// <param name="playerGold">The player's current gold, used to colour-code prices.</param>
    void ShowShop(IEnumerable<(Dungnz.Models.Item item, int price)> stock, int playerGold);

    /// <summary>
    /// Renders the shop and handles arrow-key selection. Returns the 1-based index of the
    /// selected item, or 0 if the player cancels.
    /// </summary>
    int ShowShopAndSelect(IEnumerable<(Dungnz.Models.Item item, int price)> stock, int playerGold);

    /// <summary>
    /// Renders a numbered sell menu listing items the player can sell with tier-colored names
    /// and green sell prices.
    /// </summary>
    void ShowSellMenu(IEnumerable<(Dungnz.Models.Item item, int sellPrice)> items, int playerGold);

    /// <summary>
    /// Renders the sell menu and handles arrow-key selection. Returns the 1-based index of the
    /// selected item, or 0 if the player cancels.
    /// </summary>
    int ShowSellMenuAndSelect(IEnumerable<(Dungnz.Models.Item item, int sellPrice)> items, int playerGold);

    /// <summary>
    /// Renders a box-drawn recipe card showing the craftable result's stats and each ingredient
    /// with ✅ (player has it) or ❌ (missing) availability indicators.
    /// </summary>
    /// <param name="recipeName">Display name of the recipe.</param>
    /// <param name="result">The item that will be produced when the recipe is crafted.</param>
    /// <param name="ingredients">Each ingredient name paired with whether the player currently holds it.</param>
    void ShowCraftRecipe(string recipeName, Dungnz.Models.Item result, List<(string ingredient, bool playerHasIt)> ingredients);

    /// <summary>
    /// Displays the combat start banner with enemy name.
    /// </summary>
    /// <param name="enemy">The enemy to show in the banner.</param>
    void ShowCombatStart(Enemy enemy);

    /// <summary>
    /// Displays one-line flags for Elite, special abilities, etc.
    /// </summary>
    /// <param name="enemy">The enemy whose flags to display.</param>
    void ShowCombatEntryFlags(Enemy enemy);

    /// <summary>
    /// Displays numbered menu of level-up stat choices.
    /// </summary>
    /// <param name="player">The player leveling up.</param>
    void ShowLevelUpChoice(Player player);

    /// <summary>
    /// Displays box-drawn floor banner with floor number and variant.
    /// </summary>
    /// <param name="floor">Current floor number.</param>
    /// <param name="maxFloor">Total floors in run.</param>
    /// <param name="variant">Dungeon variant for display name.</param>
    void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant);

    /// <summary>
    /// Displays detailed enemy card with stats and abilities.
    /// </summary>
    /// <param name="enemy">The enemy to display details for.</param>
    void ShowEnemyDetail(Enemy enemy);

    /// <summary>
    /// Displays full victory screen with run statistics.
    /// </summary>
    /// <param name="player">The player's final state.</param>
    /// <param name="floorsCleared">Number of floors cleared.</param>
    /// <param name="stats">Run statistics.</param>
    void ShowVictory(Player player, int floorsCleared, Dungnz.Systems.RunStats stats);

    /// <summary>
    /// Displays game over screen with cause of death and run statistics.
    /// </summary>
    /// <param name="player">The player's final state.</param>
    /// <param name="killedBy">Optional cause of death.</param>
    /// <param name="stats">Run statistics.</param>
    void ShowGameOver(Player player, string? killedBy, Dungnz.Systems.RunStats stats);

    /// <summary>
    /// Renders the enemy's ASCII art in a styled box before combat, if art is present.
    /// Does nothing when <paramref name="enemy"/> has no art.
    /// </summary>
    /// <param name="enemy">The enemy whose art should be displayed.</param>
    void ShowEnemyArt(Enemy enemy);

    /// <summary>
    /// Displays an arrow-key navigable level-up stat choice menu and returns the
    /// player's selection as a 1-based index (1 = +5 Max HP, 2 = +2 Attack, 3 = +2 Defense).
    /// Falls back to numbered text input when arrow-key input is unavailable.
    /// </summary>
    int ShowLevelUpChoiceAndSelect(Player player);

    /// <summary>
    /// Displays an arrow-key navigable combat action menu and returns the player's choice
    /// as a single uppercase letter: "A" (Attack), "B" (Ability), or "F" (Flee).
    /// Falls back to numbered text input when arrow-key input is unavailable.
    /// </summary>
    string ShowCombatMenuAndSelect(Player player, Enemy enemy);

    /// <summary>
    /// Displays an arrow-key navigable crafting recipe selection menu and returns the
    /// 1-based index of the chosen recipe, or 0 if the player cancels.
    /// Falls back to numbered text input when arrow-key input is unavailable.
    /// </summary>
    int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes);

    /// <summary>
    /// Presents the ability menu. Unavailable abilities shown as info lines.
    /// Returns selected Ability, or null for cancel.
    /// </summary>
    Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities);
}
