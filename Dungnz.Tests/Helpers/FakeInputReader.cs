using Dungnz.Engine;

namespace Dungnz.Tests.Helpers;

public class FakeInputReader : IInputReader
{
    private readonly Queue<string> _inputs;

    public FakeInputReader(params string[] inputs)
    {
        _inputs = new Queue<string>(inputs);
    }

    public string? ReadLine() => _inputs.Count > 0 ? _inputs.Dequeue() : "quit";

    /// <summary>
    /// Stub implementation — tests drive menus via numbered <see cref="ReadLine"/> inputs
    /// and never need real keypresses.
    /// </summary>
    public ConsoleKeyInfo? ReadKey() => null;

    /// <summary>
    /// Always non-interactive in tests — menus fall back to numbered text prompts.
    /// </summary>
    public bool IsInteractive => false;
}
