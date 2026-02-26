namespace Dungnz.Systems;

/// <summary>
/// Ambient flavor text pools for combat events (display-only, no mechanic effect).
/// </summary>
public static class CombatNarration
{
    // ── Combat start ────────────────────────────────────────────────────────

    /// <summary>Generic combat-start lines for non-boss, non-undead enemies.</summary>
    public static readonly string[] StartGeneric =
    {
        "You raise your weapon. So does it.",
        "No turning back now.",
        "The air thickens. Combat is inevitable.",
        "You size each other up. Something has to move first.",
        "Every muscle in your body tightens. Here it comes."
    };

    /// <summary>Combat-start lines for undead enemies.</summary>
    public static readonly string[] StartUndead =
    {
        "The dead don't fear you. That's the problem.",
        "It moves with the purpose of something that has nothing left to lose.",
        "No pain. No hesitation. It just comes.",
        "The smell of rot and old magic. Your grip tightens.",
        "Whatever animates it, it isn't mercy."
    };

    // ── Player near-death (HP < 25%) ────────────────────────────────────────

    /// <summary>Atmospheric lines shown when the player's HP drops below 25%.</summary>
    public static readonly string[] NearDeath =
    {
        "Everything hurts. But you're still standing.",
        "One bad hit. Don't take one bad hit.",
        "The edges of your vision blur. Keep moving."
    };

    // ── Killing blow ─────────────────────────────────────────────────────────

    /// <summary>Killing-blow lines for melee classes (Warrior, Paladin).</summary>
    public static readonly string[] KillMelee =
    {
        "It crumples.",
        "It folds under the weight of your strike.",
        "A heavy blow. It doesn't get up."
    };

    /// <summary>Killing-blow lines for ranged classes (Ranger).</summary>
    public static readonly string[] KillRanged =
    {
        "The arrow finds its mark.",
        "Clean shot. Over before it knew.",
        "Your aim holds true. It drops."
    };

    /// <summary>Killing-blow lines for magic classes (Mage, Necromancer).</summary>
    public static readonly string[] KillMagic =
    {
        "It simply stops.",
        "The magic tears through it. Nothing remains.",
        "Reality itself dismisses it."
    };

    /// <summary>Killing-blow lines for Rogue and generic fallback.</summary>
    public static readonly string[] KillGeneric =
    {
        "It's over.",
        "Down. It's not getting back up.",
        "The fight ends here.",
        "One last blow. That's all it took."
    };

    // ── Critical hit flavor ───────────────────────────────────────────────────

    /// <summary>Brief atmospheric lines shown on a critical hit.</summary>
    public static readonly string[] CritFlavor =
    {
        "Clean hit.",
        "That one landed perfectly.",
        "Right where it hurts."
    };

    // ── Enemy special attack (format: {0} = enemy name, {1} = ability name) ─

    /// <summary>
    /// Atmospheric lines for enemy special-ability announcements.
    /// Use <see cref="NarrationService.Pick(string[], object[])"/> with
    /// args: enemy name, ability/attack name.
    /// </summary>
    public static readonly string[] EnemySpecialAttack =
    {
        "{0} rears back and unleashes {1}!",
        "{0} draws on dark power — {1}!",
        "{0} gathers itself and strikes with {1}!"
    };
}
