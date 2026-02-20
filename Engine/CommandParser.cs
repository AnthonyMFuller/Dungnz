namespace Dungnz.Engine;

public enum CommandType
{
    Go,
    Look,
    Examine,
    Take,
    Use,
    Inventory,
    Stats,
    Help,
    Quit,
    Unknown
}

public class ParsedCommand
{
    public CommandType Type { get; init; }
    public string Argument { get; init; } = string.Empty;
}

public static class CommandParser
{
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
            _ => new ParsedCommand { Type = CommandType.Unknown }
        };
    }
}
