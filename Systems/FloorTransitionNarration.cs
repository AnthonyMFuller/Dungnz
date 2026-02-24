namespace Dungnz.Systems;

/// <summary>
/// Sequential dramatic lines shown when the player descends to a new floor.
/// Each entry is a 2-3 line sequence displayed in order before the floor loads.
/// </summary>
public static class FloorTransitionNarration
{
    /// <summary>Descent into Floor 2: the Skeleton Catacombs.</summary>
    public static readonly string[] ToFloor2 =
    {
        "The goblin noise fades above you.",
        "Something older waits below — cold, patient, stripped of everything soft.",
        "Floor 2. The air smells of dust and forgotten names."
    };

    /// <summary>Descent into Floor 3: the Troll Warrens.</summary>
    public static readonly string[] ToFloor3 =
    {
        "The cold bone-smell of the catacombs gives way to something warmer. Worse.",
        "Something large moves in the dark below. You hear it breathing before you see anything.",
        "Floor 3. Whatever lives here is very much alive."
    };

    /// <summary>Descent into Floor 4: the Shadow Realm.</summary>
    public static readonly string[] ToFloor4 =
    {
        "The torchlight doesn't carry down here the way it should.",
        "The walls look solid. They don't feel solid. Reality is getting loose at the edges.",
        "Floor 4. The shadows move when nothing else does."
    };

    /// <summary>Descent into Floor 5: the Dragon's Lair.</summary>
    public static readonly string[] ToFloor5 =
    {
        "The air arrives scorched, as if the dark itself is on fire.",
        "Your chest tightens. Not from fear — from heat. The stones are warm under your boots.",
        "Floor 5. There is no floor after this one."
    };

    /// <summary>Returns the transition sequence for the given target floor, or an empty array if no sequence exists.</summary>
    public static string[] GetSequence(int targetFloor) => targetFloor switch
    {
        2 => ToFloor2,
        3 => ToFloor3,
        4 => ToFloor4,
        5 => ToFloor5,
        _ => Array.Empty<string>()
    };
}
