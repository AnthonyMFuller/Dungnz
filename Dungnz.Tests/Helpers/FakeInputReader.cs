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
}
