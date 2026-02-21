namespace Dungnz.Models;

/// <summary>
/// Represents a single room in the dungeon. Holds navigational connections to adjacent rooms,
/// the enemy or items present, and state flags that track player interaction (visited, looted,
/// shrine used). Rooms are linked at dungeon-generation time via <see cref="Exits"/>.
/// </summary>
public class Room
{
    /// <summary>Gets or sets the unique identifier for this room, assigned automatically on creation.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the narrative text shown to the player when they enter or inspect this room.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directional connections leading out of this room. Each entry maps a
    /// <see cref="Direction"/> to the adjacent <see cref="Room"/> reachable in that direction.
    /// </summary>
    public Dictionary<Direction, Room> Exits { get; set; } = new();

    /// <summary>
    /// Gets or sets the enemy currently occupying this room, or <c>null</c> if the room is clear.
    /// Set to <c>null</c> after the enemy is defeated.
    /// </summary>
    public Enemy? Enemy { get; set; }

    /// <summary>
    /// Gets or sets the list of items lying on the floor of this room that the player can pick up.
    /// Items are removed from this list when the player takes them.
    /// </summary>
    public List<Item> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this room is the dungeon's exit. Reaching an exit room with no living
    /// enemy triggers the win condition.
    /// </summary>
    public bool IsExit { get; set; }

    /// <summary>
    /// Gets or sets whether the player has previously entered this room. Used to suppress the
    /// full room description on re-entry and to render the mini-map.
    /// </summary>
    public bool Visited { get; set; }

    /// <summary>
    /// Gets or sets whether the player has already collected the items from this room's floor,
    /// preventing duplicate loot prompts.
    /// </summary>
    public bool Looted { get; set; }

    /// <summary>
    /// Gets or sets whether this room contains a healing shrine the player can interact with.
    /// </summary>
    public bool HasShrine { get; set; }

    /// <summary>
    /// Gets or sets whether the healing shrine in this room has already been activated.
    /// Shrines can only be used once per dungeon run.
    /// </summary>
    public bool ShrineUsed { get; set; }
}
