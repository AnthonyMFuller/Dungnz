using Terminal.Gui;

namespace Dungnz.Display.Tui;

/// <summary>
/// A reusable modal dialog for arrow-key menu selection in Terminal.Gui.
/// </summary>
/// <typeparam name="T">The type of value returned by the selected menu option.</typeparam>
public sealed class TuiMenuDialog<T> : Dialog
{
    private readonly List<(string Label, T Value)> _options;
    private readonly T? _cancelValue;
    private T? _selectedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TuiMenuDialog{T}"/> class.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="options">The menu options as (label, value) pairs.</param>
    /// <param name="cancelValue">The value to return if the user cancels (default(T) if null).</param>
    public TuiMenuDialog(string title, IEnumerable<(string Label, T Value)> options, T? cancelValue = default)
    {
        Title = title;
        _options = options.ToList();
        _cancelValue = cancelValue;
        _selectedValue = cancelValue;

        // Create ListView with option labels
        var listView = new ListView(_options.Select(o => o.Label).ToList())
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            AllowsMarking = false,
            CanFocus = true
        };

        listView.OpenSelectedItem += args =>
        {
            _selectedValue = _options[listView.SelectedItem].Value;
            Application.RequestStop();
        };

        // Add Cancel button
        var cancelButton = new Button("Cancel")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(listView)
        };
        cancelButton.Clicked += () =>
        {
            _selectedValue = _cancelValue;
            Application.RequestStop();
        };

        Add(listView, cancelButton);

        // Set initial focus to list
        listView.SetFocus();
    }

    /// <summary>
    /// Gets the selected value after the dialog closes.
    /// </summary>
    public T? SelectedValue => _selectedValue;

    /// <summary>
    /// Shows the dialog modally and returns the selected value.
    /// </summary>
    /// <returns>The selected value, or the cancel value if cancelled.</returns>
    public T? ShowAndGetResult()
    {
        Application.Run(this);
        return _selectedValue;
    }
}

/// <summary>
/// Non-generic helper for creating TuiMenuDialog instances with type inference.
/// </summary>
public static class TuiMenuDialog
{
    /// <summary>
    /// Creates and shows a simple string-based menu dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="options">The menu options.</param>
    /// <param name="cancelValue">The value to return if cancelled.</param>
    /// <returns>The selected option, or cancelValue if cancelled.</returns>
    public static string? Show(string title, IEnumerable<string> options, string? cancelValue = null)
    {
        var dialog = new TuiMenuDialog<string>(
            title,
            options.Select(o => (o, o)),
            cancelValue
        );
        return dialog.ShowAndGetResult();
    }

    /// <summary>
    /// Creates and shows an indexed menu dialog (returns 1-based index, 0 for cancel).
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="options">The menu options.</param>
    /// <returns>The 1-based index of the selected option, or 0 if cancelled.</returns>
    public static int ShowIndexed(string title, IEnumerable<string> options)
    {
        var optionsList = options.ToList();
        var indexedOptions = optionsList.Select((label, idx) => (label, idx + 1));
        var dialog = new TuiMenuDialog<int>(
            title,
            indexedOptions,
            cancelValue: 0
        );
        return dialog.ShowAndGetResult();
    }

    /// <summary>
    /// Creates and shows a Yes/No confirmation dialog.
    /// </summary>
    /// <param name="prompt">The confirmation prompt.</param>
    /// <returns>True if Yes selected, false if No or cancelled.</returns>
    public static bool ShowConfirm(string prompt)
    {
        var dialog = new TuiMenuDialog<bool>(
            prompt,
            new[] { ("Yes", true), ("No", false) },
            cancelValue: false
        );
        return dialog.ShowAndGetResult();
    }
}
