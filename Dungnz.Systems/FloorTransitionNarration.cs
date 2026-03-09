namespace Dungnz.Systems;

/// <summary>
/// Sequential dramatic lines shown when the player descends to a new floor.
/// Each entry is a 5-line sequence displayed in order before the floor loads.
/// </summary>
public static class FloorTransitionNarration
{
    /// <summary>Descent into Floor 1: the first step into the dungeon.</summary>
    public static readonly string[] ToFloor1 =
    {
        "The entrance seals behind you. Or you imagined it. Hard to say.",
        "The air changes immediately — damp, hot, smelling of animal and old smoke.",
        "Torchlight trembles on walls carved by something that didn't care about straight lines.",
        "Somewhere below, something laughs. It doesn't know you're here yet.",
        "Floor 1. The dungeon begins here. So does everything that follows.",
    };

    /// <summary>Descent into Floor 2: deeper, torch smoke thickens.</summary>
    public static readonly string[] ToFloor2 =
    {
        "The goblin noise fades above you.",
        "The stairs are older here -- chiselled, not hacked.",
        "Torch smoke thickens. The air grows heavier with every step.",
        "Something cold and patient waits below, stripped of everything soft.",
        "Floor 2. The darkness here smells of dust and forgotten names.",
    };

    /// <summary>Descent into Floor 3: armory smell, clanging metal in the distance.</summary>
    public static readonly string[] ToFloor3 =
    {
        "The cold bone-smell of the catacombs gives way to something warmer. Worse.",
        "Iron and old blood. The smell of an armory left to rot.",
        "Clanging metal, distant -- like something testing its chains.",
        "Something large moves in the dark below. You hear it breathing first.",
        "Floor 3. Whatever lives here is very much alive.",
    };

    /// <summary>Descent into Floor 4: dead-air smell of sealed crypts.</summary>
    public static readonly string[] ToFloor4 =
    {
        "The stench of sealed crypts hits you before the cold does.",
        "Dead air -- the kind that has not moved in centuries.",
        "The torchlight does not carry down here the way it should.",
        "Reality is getting loose at the edges. The walls do not feel solid.",
        "Floor 4. The shadows move when nothing else does.",
    };

    /// <summary>Descent into Floor 5: bones in patterns -- first necromantic sign.</summary>
    public static readonly string[] ToFloor5 =
    {
        "The first sign: bones, arranged in patterns. Not randomly fallen.",
        "Necromantic runes scratched into the stair risers. Someone was marking the way.",
        "The air arrives scorched, as if the dark itself is on fire.",
        "Your chest tightens -- not from fear. From heat. The stones are warm.",
        "Floor 5. There is no floor after this one that you have heard of.",
    };

    /// <summary>Descent into Floor 6: Lich domain, temperature drops unnaturally.</summary>
    public static readonly string[] ToFloor6 =
    {
        "The temperature drops unnaturally. Not cold -- wrongly cold.",
        "This is the Lich domain proper. You feel it before you see anything.",
        "The last warmth of the Forge fades behind you, replaced by silence.",
        "The stairs here were carved by hands that had stopped caring about the living.",
        "You step through and reality becomes negotiable.",
    };

    /// <summary>Descent into Floor 7: ground becomes warm, lava glow visible ahead.</summary>
    public static readonly string[] ToFloor7 =
    {
        "The ground is warm. Faintly, undeniably warm.",
        "A lava glow bleeds through cracks in the stairwell walls.",
        "The void thins into architecture -- bone architecture, massive and deliberate.",
        "Sulfur on the air. The smell of something geological and ancient.",
        "Whoever built this palace spent lifetimes on it. They had help.",
    };

    /// <summary>Descent into Floor 8: oppressive heat, demonic whispers.</summary>
    public static readonly string[] ToFloor8 =
    {
        "Oppressive heat. Your armour is warm to the touch.",
        "Demonic whispers filter up through the stone. Words without language.",
        "The final staircase does not descend. It falls.",
        "The air screams around you as you drop into the heart of it.",
        "You can hear it. You can feel it. You still descend.",
    };

    /// <summary>Returns the transition sequence for the given target floor, or empty if none exists.</summary>
    public static string[] GetSequence(int targetFloor) => targetFloor switch
    {
        1 => ToFloor1,
        2 => ToFloor2,
        3 => ToFloor3,
        4 => ToFloor4,
        5 => ToFloor5,
        6 => ToFloor6,
        7 => ToFloor7,
        8 => ToFloor8,
        _ => Array.Empty<string>()
    };

    /// <summary>Returns brief atmospheric lines shown when the player ascends back to a previous floor.</summary>
    public static string[] GetAscendSequence(int targetFloor) => targetFloor switch
    {
        1 => ["The suffocating dark lifts. The familiar smell of surface stone reaches you.", "Your footsteps echo lighter here. You remember this place."],
        2 => ["The air grows cooler and less oppressive as you climb.", "The torch smoke is thinner. The sounds from below fade behind you."],
        3 => ["The iron smell recedes. A colder silence wraps around you.", "You leave the warmth of the forge levels behind."],
        4 => ["The dead air of the sealed crypts eases. Something feels less wrong.", "Reality snaps back into place, just slightly."],
        5 => ["The necromantic hum fades as you ascend. The bones no longer arranged.", "The scorched air gives way to something colder, older."],
        6 => ["The unnatural cold retreats. You are leaving the Lich domain.", "A fragment of warmth returns, though the dark remains."],
        7 => ["The lava glow fades to darkness behind you. The ground cools beneath your feet.", "The sulfur smell dissipates. The architecture grows less deliberate."],
        _ => Array.Empty<string>()
    };
}
