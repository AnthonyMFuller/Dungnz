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

    /// <summary>Descent into Floor 6: the Void Antechamber.</summary>
    public static readonly string[] ToFloor6 =
    {
        "You descend past the last warmth. The Forge's heat fades behind you, replaced by something worse: silence.",
        "The stairs here were carved by hands that had stopped caring about the living.",
        "You step through and the air changes — reality becomes negotiable."
    };

    /// <summary>Descent into Floor 7: the Bone Palace.</summary>
    public static readonly string[] ToFloor7 =
    {
        "The void thins into architecture. Bone architecture — massive, deliberate, obscene in its craftsmanship.",
        "Whoever built this palace spent lifetimes on it. They had help. The help was not willing."
    };

    /// <summary>Descent into Floor 8: the Final Sanctum.</summary>
    public static readonly string[] ToFloor8 =
    {
        "The final staircase doesn't descend. It falls.",
        "The air screams around you as you drop into the heart of it — the oldest part of the dungeon, where whatever spawned all of this still breathes.",
        "You can hear it. You can feel it. You still descend."
    };

    /// <summary>Returns the transition sequence for the given target floor, or an empty array if no sequence exists.</summary>
    public static string[] GetSequence(int targetFloor) => targetFloor switch
    {
        2 => ToFloor2,
        3 => ToFloor3,
        4 => ToFloor4,
        5 => ToFloor5,
        6 => ToFloor6,
        7 => ToFloor7,
        8 => ToFloor8,
        _ => Array.Empty<string>()
    };
}
