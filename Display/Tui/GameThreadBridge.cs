using System.Collections.Concurrent;
using Terminal.Gui;

namespace Dungnz.Display.Tui;

/// <summary>
/// Coordinates dual-thread communication between Terminal.Gui (main thread)
/// and the game logic thread (background thread).
/// </summary>
/// <remarks>
/// Terminal.Gui requires Application.Run() on the main thread. The game loop
/// is a blocking synchronous loop that cannot run on the UI thread without
/// deadlocking. This bridge enables:
/// <list type="bullet">
/// <item>Game thread → UI thread: via Application.Invoke() for all display updates</item>
/// <item>UI thread → Game thread: via TaskCompletionSource or BlockingCollection for input</item>
/// </list>
/// </remarks>
public sealed class GameThreadBridge
{
    private readonly BlockingCollection<string> _commandQueue = new();

    /// <summary>
    /// Posts a command from the UI thread to the game thread.
    /// Called when the user types a command in the TUI input field.
    /// </summary>
    /// <param name="command">The command text to send to the game loop.</param>
    public void PostCommand(string command)
    {
        _commandQueue.Add(command);
    }

    /// <summary>
    /// Waits for the next command from the UI thread.
    /// Called by the game thread when it needs player input.
    /// Blocks until a command is available.
    /// </summary>
    /// <returns>The next command from the player, or null if the queue is closed.</returns>
    public string? WaitForCommand()
    {
        try
        {
            return _commandQueue.Take();
        }
        catch (InvalidOperationException)
        {
            // Queue was completed/closed
            return null;
        }
    }

    /// <summary>
    /// Signals that no more commands will be posted.
    /// Should be called when the UI is shutting down.
    /// </summary>
    public void Complete()
    {
        _commandQueue.CompleteAdding();
    }

    /// <summary>
    /// Marshals a UI update action to the Terminal.Gui main thread.
    /// Safe to call from any thread.
    /// </summary>
    /// <param name="action">The action to execute on the UI thread.</param>
    public static void InvokeOnUiThread(Action action)
    {
        Application.MainLoop?.Invoke(action);
    }

    /// <summary>
    /// Marshals a UI update and waits for it to complete synchronously.
    /// Blocks the calling thread until the action completes on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute on the UI thread.</param>
    public static void InvokeOnUiThreadAndWait(Action action)
    {
        if (Application.MainLoop is null)
        {
            action();
            return;
        }
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Application.MainLoop.Invoke(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Marshals a UI query to the main thread and returns the result.
    /// Blocks the calling thread until the query completes on the UI thread.
    /// </summary>
    /// <typeparam name="T">The return type of the query.</typeparam>
    /// <param name="func">The function to execute on the UI thread.</param>
    /// <returns>The result of the query.</returns>
    public static T InvokeOnUiThreadAndWait<T>(Func<T> func)
    {
        if (Application.MainLoop is null)
        {
            return func();
        }
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Application.MainLoop.Invoke(() =>
        {
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task.GetAwaiter().GetResult();
    }
}
