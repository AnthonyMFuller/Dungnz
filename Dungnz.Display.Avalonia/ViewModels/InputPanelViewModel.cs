using CommunityToolkit.Mvvm.ComponentModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the input panel showing command prompt and accepting user commands.
/// Raises <see cref="InputSubmitted"/> when the player presses Enter.
/// </summary>
public partial class InputPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _commandText = "";

    [ObservableProperty]
    private string _promptText = ">";

    [ObservableProperty]
    private bool _isInputEnabled;

    /// <summary>
    /// Raised on the UI thread when the player submits a command (presses Enter).
    /// The argument is the trimmed command text.
    /// </summary>
    public event Action<string>? InputSubmitted;

    /// <summary>
    /// Submits the current <see cref="CommandText"/>, raises <see cref="InputSubmitted"/>,
    /// then clears the text box and disables input.
    /// </summary>
    public void Submit()
    {
        var text = CommandText.Trim();
        CommandText = "";
        IsInputEnabled = false;
        InputSubmitted?.Invoke(text);
    }
}
