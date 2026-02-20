namespace Dungnz.Engine;

public interface IInputReader
{
    string? ReadLine();
}

public class ConsoleInputReader : IInputReader
{
    public string? ReadLine() => Console.ReadLine();
}
