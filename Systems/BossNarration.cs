namespace Dungnz.Systems;

/// <summary>
/// Provides multi-line dramatic introductions, death narration, and phase-trigger lines
/// for each boss variant. Intros are returned in order and should be displayed
/// sequentially as a build-up, not picked at random.
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
        ["Goblin Warchief"] = new[]
        {
            "\"You there! Kill it dead, boyz!\" The warchief levels a rusted cleaver.",
            "Behind him, goblins scramble."
        },
        ["Plague Hound Alpha"] = new[]
        {
            "The beast circles you, foam at its maw.",
            "It smells blood from floors away."
        },
        ["Iron Sentinel"] = new[]
        {
            "The construct turns, joints grinding.",
            "No command given. It simply acts."
        },
        ["Bone Archon"] = new[]
        {
            "\"Join the collection.\" Its voice is borrowed — stitched from a dozen throats."
        },
        ["Crimson Vampire"] = new[]
        {
            "\"You smell... extraordinary.\" She tilts her head.",
            "\"I'll try to make it quick.\""
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
            "\"Another mortal challenger. How... nostalgic.\"",
            "It floats forward without moving its legs."
        },
        ["Abyssal Leviathan"] = new[]
        {
            "The water churns. Then the chamber walls move.",
            "No — that's it. All of it."
        },
        ["Infernal Dragon"] = new[]
        {
            "It doesn't roar. It waits, watching you with one ancient eye.",
            "Then it breathes."
        }
    };

    private static readonly Dictionary<string, string> _deaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dungeon Boss"] = "The boss collapses with a thunderous crash. The dungeon grows quiet.",
        ["Goblin Warchief"] = "He gurgles something that might have been an order.",
        ["Plague Hound Alpha"] = "It collapses mid-lunge, a final wet cough rattling through it.",
        ["Iron Sentinel"] = "The glow fades from its eye-slits. It stands still for a moment, then falls.",
        ["Bone Archon"] = "The bones scatter. The voices stop.",
        ["Crimson Vampire"] = "She turns to ash before she hits the ground.",
        ["Lich King"] = "The Lich King releases a terrible shriek as his phylactery shatters. The necromantic energies binding him dissolve into ash. The cold lifts.",
        ["Stone Titan"] = "The Stone Titan fractures from within, cracks of golden light racing across its body. It shatters into gravel with a sound like a collapsing cliff face.",
        ["Shadow Wraith"] = "The Shadow Wraith screams a sound that exists only inside your skull. Then — nothing. The torches relight. Whatever it was, it's gone.",
        ["Vampire Lord"] = "The Vampire Lord staggers, clutching the wound you've dealt. A look of genuine surprise crosses its ancient face. It crumbles to dust before it hits the floor.",
        ["Archlich Sovereign"] = "\"This is not... the end...\" The phylactery will not be found.",
        ["Abyssal Leviathan"] = "The tentacles go slack. The water turns still and dark.",
        ["Infernal Dragon"] = "The fire dies. The silence is almost worse."
    };

    private static readonly Dictionary<string, string> _phases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin Warchief"] = "\"BOYZ! BOYZ TO ME!\" He bellows across the chamber.",
        ["Plague Hound Alpha"] = "Wounds glowing sickly, it screams and lunges faster.",
        ["Iron Sentinel"] = "Joints locking, it winds back and delivers a blow that shakes the floor.",
        ["Bone Archon"] = "It raises a hand, and the air grows cold and heavy.",
        ["Crimson Vampire"] = "She hisses and lunges with inhuman desperation. \"Give me that life.\"",
        ["Archlich Sovereign"] = "A shroud of death unfolds from its outstretched arms.",
        ["Abyssal Leviathan"] = "Multiple limbs crash down in rapid succession — there's no predicting them.",
        ["Infernal Dragon"] = "It inhales slowly. The air itself begins to char."
    };

    private static readonly string[] _defaultIntro = { "The ground shakes.", "Something enormous stirs.", "A dungeon boss emerges!" };
    private static readonly string _defaultDeath = "The boss falls with a thunderous crash.";
    private static readonly string _defaultPhase = "It reacts to its wounds with sudden, violent desperation.";

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

    /// <summary>
    /// Returns the phase-trigger narration line for the named boss, displayed when an
    /// HP threshold is crossed and the boss's phase ability fires.
    /// </summary>
    /// <param name="bossName">The <see cref="Dungnz.Models.Enemy.Name"/> of the boss.</param>
    /// <returns>A single dramatic phase-trigger narration string.</returns>
    public static string GetPhase(string bossName) =>
        _phases.GetValueOrDefault(bossName, _defaultPhase);
}
