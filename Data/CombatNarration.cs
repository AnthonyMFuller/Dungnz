namespace Dungnz.Data;

/// <summary>
/// Centralised narration message arrays used by the combat engine.
/// Extracted from <c>CombatEngine</c> and merged with ambient flavor text
/// to reduce class bloat and keep narrative content separate from mechanical logic.
/// </summary>
public static class CombatNarration
{
    // ── Player hit messages (generic + class-specific) ──────────────

    /// <summary>Generic player hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] PlayerHitMessages =
    {
        "You strike {0} for {1} damage!",
        "Your blade finds a gap — {1} damage on {0}!",
        "A solid blow connects! {0} takes {1} damage!",
        "You tear through {0}'s guard for {1} damage!",
        "{0} staggers back — {1} damage!"
    };

    /// <summary>Warrior-class hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] WarriorHitMessages =
    {
        "You drive your blade deep — {0} takes {1} damage!",
        "A bone-crunching blow! {0} reels back — {1} damage!",
        "You hammer through {0}'s guard with brute force — {1} damage!",
        "Pure power behind the swing — {0} staggers for {1} damage!",
        "You crash into {0} like a battering ram — {1} damage!"
    };

    /// <summary>Mage-class hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] MageHitMessages =
    {
        "Arcane force tears through {0} — {1} damage!",
        "Eldritch energy crackles as it strikes {0} for {1} damage!",
        "You channel raw magic into a focused bolt — {1} damage to {0}!",
        "Reality bends around your attack — {0} takes {1} damage!",
        "Your spell finds its mark — {1} crackling damage to {0}!"
    };

    /// <summary>Rogue-class hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] RogueHitMessages =
    {
        "You dart in for a precise cut — {0} takes {1} damage!",
        "A lightning-quick strike to the weak point — {1} damage!",
        "You find the gap in {0}'s defenses — {1} damage!",
        "Quick as shadow — {0} barely registers the blow until it hurts. {1} damage!",
        "A surgical strike — {0} bleeds from a wound it didn't see coming. {1} damage!"
    };

    /// <summary>Paladin-class hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] PaladinHitMessages =
    {
        "You bring your holy weapon down upon {0} — {1} damage!",
        "Justice is served — {0} takes {1} damage!",
        "The Light guides your hand — {1} damage to {0}!",
        "A righteous blow strikes {0} for {1} damage!",
        "You smite {0} with holy fury — {1} damage!"
    };

    /// <summary>Necromancer-class hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] NecromancerHitMessages =
    {
        "Necrotic energy flows through your strike — {0} takes {1} damage!",
        "You channel dark power into a blow — {1} damage to {0}!",
        "Death magic crackles as you hit {0} for {1} damage!",
        "Shadow and decay tear through {0} — {1} damage!",
        "You strike with the power of the grave — {1} damage on {0}!"
    };

    /// <summary>Ranger-class hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] RangerHitMessages =
    {
        "A precise strike finds the gap — {0} takes {1} damage!",
        "Hunter's instinct guides your aim — {1} damage to {0}!",
        "You strike with practiced efficiency — {1} damage on {0}!",
        "Years of tracking this prey pay off — {0} takes {1} damage!",
        "Swift and sure — {0} takes {1} damage!"
    };

    // ── Player miss messages (generic + class-specific) ─────────────

    /// <summary>Generic player miss narration. Format: {0} = enemy name.</summary>
    public static readonly string[] PlayerMissMessages =
    {
        "{0} sidesteps your attack!",
        "Your blow glances off harmlessly.",
        "You swing wide — {0} ducks back!",
        "{0} twists away at the last moment!",
        "Your strike finds nothing but air."
    };

    /// <summary>Warrior-class miss narration. Format: {0} = enemy name.</summary>
    public static readonly string[] WarriorMissMessages =
    {
        "You swing with power but {0} isn't where you thought!",
        "Too slow — {0} sidesteps your heavy blow!"
    };

