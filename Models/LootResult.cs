namespace TextGame.Models;

public readonly struct LootResult
{
    public Item? Item { get; init; }
    public int Gold { get; init; }
}
