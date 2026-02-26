namespace Dungnz.Models;

/// <summary>
/// Identifies the body slot that an armor piece occupies when equipped.
/// Weapons, accessories, consumables, and gold use <see cref="None"/>.
/// </summary>
public enum ArmorSlot
{
    /// <summary>Not a slot-occupying armor piece (weapons, accessories, consumables, gold).</summary>
    None,
    /// <summary>Head slot — helms, hoods, crowns.</summary>
    Head,
    /// <summary>Shoulder slot — pauldrons, mantles.</summary>
    Shoulders,
    /// <summary>Chest slot — cuirasses, robes, tunics, chainmail.</summary>
    Chest,
    /// <summary>Hand slot — gauntlets, gloves, bracers.</summary>
    Hands,
    /// <summary>Leg slot — greaves, leggings, chaps.</summary>
    Legs,
    /// <summary>Foot slot — boots, sabatons, shoes.</summary>
    Feet,
    /// <summary>Back slot — cloaks, capes.</summary>
    Back,
    /// <summary>Off-hand slot — shields and spell focuses.</summary>
    OffHand,
}