    /// <summary>Mage-class miss narration. Format: {0} = enemy name.</summary>
    public static readonly string[] MageMissMessages =
    {
        "Your spell fizzles at the last moment.",
        "The incantation slips — {0} escapes unscathed!"
    };

    /// <summary>Rogue-class miss narration. Format: {0} = enemy name.</summary>
    public static readonly string[] RogueMissMessages =
    {
        "{0} anticipates your angle — the strike finds nothing.",
        "You dart in but {0} reads your movement!"
    };

    // ── Critical hit messages (generic + class-specific) ────────────

    /// <summary>Generic critical hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] CritMessages =
    {
        "💥 Critical hit! You slam {0} for {1} damage!",
        "💥 Devastating blow! {1} damage to {0}!",
        "💥 Perfect strike — {1} crushing damage!",
        "💥 You find the weak point! {1} damage on {0}!"
    };

    /// <summary>Warrior-class critical hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] WarriorCritMessages =
    {
        "💥 CRUSHING BLOW! You put your entire body into it — {1} devastating damage to {0}!",
        "💥 SHATTERING STRIKE! {0} is sent reeling — {1} damage!"
    };

    /// <summary>Mage-class critical hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] MageCritMessages =
    {
        "💥 ARCANE SURGE! Your spell overloads and detonates — {1} damage on {0}!",
        "💥 CRITICAL RESONANCE! The magic tears through {0} for {1} damage!"
    };

    /// <summary>Rogue-class critical hit narration. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] RogueCritMessages =
    {
        "💥 VITAL STRIKE! You find the perfect spot — {1} piercing damage to {0}!",
        "💥 BACKSTAB! {0} never saw it coming — {1} damage!"
    };

    // ── Enemy messages ──────────────────────────────────────────────

    /// <summary>Enemy hit narration shown when the player takes damage. Format: {0} = enemy name, {1} = damage.</summary>
    public static readonly string[] EnemyHitMessages =
    {
        "{0} strikes you for {1} damage!",
        "{0} lands a hit — {1} damage!",
        "You take {1} damage from {0}'s attack!",
        "{0}'s blow connects! {1} damage!",
        "You fail to dodge — {0} deals {1} damage!"
    };

    // ── Player dodge messages ───────────────────────────────────────

    /// <summary>Player dodge narration shown when the player evades an attack. Format: {0} = enemy name.</summary>
    public static readonly string[] PlayerDodgeMessages =
    {
        "You dodge {0}'s attack!",
        "You sidestep {0}'s blow just in time!",
        "{0} swings and misses — you're too quick!",
        "You slip past {0}'s strike!"
    };

    // ── Combat start (ambient flavor) ───────────────────────────────

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

    // ── Player near-death (HP below 25%) ────────────────────────────

    /// <summary>Atmospheric lines shown when the player's HP drops below 25%.</summary>
    public static readonly string[] NearDeath =
    {
        "Everything hurts. But you're still standing.",
        "One bad hit. Don't take one bad hit.",
        "The edges of your vision blur. Keep moving."
    };

    // ── Killing blow ────────────────────────────────────────────────

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

    // ── Critical hit flavor ─────────────────────────────────────────

    /// <summary>Brief atmospheric lines shown on a critical hit.</summary>
    public static readonly string[] CritFlavor =
    {
        "Clean hit.",
        "That one landed perfectly.",
        "Right where it hurts."
    };

    // ── Enemy special attack ────────────────────────────────────────

    /// <summary>
    /// Atmospheric lines for enemy special-ability announcements.
    /// Format: {0} = enemy name, {1} = ability/attack name.
    /// </summary>
    public static readonly string[] EnemySpecialAttack =
    {
        "{0} rears back and unleashes {1}!",
        "{0} draws on dark power — {1}!",
        "{0} gathers itself and strikes with {1}!"
    };
}
