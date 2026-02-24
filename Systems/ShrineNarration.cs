namespace Dungnz.Systems;

/// <summary>
/// Flavor text pools for shrine encounters and activations.
/// </summary>
public static class ShrineNarration
{
    // Shown when a shrine is present in the room (before use)
    /// <summary>Lines shown when a shrine is present in the room (before use).</summary>
    public static readonly string[] Presence =
    {
        "In the corner, something carved from stone pulses with faint light. You shouldn't trust it, but you probably will.",
        "A shrine stands here, cold and deliberate. It's been waiting.",
        "The walls around the shrine are cleaner than they should be. Something maintains this.",
        "A pale glow traces runes you don't recognize. They seem pleased you've arrived.",
        "The air near the shrine is still in a way that air shouldn't be. It knows you're here.",
        "An altar of dark stone hums at a frequency you feel more than hear.",
        "Old magic clings to this corner of the dungeon like a stain that won't wash out.",
        "The shrine's light doesn't flicker. Nothing down here burns that steady.",
        "Whoever built this didn't build it for you. You'll use it anyway.",
        "A carved figure squats at the shrine's base, watching the doorway with blank stone eyes.",
        "The runes rearrange slightly each time you look away. You're almost certain of it.",
        "Something carved from grief and older stone. It offers comfort. The terms are unclear."
    };

    // Shown when player activates the shrine
    /// <summary>Lines shown when the player activates the shrine.</summary>
    public static readonly string[] UseShrine =
    {
        "The light passes through you. Something is restored.",
        "Cold comfort, warmly given. You feel better. Somewhat.",
        "The shrine acknowledges you. You're not sure what that means.",
        "It costs something, though not what you expected. You feel different. Arguably better.",
        "A moment of grace in a graceless place. Don't read too much into it.",
        "The hum rises, peaks, fades. The dungeon doesn't care. The shrine does. Just barely.",
        "Power moves through your bones like water through a crack. Old. Not entirely benign.",
        "You take what's offered and try not to think about who offered it."
    };
}
