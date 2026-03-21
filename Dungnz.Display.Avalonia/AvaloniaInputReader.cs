using System.Threading;
using Avalonia.Threading;
using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Models;

namespace Dungnz.Display.Avalonia;

/// <summary>
/// Avalonia implementation of <see cref="IInputReader"/> that bridges the game thread
/// (which blocks on <see cref="ReadLine"/>) with the Avalonia UI thread via
/// <see cref="TaskCompletionSource{T}"/>. The player types in the
/// <see cref="InputPanelViewModel"/> TextBox and presses Enter; the resulting text
/// unblocks the waiting game thread.
/// </summary>
public class AvaloniaInputReader : IInputReader
{
    private readonly InputPanelViewModel _inputVM;
    private TaskCompletionSource<string?>? _pendingLine;

    /// <summary>
    /// Creates a new <see cref="AvaloniaInputReader"/> wired to the given input panel view model.
    /// </summary>
    /// <param name="inputVM">The view model whose <see cref="InputPanelViewModel.InputSubmitted"/>
    /// event signals that the player has submitted a command.</param>
    public AvaloniaInputReader(InputPanelViewModel inputVM)
    {
        ArgumentNullException.ThrowIfNull(inputVM);
        _inputVM = inputVM;
        _inputVM.InputSubmitted += OnInputSubmitted;
    }

    /// <summary>
    /// Blocks the calling (game) thread until the player types a line and presses Enter
    /// in the Avalonia input panel.
    /// </summary>
    /// <returns>The trimmed command string, or <see langword="null"/> if blank.</returns>
    public string? ReadLine()
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingLine = tcs;

        // Enable the input TextBox on the UI thread
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _inputVM.PromptText = "> ";
            _inputVM.CommandText = "";
            _inputVM.IsInputEnabled = true;
        });

        // Block game thread until user submits
        var result = tcs.Task.GetAwaiter().GetResult();
        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    /// <summary>
    /// Reads a single keypress. Not yet implemented for Avalonia — returns <see langword="null"/>
    /// so the game falls back to numbered text prompts (P6 will add key navigation).
    /// </summary>
    public ConsoleKeyInfo? ReadKey() => null;

    /// <summary>
    /// Returns <see langword="false"/> until P6 adds key-based navigation support.
    /// When false, the game uses numbered text prompts instead of arrow-key menus.
    /// </summary>
    public bool IsInteractive => false;

    private void OnInputSubmitted(string text)
    {
        var pending = Interlocked.Exchange(ref _pendingLine, null);
        pending?.TrySetResult(text);
    }
}
