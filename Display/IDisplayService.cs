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
    void ShowCombatStatus(Player player, Enemy enemy);

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
    void ShowLootDrop(Item item);

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
    void ShowCommandPrompt();

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
}
