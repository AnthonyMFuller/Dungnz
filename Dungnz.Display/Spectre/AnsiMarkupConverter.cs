using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Dungnz.Display.Spectre;

/// <summary>
/// Converts ANSI inline escape codes to Spectre.Console markup, and strips ANSI codes
/// from plain-text output. Extracted from <see cref="SpectreLayoutDisplayService"/> for
/// testability — the logic is pure string transformation with no terminal dependencies.
/// </summary>
internal static class AnsiMarkupConverter
{
    private static readonly Regex AnsiEscapePattern = new(@"\x1B\[[0-9;]*m", RegexOptions.Compiled);

    /// <summary>
    /// Strips all ANSI escape sequences from <paramref name="input"/>, returning plain text
    /// suitable for log output or terminals that do not support color codes.
    /// </summary>
    internal static string StripAnsiCodes(string input) =>
        AnsiEscapePattern.Replace(input, string.Empty);

    /// <summary>
    /// Translates ANSI inline color/bold escape codes in <paramref name="input"/> to
    /// equivalent Spectre.Console markup tags. Plain text segments are escaped with
    /// <see cref="Markup.Escape"/> so that literal brackets are never misinterpreted.
    /// </summary>
    /// <remarks>
    /// Handles nested ANSI sequences (e.g. a bold-yellow outer wrap that contains an
    /// inner bright-red damage number) by closing the current open tag before opening a
    /// new one whenever the color/bold state changes mid-message.
    /// </remarks>
    internal static string ConvertAnsiInlineToSpectre(string input)
    {
        var matches = AnsiEscapePattern.Matches(input);
        if (matches.Count == 0)
            return Markup.Escape(input);

        var result = new StringBuilder();
        var lastIndex = 0;
        var isBold = false;
        var currentColor = "";
        var isTagOpen = false;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                var plainText = input.Substring(lastIndex, match.Index - lastIndex);

                if ((isBold || !string.IsNullOrEmpty(currentColor)) && !string.IsNullOrEmpty(plainText))
                {
                    // Close any previously open tag before opening a new one — this handles
                    // the case where a color change occurs mid-message (e.g. crit hits wrap
                    // the whole message in bold-yellow while the damage number is bright-red).
                    if (isTagOpen)
                        result.Append("[/]");

                    result.Append('[');
                    if (isBold) result.Append("bold");
                    if (isBold && !string.IsNullOrEmpty(currentColor)) result.Append(' ');
                    if (!string.IsNullOrEmpty(currentColor)) result.Append(currentColor);
                    result.Append(']');
                    isTagOpen = true;
                }

                result.Append(Markup.Escape(plainText));
            }

            var code = match.Value;
            if (code == "\u001b[0m") // Reset
            {
                if (isTagOpen)
                {
                    result.Append("[/]");
                    isTagOpen = false;
                }
                isBold = false;
                currentColor = "";
            }
            else if (code == "\u001b[1m") // Bold
            {
                isBold = true;
            }
            else // Color code
            {
                currentColor = code switch
                {
                    "\u001b[91m" => "red",
                    "\u001b[32m" => "green",
                    "\u001b[33m" => "yellow",
                    "\u001b[36m" => "cyan",
                    "\u001b[37m" => "grey",
                    "\u001b[34m" => "blue",
                    "\u001b[97m" => "white",
                    _            => ""
                };
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < input.Length)
        {
            var plainText = input.Substring(lastIndex);
            if ((isBold || !string.IsNullOrEmpty(currentColor)) && !string.IsNullOrEmpty(plainText))
            {
                if (isTagOpen)
                    result.Append("[/]");

                result.Append('[');
                if (isBold) result.Append("bold");
                if (isBold && !string.IsNullOrEmpty(currentColor)) result.Append(' ');
                if (!string.IsNullOrEmpty(currentColor)) result.Append(currentColor);
                result.Append(']');
                isTagOpen = true;
            }
            result.Append(Markup.Escape(plainText));
        }

        if (isTagOpen)
            result.Append("[/]");

        return result.ToString();
    }
}
