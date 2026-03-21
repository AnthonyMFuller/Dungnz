using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Dungnz.Display.Avalonia.Controls;

/// <summary>
/// A <see cref="TextBlock"/> that parses ANSI escape codes in
/// <see cref="FormattedText"/> and renders colored <see cref="Run"/> inlines.
/// </summary>
public partial class AnsiTextBlock : TextBlock
{
    /// <summary>
    /// Styled property for the ANSI-encoded text input.  Named
    /// <c>FormattedText</c> to avoid conflicting with
    /// <see cref="TextBlock.Text"/>.
    /// </summary>
    public static readonly StyledProperty<string?> FormattedTextProperty =
        AvaloniaProperty.Register<AnsiTextBlock, string?>(nameof(FormattedText));

    /// <summary>
    /// Gets or sets the ANSI-encoded text to display.
    /// </summary>
    public string? FormattedText
    {
        get => GetValue(FormattedTextProperty);
        set => SetValue(FormattedTextProperty, value);
    }

    // Static brush cache - one allocation per color for the lifetime of the app.
    private static readonly Dictionary<string, IBrush> AnsiColorMap = new()
    {
        ["31"] = new SolidColorBrush(Color.Parse("#FF4444")),   // Red
        ["32"] = new SolidColorBrush(Color.Parse("#44FF44")),   // Green
        ["33"] = new SolidColorBrush(Color.Parse("#FFDD44")),   // Yellow
        ["34"] = new SolidColorBrush(Color.Parse("#4488FF")),   // Blue
        ["35"] = new SolidColorBrush(Color.Parse("#DD44FF")),   // Magenta
        ["36"] = new SolidColorBrush(Color.Parse("#44DDFF")),   // Cyan
        ["37"] = new SolidColorBrush(Color.Parse("#DDDDDD")),   // White
        ["90"] = new SolidColorBrush(Color.Parse("#888888")),   // Gray
        ["91"] = new SolidColorBrush(Color.Parse("#FF6666")),   // BrightRed
        ["96"] = new SolidColorBrush(Color.Parse("#66FFFF")),   // BrightCyan
        ["97"] = new SolidColorBrush(Color.Parse("#FFFFFF")),   // BrightWhite
    };

    static AnsiTextBlock()
    {
        FormattedTextProperty.Changed.AddClassHandler<AnsiTextBlock>(
            (tb, _) => tb.RebuildInlines());
    }

    /// <summary>
    /// Parses <see cref="FormattedText"/> for ANSI escape sequences and
    /// rebuilds the <see cref="TextBlock.Inlines"/> collection.
    /// </summary>
    private void RebuildInlines()
    {
        Inlines?.Clear();

        var text = FormattedText;
        if (string.IsNullOrEmpty(text))
            return;

        IBrush? currentBrush = null;
        var isBold = false;
        var lastIndex = 0;

        foreach (Match match in AnsiEscapeRegex().Matches(text))
        {
            // Emit the plain-text segment before this escape code.
            if (match.Index > lastIndex)
                AddTextSegment(text[lastIndex..match.Index], currentBrush, isBold);

            // Process each code part (handles compound codes like "1;31").
            var code = match.Groups[1].Value;
            foreach (var part in code.Split(';'))
            {
                if (part is "0" or "")
                {
                    currentBrush = null;
                    isBold = false;
                }
                else if (part is "1")
                {
                    isBold = true;
                }
                else if (AnsiColorMap.TryGetValue(part, out var brush))
                {
                    currentBrush = brush;
                }
            }

            lastIndex = match.Index + match.Length;
        }

        // Trailing text after the last escape code.
        if (lastIndex < text.Length)
            AddTextSegment(text[lastIndex..], currentBrush, isBold);
    }

    /// <summary>
    /// Appends one segment of plain text (which may contain newlines) to
    /// <see cref="TextBlock.Inlines"/>, applying the current color/bold state.
    /// </summary>
    private void AddTextSegment(string segment, IBrush? brush, bool bold)
    {
        var lines = segment.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                Inlines?.Add(new LineBreak());

            if (lines[i].Length > 0)
            {
                var run = new Run(lines[i]);
                if (brush is not null)
                    run.Foreground = brush;
                if (bold)
                    run.FontWeight = FontWeight.Bold;
                Inlines?.Add(run);
            }
        }
    }

    [GeneratedRegex(@"\x1b\[([0-9;]*)m")]
    private static partial Regex AnsiEscapeRegex();
}
