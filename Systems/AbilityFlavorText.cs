namespace Dungnz.Systems;
using Dungnz.Models;

/// <summary>
/// Provides immersive flavor text for ability activations, displayed before each ability
/// is used in combat. Maintains a dark fantasy tone and emphasizes impact.
/// </summary>
public static class AbilityFlavorText
{
    /// <summary>
    /// Returns the activation flavor text for the specified ability type.
    /// </summary>
    /// <param name="type">The ability type to get flavor text for.</param>
    /// <param name="enhanced">If true, returns enhanced flavor for conditional effects (e.g., empowered Backstab).</param>
    /// <returns>A string containing the activation flavor text, or empty string if not found.</returns>
    public static string Get(AbilityType type, bool enhanced = false)
    {
        return type switch
        {
            // Warrior abilities
            AbilityType.ShieldBash => "You slam your shield into the enemy's skull with bone-cracking force!",
            AbilityType.BattleCry => "A primal roar tears from your throat. The enemy takes a step back.",
            AbilityType.Fortify => "You plant your feet and brace. Nothing will move you.",
            AbilityType.RecklessBlow => "You throw caution aside and swing with everything you have!",
            AbilityType.LastStand => "Your vision narrows. The pain fades. Everything slows. This ends now.",

            // Mage abilities
            AbilityType.ArcaneBolt => "Crackling energy leaps from your fingertips, burning the air between you!",
            AbilityType.FrostNova => "A wave of bitter cold explodes outward, frosting everything in its path!",
            AbilityType.ManaShield when enhanced => "The arcane barrier dissolves back into the aether.",
            AbilityType.ManaShield => "You wrap yourself in a lattice of pure arcane energy.",
            AbilityType.ArcaneSacrifice => "You tear power from your own life essence. It costs you, but it feeds the spell.",
            AbilityType.Meteor => "The ceiling fractures. A shard of the heavens answers your call.",

            // Rogue abilities
            AbilityType.QuickStrike => "A lightning jab — already moving before they can react.",
            AbilityType.Backstab when enhanced => "The opening is perfect. You exploit it without mercy.",
            AbilityType.Backstab => "You find the gap. The blade goes exactly where it needs to.",
            AbilityType.Evade => "You dissolve into the shadows. The blow finds only air.",
            AbilityType.Flurry => "Steel becomes a blur. One, two, three — you've lost count.",
            AbilityType.Assassinate => "One clean strike. They never see it coming.",

            _ => ""
        };
    }
}
