using Dungnz.Engine;

namespace Dungnz.Tests.Helpers;

/// <summary>
/// Test double for <see cref="IMenuNavigator"/>. Accepts pre-scripted selections
/// via a queue of 0-based indices. Never touches the terminal.
/// </summary>
public sealed class FakeMenuNavigator : IMenuNavigator
{
    private readonly Queue<int>  _selections    = new();
    private readonly Queue<bool> _confirmations = new();

    /// <summary>Enqueues a 0-based index to be returned on the next Select call.</summary>
    public FakeMenuNavigator EnqueueSelection(int index)
    {
        _selections.Enqueue(index);
        return this;
    }

    /// <summary>Enqueues a bool to be returned on the next Confirm call.</summary>
    public FakeMenuNavigator EnqueueConfirm(bool value)
    {
        _confirmations.Enqueue(value);
        return this;
    }

    public T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null)
    {
        var idx = _selections.Count > 0 ? _selections.Dequeue() : 0;
        if (idx < 0 || idx >= options.Count)
            throw new InvalidOperationException(
                $"FakeMenuNavigator: scripted index {idx} is out of range (0–{options.Count - 1}). " +
                $"Options: [{string.Join(", ", options.Select(o => o.Label))}]");
        return options[idx].Value;
    }

    public bool Confirm(string prompt)
        => _confirmations.Count > 0 ? _confirmations.Dequeue() : false;
}
