namespace Dungnz.Engine;

/// <summary>
/// Identifies the action the player wants to perform, as parsed from their raw text input.
/// </summary>
public enum CommandType
{
    /// <summary>Move the player in a cardinal direction (north, south, east, or west).</summary>
    Go,

    /// <summary>Re-describe the current room, its exits, enemies, and visible items.</summary>
    Look,

    /// <summary>Inspect a specific enemy, room item, or inventory item for detailed information.</summary>
    Examine,

    /// <summary>Pick up a named item from the floor of the current room.</summary>
    Take,

    /// <summary>Consume or activate a named item from the player's inventory.</summary>
    Use,

    /// <summary>List all items currently held in the player's inventory.</summary>
    Inventory,

    /// <summary>Display the player's current HP, attack, defense, XP, level, and gold.</summary>
    Stats,

    /// <summary>Print the list of available commands and their accepted aliases.</summary>
    Help,

    /// <summary>Exit the game immediately, ending the current run.</summary>
    Quit,

    /// <summary>Equip a weapon, armour piece, or accessory from the player's inventory.</summary>
    Equip,

    /// <summary>Remove the item occupying the specified equipment slot and return it to inventory.</summary>
    Unequip,

    /// <summary>Show a summary of all currently equipped items and their stat bonuses.</summary>
    Equipment,

    /// <summary>Persist the current game state to a named save file.</summary>
    Save,

    /// <summary>Restore a previously saved game state from a named save file.</summary>
    Load,

    /// <summary>Print a list of all available save files.</summary>
    ListSaves,

    /// <summary>Advance to the next dungeon floor from a cleared exit room.</summary>
    Descend,

    /// <summary>Render an ASCII mini-map of all discovered rooms, with the player's current position highlighted.</summary>
    Map,

    /// <summary>Browse the wares of a merchant present in the current room.</summary>
    Shop,

    /// <summary>
    /// The player's input could not be matched to any known command verb;
    /// the game loop will display an error and prompt again.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents a fully parsed player command, carrying the recognised action type
/// and any trailing argument (such as a direction or item name).
/// </summary>
public class ParsedCommand
{
    /// <summary>
    /// The canonical action the player intends to perform.
    /// </summary>
    public CommandType Type { get; init; }

    /// <summary>
    /// The optional argument that accompanies the command verb — for example the
    /// direction for a <see cref="CommandType.Go"/> command or the item name for
    /// <see cref="CommandType.Take"/>. Empty string when no argument is present.
    /// </summary>
    public string Argument { get; init; } = string.Empty;
}

/// <summary>
/// Converts a raw line of player input into a structured <see cref="ParsedCommand"/>
/// by normalising whitespace, splitting on the first space, and matching the verb
/// against all recognised command keywords and their shorthand aliases.
/// </summary>
public static class CommandParser
{
    /// <summary>
    /// Parses a single line of player input into a <see cref="ParsedCommand"/>.
    /// Handles direction shortcuts (n/s/e/w), common abbreviations (inv, ex, etc.),
    /// and falls back to <see cref="CommandType.Unknown"/> for unrecognised verbs.
    /// </summary>
    /// <param name="input">The raw text entered by the player.</param>
    /// <returns>
    /// A <see cref="ParsedCommand"/> whose <see cref="ParsedCommand.Type"/> reflects
    /// the matched action and whose <see cref="ParsedCommand.Argument"/> holds any
    /// trailing text after the command verb.
    /// </returns>
    public static ParsedCommand Parse(string input)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new ParsedCommand { Type = CommandType.Unknown };
        }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1] : string.Empty;

        return command switch
        {
            "go" => new ParsedCommand { Type = CommandType.Go, Argument = argument },
            "north" or "n" => new ParsedCommand { Type = CommandType.Go, Argument = "north" },
            "south" or "s" => new ParsedCommand { Type = CommandType.Go, Argument = "south" },
            "east" or "e" => new ParsedCommand { Type = CommandType.Go, Argument = "east" },
            "west" or "w" => new ParsedCommand { Type = CommandType.Go, Argument = "west" },
            "look" or "l" => new ParsedCommand { Type = CommandType.Look },
            "examine" or "ex" => new ParsedCommand { Type = CommandType.Examine, Argument = argument },
            "take" or "get" => new ParsedCommand { Type = CommandType.Take, Argument = argument },
            "use" => new ParsedCommand { Type = CommandType.Use, Argument = argument },
            "inventory" or "inv" or "i" => new ParsedCommand { Type = CommandType.Inventory },
            "stats" or "status" => new ParsedCommand { Type = CommandType.Stats },
            "help" or "?" or "h" => new ParsedCommand { Type = CommandType.Help },
            "quit" or "exit" or "q" => new ParsedCommand { Type = CommandType.Quit },
            "equip" => new ParsedCommand { Type = CommandType.Equip, Argument = argument },
            "unequip" => new ParsedCommand { Type = CommandType.Unequip, Argument = argument },
            "equipment" or "gear" => new ParsedCommand { Type = CommandType.Equipment },
            "save" => new ParsedCommand { Type = CommandType.Save, Argument = argument },
            "load" => new ParsedCommand { Type = CommandType.Load, Argument = argument },
            "list" or "saves" => new ParsedCommand { Type = CommandType.ListSaves },
            "descend" or "down" => new ParsedCommand { Type = CommandType.Descend },
            "map" or "m" => new ParsedCommand { Type = CommandType.Map },
            "shop" or "buy" => new ParsedCommand { Type = CommandType.Shop },
            _ => new ParsedCommand { Type = CommandType.Unknown }
        };
    }
}
