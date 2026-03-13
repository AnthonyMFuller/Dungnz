
namespace Dungnz.Models;

/// <summary>
/// Output-only display operations. Methods in this interface render information
/// to the player but never block for user input. Extracted from IDisplayService
/// as part of the Avalonia migration (Phase 0).
/// </summary>
/// <remarks>
/// <para>New code should depend on this interface when it only needs to push
/// output to the player without collecting input.</para>
/// <para>All existing implementations (ConsoleDisplayService, SpectreLayoutDisplayService)
/// already implement every method — they automatically satisfy this interface via
/// <see cref="IDisplayService"/> inheritance.</para>
/// </remarks>
public interface IGameDisplay
{
    // ── Title / Narrative ──

    /// <summary>
    /// Renders the game's title screen / splash banner to the player.
    /// </summary>
    void ShowTitle();

    /// <summary>Renders the enhanced ASCII art title screen with colors.</summary>
    void ShowEnhancedTitle();

    /// <summary>Displays the atmospheric lore introduction paragraph. Always returns <see langword="false"/> in the current implementation; the return value is reserved for a future skip path.</summary>
    bool ShowIntroNarrative();

    /// <summary>Displays prestige level card. Only called when prestige.PrestigeLevel > 0.</summary>
    void ShowPrestigeInfo(PrestigeData prestige);

