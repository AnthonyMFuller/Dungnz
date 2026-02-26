namespace Dungnz.Systems;

/// <summary>
/// Provides multi-line dramatic introductions and unique death narration for each
/// boss variant. Intros are returned in order and should be displayed sequentially
/// as a build-up, not picked at random.
/// </summary>
public static class BossNarration
{
    private static readonly Dictionary<string, string[]> _intros = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dungeon Boss"] = new[]
        {
            "The ground shakes.",
            "Something ancient and terrible stirs in the darkness ahead.",
            "The dungeon boss emerges — this is what you came for."
        },
        ["Lich King"] = new[]
        {
            "The temperature plummets. Your breath fogs in the frigid air.",
            "Blue-green flames flicker to life in empty eye sockets as a robed figure rises from a stone throne.",
            "The Lich King regards you with the cold patience of ten thousand years. 'Another mortal comes to fail.'"
        },
        ["Stone Titan"] = new[]
        {
            "The floor cracks under something impossibly heavy.",
            "A shape like a walking mountain resolves from the shadows — granite skin, fists like boulders.",
            "The Stone Titan turns its featureless face toward you. The ceiling groans under its presence."
        },
        ["Shadow Wraith"] = new[]
        {
            "The torches go out.",
            "In the absolute darkness, something breathes — slow, cold, hateful.",
            "The Shadow Wraith coalesces from the dark itself, a wound in reality shaped like a predator."
        },
        ["Vampire Lord"] = new[]
        {
            "A figure sits at the far end of the chamber, utterly still.",
            "It has been waiting for you. Perhaps for centuries. It smiles, and its teeth are wrong.",
            "'At last,' the Vampire Lord says, 'something worth the evening.'"
        },
        ["Archlich Sovereign"] = new[]
        {
            "The air turns to ash. From the shadows coalesces a figure of impossible age — bone wrapped in void, crowned with the weight of a thousand stolen years.",
            "The Archlich Sovereign does not speak. It simply raises one skeletal hand, and the dead begin to stir.",
            "'You have come far, morsel. Further than most. But this is where all journeys end.'"
        },
        ["Abyssal Leviathan"] = new[]
        {
            "The chamber floods in seconds. From the black water below erupts something vast and wrong — scales the color of deep ocean, eyes like collapsed stars, a body that shouldn't fit in the space it occupies.",
            "The Abyssal Leviathan's roar shakes the bones of the world.",
            "You realize, suddenly, that the water didn't flood the room. The room IS the creature."
        },
        ["Infernal Dragon"] = new[]
        {
            "The Final Sanctum ignites. The creature that drops from the ceiling is made of fire given hunger, of hatred given wings.",
            "The Infernal Dragon exhales once, and the temperature doubles.",
            "It looks at you the way a furnace looks at kindling. It has been waiting. Not for a hero — for fuel."
        }
    };

    private static readonly Dictionary<string, string> _deaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dungeon Boss"] = "The boss collapses with a thunderous crash. The dungeon grows quiet.",
        ["Lich King"] = "The Lich King releases a terrible shriek as his phylactery shatters. The necromantic energies binding him dissolve into ash. The cold lifts.",
        ["Stone Titan"] = "The Stone Titan fractures from within, cracks of golden light racing across its body. It shatters into gravel with a sound like a collapsing cliff face.",
        ["Shadow Wraith"] = "The Shadow Wraith screams a sound that exists only inside your skull. Then — nothing. The torches relight. Whatever it was, it's gone.",
        ["Vampire Lord"] = "The Vampire Lord staggers, clutching the wound you've dealt. A look of genuine surprise crosses its ancient face. It crumbles to dust before it hits the floor.",
        ["Archlich Sovereign"] = "The Archlich Sovereign releases a keening shriek as its phylactery fractures. The void that sustained it unravels — threads of stolen centuries dissolving into the dark.",
        ["Abyssal Leviathan"] = "The Leviathan convulses, its vast body thrashing the chamber. The water recedes as the creature's will fails. The room is just a room again. Just wet stone and silence.",
        ["Infernal Dragon"] = "The Infernal Dragon lets out one final, hollow roar — then the fire goes out. In the sudden dark and cold, the silence is absolute. You've done it. You've reached the end."
    };

    private static readonly string[] _defaultIntro = { "The ground shakes.", "Something enormous stirs.", "A dungeon boss emerges!" };
    private static readonly string _defaultDeath = "The boss falls with a thunderous crash.";

    /// <summary>
    /// Returns the ordered introduction lines for the named boss. Lines should be
    /// displayed sequentially to build dramatic tension, not selected at random.
    /// </summary>
    /// <param name="bossName">The <see cref="Dungnz.Models.Enemy.Name"/> of the boss.</param>
    /// <returns>An array of narration strings in display order.</returns>
    public static string[] GetIntro(string bossName) =>
        _intros.GetValueOrDefault(bossName, _defaultIntro);

    /// <summary>
    /// Returns the unique death narration line for the named boss.
    /// </summary>
    /// <param name="bossName">The <see cref="Dungnz.Models.Enemy.Name"/> of the boss.</param>
    /// <returns>A single dramatic death narration string.</returns>
    public static string GetDeath(string bossName) =>
        _deaths.GetValueOrDefault(bossName, _defaultDeath);
}
