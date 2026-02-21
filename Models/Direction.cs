namespace Dungnz.Models;

/// <summary>
/// Represents the four cardinal directions used to navigate between <see cref="Room"/> instances
/// in the dungeon. Stored as keys in <see cref="Room.Exits"/> to describe available passages.
/// </summary>
public enum Direction
{
    /// <summary>The passage leading northward (upward on the map grid) to an adjacent room.</summary>
    North,

    /// <summary>The passage leading southward (downward on the map grid) to an adjacent room.</summary>
    South,

    /// <summary>The passage leading eastward (rightward on the map grid) to an adjacent room.</summary>
    East,

    /// <summary>The passage leading westward (leftward on the map grid) to an adjacent room.</summary>
    West
}
