namespace Dungnz.Systems;

/// <summary>
/// Provides floor-themed room description pools for procedural dungeon generation.
/// Each floor has 12 atmospheric descriptions tuned to its environment and inhabitants.
/// </summary>
public static class RoomDescriptions
{
    /// <summary>Returns the description pool for the given floor (1–5).</summary>
    /// <param name="floor">The dungeon floor number (1 = Goblin Caves … 5 = Dragon's Lair).</param>
    /// <returns>
    /// An array of 12 room description strings appropriate for the floor's theme.
    /// Falls back to <see cref="GoblinCaves"/> for any unrecognised floor number.
    /// </returns>
    public static string[] ForFloor(int floor) => floor switch
    {
        1 => GoblinCaves,
        2 => SkeletonCatacombs,
        3 => TrollWarrens,
        4 => ShadowRealm,
        5 => DragonsLair,
        _ => GoblinCaves
    };

    /// <summary>Floor 1 — damp, earthy chambers of crude goblin habitation.</summary>
    public static readonly string[] GoblinCaves =
    {
        "Crude goblin drawings — stick figures stabbing each other — cover the wall beside a guttering torch.",
        "A pile of gnawed bones marks a goblin's feeding spot. The smell is appalling.",
        "Water seeps through the ceiling and pools in a shallow depression. Something has been drinking here recently.",
        "Rusted iron cages hang from the ceiling on frayed rope, some still occupied by grim remains.",
        "Muddy footprints of varying sizes crisscross the floor, all leading deeper into the dark.",
        "A guttering tallow candle sits in a niche hacked from the stone. Someone was here very recently.",
        "Scratches tally the days on the wall — or the kills. The count is disturbingly high.",
        "A crude fire pit of blackened stones smells of rancid meat and burnt fur.",
        "Broken clay pots and scattered grain suggest a ransacked larder. Goblins eat well, if not cleanly.",
        "Rough-hewn supports of splintered timber hold the ceiling up — barely. Mud drips between the cracks.",
        "A child-sized tunnel opens low in the far wall. Whatever uses it, you'd rather not meet.",
        "Rows of sharpened stakes line a shallow pit just inside the doorway. Someone was expecting visitors."
    };

    /// <summary>Floor 2 — ancient stone catacombs heavy with undead and cold silence.</summary>
    public static readonly string[] SkeletonCatacombs =
    {
        "Rows of stone niches line the walls, each holding a skull that seems to watch you pass.",
        "Ancient inscriptions in a long-dead tongue circle the floor. Some still glow faintly blue.",
        "The air here is ice-cold and utterly still. Dust motes hang suspended like memories.",
        "A stone sarcophagus dominates the centre of the room, its lid shoved askew by something from within.",
        "Cobwebs thick as curtains drape every corner. The spiders that made them must have been enormous.",
        "Calcified candles on a stone altar have burned down to stubs, yet somehow still flicker.",
        "Names are carved into every flagstone underfoot. You try not to read them.",
        "The ceiling here is lost in shadow. Occasionally something shifts up there with a dry, papery sound.",
        "A scattering of grave goods — tarnished rings, cracked pottery, a rusted dagger — lies before a sealed niche.",
        "Ice rimes the lower walls despite no apparent source of cold. Your breath fogs with every step.",
        "Empty eye sockets on a dozen skulls all point toward the northern passage. Coincidence is unlikely.",
        "A chime sounds softly somewhere above. The echo takes far too long to fade."
    };

    /// <summary>Floor 3 — fetid, bestial warrens carved by creatures of immense strength.</summary>
    public static readonly string[] TrollWarrens =
    {
        "Deep claw gouges in the stone walls suggest something enormous once squeezed through here.",
        "A crude nest of smashed furniture and matted fur fills one corner. The smell is legendary.",
        "Half-eaten carcasses hang from iron hooks in the ceiling. Trolls don't waste food — they just forget it.",
        "The floor is slick with a substance you choose not to identify. Your boots will never be the same.",
        "A boulder the size of a cart has been wedged into the doorway and then punched through again.",
        "Enormous knuckle-prints dent the stone floor, each deep enough to hold a boot.",
        "Scraps of armour — human-sized — have been bent into crude ornaments and hung from the walls.",
        "A fire of monstrous scale has scorched an entire wall black. Whatever cooked here, it wasn't small.",
        "The stench of wet fur and old blood is almost physical. You breathe through your mouth.",
        "Massive leg bones have been stacked into a rough cairn. A trophy display, perhaps, or a warning.",
        "A low growl resonates through the walls even now. Distance and stone make it no less alarming.",
        "Troll-sized handprints in dark rust cover the ceiling. You don't want to imagine the context."
    };

    /// <summary>Floor 4 — a surreal realm where geometry fails and shadows have their own agenda.</summary>
    public static readonly string[] ShadowRealm =
    {
        "The shadows here move counter to the light source. You try not to look directly at them.",
        "Two corridors intersect at an impossible angle. Your compass spins uselessly.",
        "The walls breathe. Slowly, rhythmically. You're choosing not to think about that.",
        "A mirror on the far wall shows a room that isn't the one you're standing in.",
        "Whispers circle just beyond hearing — close enough that you keep turning, finding nothing.",
        "The floor tiles repeat in a pattern that shouldn't tile. Your eyes water if you follow it.",
        "Colours are wrong here: the stone is a shade of purple that has no name.",
        "Your torch casts four shadows. You only cast one.",
        "A doorway leads to the room you just came from, but the room looks subtly different now.",
        "The ceiling and floor are identical. For a disorienting moment you cannot tell which way is up.",
        "Something like laughter echoes from every direction simultaneously, then stops at once.",
        "Time feels wrong in this room. You may have been standing here for seconds or hours."
    };

    /// <summary>Floor 5 — the scorched, gold-laden lair of an ancient and terrible dragon.</summary>
    public static readonly string[] DragonsLair =
    {
        "Coins are embedded in the melted stone floor, half-fused by ancient dragon-fire. Each one was someone's treasure.",
        "Claw prints the size of cart wheels lead deeper into the darkness.",
        "The walls are glazed smooth by unimaginable heat. You can see your reflection, distorted and afraid-looking.",
        "The air shimmers with residual heat. Breathing it feels like drinking from a forge.",
        "A skeleton in full plate armour has been fused to the wall, hands raised in a final ward.",
        "Gold coins spill from a fissure in the floor, glinting in the heat haze like a dream of wealth.",
        "Scorch marks radiate from a central point on the ceiling in perfect arcs — a single, precise exhalation.",
        "Ancient draconic runes are scorched into the stone. Even without knowing the tongue, they feel like warnings.",
        "A hoard of half-melted treasure slumps against the far wall, coins and crowns fused into a single mass.",
        "The bones of something as large as a horse have been snapped cleanly in two. A territorial dispute, perhaps.",
        "Heat shimmer makes the corridor ahead ripple as though submerged. Your eyes ache from the brightness.",
        "Sulphur hangs thick in the air. Every breath tastes of brimstone and old fire."
    };
}