    /// <summary>
    /// Displays box-drawn floor banner with floor number and variant.
    /// </summary>
    /// <param name="floor">Current floor number.</param>
    /// <param name="maxFloor">Total floors in run.</param>
    /// <param name="variant">Dungeon variant for display name.</param>
    void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant);

    // ── Room / Map ──

    /// <summary>
    /// Describes the current room to the player, including its exits,
    /// any enemies present, and any items lying on the floor.
    /// </summary>
    /// <param name="room">The room whose details should be displayed.</param>
    void ShowRoom(Room room);

    /// <summary>
    /// Renders an ASCII mini-map of the dungeon centred on <paramref name="currentRoom"/>,
    /// using BFS to discover all reachable rooms and infer their grid positions from
    /// exit directions. The map shows fog-of-war for unvisited rooms and distinct
    /// symbols for the current room, boss rooms, shrines, enemies, and cleared rooms.
    /// </summary>
    /// <param name="currentRoom">
    /// The room the player currently occupies; rendered at grid origin (0,0) and
    /// displayed with the <c>[@]</c> symbol.
    /// </param>
    /// <param name="floor">The current dungeon floor number shown in the panel header.</param>
    void ShowMap(Room currentRoom, int floor = 1);

    // ── Combat Output ──

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
    /// Displays a combat message with the specified ANSI color applied, using
    /// the standard combat message indentation.
    /// </summary>
    /// <param name="message">The combat message text to display.</param>
    /// <param name="color">The ANSI color code to apply (from <c>ColorCodes</c>).</param>
    void ShowColoredCombatMessage(string message, string color);

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
    /// Renders the enemy's ASCII art in a styled box before combat, if art is present.
    /// Does nothing when <paramref name="enemy"/> has no art.
    /// </summary>
    /// <param name="enemy">The enemy whose art should be displayed.</param>
    void ShowEnemyArt(Enemy enemy);

    /// <summary>
    /// Displays detailed enemy card with stats and abilities.
    /// </summary>
    /// <param name="enemy">The enemy to display details for.</param>
    void ShowEnemyDetail(Enemy enemy);

    /// <summary>
    /// Displays the full combat log history in the Content panel (or to console output).
    /// Shows all retained log entries — up to the maximum buffer size — with timestamps and type icons.
    /// Useful for reviewing events from earlier in the combat session.
    /// </summary>
    void ShowCombatHistory();

    // ── Player / Item Display ──

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
    /// Displays the player's currently equipped items in all slots,
    /// using a rich Spectre.Console table layout with tier-coloured item names
    /// and active set bonus information.
    /// </summary>
    /// <param name="player">The player whose equipment is displayed.</param>
    void ShowEquipment(Player player);

    /// <summary>
    /// Displays a side-by-side comparison of equipment showing before/after stats
    /// with color-coded deltas (+X green, -X red, no change gray).
    /// </summary>
    /// <param name="player">The player for stat context.</param>
    /// <param name="oldItem">The currently equipped item (null if none).</param>
    /// <param name="newItem">The item being equipped.</param>
    void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem);

    /// <summary>
    /// Renders a full stat card for an item when the player uses the EXAMINE command.
    /// </summary>
    /// <param name="item">The item to examine.</param>
    void ShowItemDetail(Item item);

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

    // ── Shop / Craft Display ──

    /// <summary>
    /// Renders a box-drawn card for each shop item with type icon, tier-colored name,
    /// tier badge, primary stat, weight, and price (green = affordable, red = too expensive).
    /// </summary>
    /// <param name="stock">The merchant's available stock as (item, price) pairs.</param>
    /// <param name="playerGold">The player's current gold, used to colour-code prices.</param>
    void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold);

    /// <summary>
    /// Renders a numbered sell menu listing items the player can sell with tier-colored names
    /// and green sell prices.
    /// </summary>
    void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold);

    /// <summary>
    /// Renders a box-drawn recipe card showing the craftable result's stats and each ingredient
    /// with ✅ (player has it) or ❌ (missing) availability indicators.
    /// </summary>
    /// <param name="recipeName">Display name of the recipe.</param>
    /// <param name="result">The item that will be produced when the recipe is crafted.</param>
    /// <param name="ingredients">Each ingredient name paired with whether the player currently holds it.</param>
    void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients);

    // ── General Output ──

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
    /// Displays a message with the specified ANSI color applied.
    /// </summary>
    /// <param name="message">The message text to display.</param>
    /// <param name="color">The ANSI color code to apply (from <c>ColorCodes</c>).</param>
    void ShowColoredMessage(string message, string color);

    /// <summary>
    /// Displays a stat label and value pair where the value is colorized.
    /// </summary>
    /// <param name="label">The stat label (e.g. "HP:", "Mana:").</param>
    /// <param name="value">The stat value to display.</param>
    /// <param name="valueColor">The ANSI color code to apply to the value.</param>
    void ShowColoredStat(string label, string value, string valueColor);

    /// <summary>
    /// Displays numbered menu of level-up stat choices.
    /// </summary>
    /// <param name="player">The player leveling up.</param>
    void ShowLevelUpChoice(Player player);

    // ── End Screens ──

    /// <summary>
    /// Displays full victory screen with run statistics.
    /// </summary>
    /// <param name="player">The player's final state.</param>
    /// <param name="floorsCleared">Number of floors cleared.</param>
    /// <param name="stats">Run statistics.</param>
    void ShowVictory(Player player, int floorsCleared, RunStats stats);

    /// <summary>
    /// Displays game over screen with cause of death and run statistics.
    /// </summary>
    /// <param name="player">The player's final state.</param>
    /// <param name="killedBy">Optional cause of death.</param>
    /// <param name="stats">Run statistics.</param>
    void ShowGameOver(Player player, string? killedBy, RunStats stats);

    // ── Refresh / HUD ──

    /// <summary>
    /// Atomically updates all display panels (player stats, room description, and map) to
    /// eliminate panel staleness. Call this at turn boundaries to ensure a consistent display state.
    /// </summary>
    /// <param name="player">The player whose stats should be displayed.</param>
    /// <param name="room">The current room to display.</param>
    /// <param name="floor">The current dungeon floor number shown in the map panel header.</param>
    void RefreshDisplay(Player player, Room room, int floor);

    /// <summary>
    /// Updates the ability cooldown state displayed in the stats panel HUD during combat.
    /// Called each turn after <c>TickCooldowns()</c>. Only abilities that have a cooldown
    /// mechanic (<c>CooldownTurns &gt; 0</c>) are passed; <c>turnsRemaining == 0</c> means ready.
    /// Default is a no-op — only the Spectre layout renderer overrides this.
    /// </summary>
    /// <param name="cooldowns">Cooldown state for each ability with a cooldown mechanic. Pass an empty list outside of combat.</param>
    void UpdateCooldownDisplay(IReadOnlyList<(string name, int turnsRemaining)> cooldowns) { }
}
