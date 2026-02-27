namespace Dungnz.Models;

/// <summary>Describes the environmental flavour of a dungeon room.</summary>
public enum RoomType
{
    /// <summary>Represents a standard, unremarkable dungeon room.</summary>
    Standard,
    /// <summary>Represents a darkened room with limited visibility.</summary>
    Dark,
    /// <summary>Represents a damp, moss-covered room.</summary>
    Mossy,
    /// <summary>Represents a partially flooded room with standing water.</summary>
    Flooded,
    /// <summary>Represents a scorched room blackened by fire or magic.</summary>
    Scorched,
    /// <summary>Represents an ancient room lined with old stonework and relics.</summary>
    Ancient,

    /// <summary>A forgotten shrine radiating holy energy — offers blessings to the player.</summary>
    ForgottenShrine,

    /// <summary>A petrified library containing ancient tomes and scrolls.</summary>
    PetrifiedLibrary,

    /// <summary>A contested armory with trapped weapons — risky but potentially rewarding.</summary>
    ContestedArmory,

    /// <summary>A room rigged with a deadly trap — the player must choose how to deal with it.</summary>
    TrapRoom
}

/// <summary>Identifies the specific trap mechanism inside a <see cref="RoomType.TrapRoom"/>.</summary>
public enum TrapVariant
{
    /// <summary>Triggered pressure plates fire a volley of arrows from hidden wall slits.</summary>
    ArrowVolley,
    /// <summary>Ceiling vents release a cloud of choking, poisonous gas.</summary>
    PoisonGas,
    /// <summary>The floor is riddled with stress fractures that collapse under weight.</summary>
    CollapsingFloor
}

/// <summary>Describes environmental hazards that can damage the player on entry.</summary>
public enum HazardType
{
    /// <summary>Represents the absence of a hazard.</summary>
    None,
    /// <summary>Represents a spike trap that deals physical damage on entry.</summary>
    Spike,
    /// <summary>Represents a poison cloud that poisons the player on entry.</summary>
    Poison,
    /// <summary>Represents a fire hazard that deals burn damage on entry.</summary>
    Fire
}

/// <summary>
/// Represents a single room in the dungeon. Holds navigational connections to adjacent rooms,
/// the enemy or items present, and state flags that track player interaction (visited, looted,
/// shrine used). Rooms are linked at dungeon-generation time via <see cref="Exits"/>.
/// </summary>
public class Room
{
    /// <summary>Gets or sets the environmental type of this room, used to add flavour text on entry.</summary>
    public RoomType Type { get; set; } = RoomType.Standard;

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

    /// <summary>Gets or sets the merchant present in this room, or <c>null</c> if none.</summary>
    public Merchant? Merchant { get; set; }

    /// <summary>Gets or sets the environmental hazard in this room that damages the player on entry.</summary>
    public HazardType Hazard { get; set; } = HazardType.None;

    /// <summary>
    /// Gets or sets the specific trap variant for <see cref="RoomType.TrapRoom"/> rooms.
    /// <see langword="null"/> for all other room types.
    /// </summary>
    public TrapVariant? Trap { get; set; }

    /// <summary>
    /// Gets or sets whether the special room interaction (ForgottenShrine blessing,
    /// PetrifiedLibrary tome, or ContestedArmory loot) has already been triggered.
    /// </summary>
    public bool SpecialRoomUsed { get; set; }

    /// <summary>Gets or sets the narrative state of this room: Fresh on first entry, Cleared after its enemy is defeated, Revisited on subsequent entries.</summary>
    public RoomState State { get; set; } = RoomState.Fresh;
}

/// <summary>Tracks the narrative state of a room for flavor text selection.</summary>
public enum RoomState
{
    /// <summary>Room has not yet been entered by the player.</summary>
    Fresh,
    /// <summary>Room has been cleared of its enemy.</summary>
    Cleared,
    /// <summary>Room has been visited before.</summary>
    Revisited
}
