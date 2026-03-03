namespace Dungnz.Models;

/// <summary>Discriminated union returned by <see cref="Display.IDisplayService.ShowTakeMenuAndSelect"/>.</summary>
public abstract record TakeSelection
{
    private TakeSelection() { }

    /// <summary>The player chose to take every item in the room.</summary>
    public sealed record All : TakeSelection;

    /// <summary>The player chose a specific item.</summary>
    public sealed record Single(Item Item) : TakeSelection;
}
