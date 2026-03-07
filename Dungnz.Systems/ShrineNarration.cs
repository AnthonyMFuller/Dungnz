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
    // Type-specific shrine blessing lines

    /// <summary>Lines shown when a healing shrine fully restores the player's HP.</summary>
    public static readonly string[] GrantHeal =
    {
        "The stone bleeds warmth. Your wounds close like they were never there.",
        "Something old and merciful passes through you. The pain recedes.",
        "The shrine's light seeps into your skin. You breathe easier.",
        "Flesh knits. Blood stills. The shrine asks nothing in return. Nothing visible.",
        "Whatever god this was built for — it was the merciful kind."
    };

    /// <summary>Lines shown when a power shrine blesses the player with ATK/DEF.</summary>
    public static readonly string[] GrantPower =
    {
        "The runes flare. Your hands feel steadier. Your strike will be truer.",
        "Something ancient sharpens inside you. A gift — or a debt.",
        "The shrine's power settles into your bones. Quietly. Permanently.",
        "You feel the difference immediately. Harder to hurt. Harder to miss.",
        "Old prayers for old warriors. You aren't sure you're worthy. The shrine disagrees."
    };

    /// <summary>Lines shown when a protection shrine fortifies the player's MaxHP.</summary>
    public static readonly string[] GrantProtection =
    {
        "Your chest expands. Something has decided you can take more.",
        "The shrine reinforces you from within. Stone and faith, braided together.",
        "You feel it in your ribs — a quiet, permanent refusal to fall.",
        "Endurance, granted by something that has endured far longer than you.",
        "The light wraps around you once, tight. Then fades. It left something behind."
    };

    /// <summary>Lines shown when a wisdom shrine expands the player's MaxMana.</summary>
    public static readonly string[] GrantWisdom =
    {
        "The runes whisper in a language older than the dungeon. You understand it.",
        "Your mind opens — just slightly, just enough. The magic flows deeper now.",
        "Something vast reaches into your thoughts and makes room.",
        "Knowledge, or the capacity for it. Either way, the well is deeper.",
        "The shrine's light enters behind your eyes. The world looks slightly wider."
    };

    /// <summary>Lines shown when the player leaves the shrine without using it.</summary>
    public static readonly string[] GrantNothing =
    {
        "The shrine watches you leave. It has waited this long.",
        "Its light dims slightly as you turn away. Perhaps it was judging you. Perhaps not."
    };
}