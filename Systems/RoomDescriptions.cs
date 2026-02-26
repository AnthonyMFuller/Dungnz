namespace Dungnz.Systems;

/// <summary>
/// Provides floor-themed room description pools for procedural dungeon generation.
/// Each floor has 12 atmospheric descriptions tuned to its environment and inhabitants.
/// </summary>
public static class RoomDescriptions
{
    /// <summary>Returns the description pool for the given floor (1–8).</summary>
    /// <param name="floor">The dungeon floor number (1 = Goblin Caves … 8 = Final Sanctum).</param>
    /// <returns>
    /// An array of room description strings appropriate for the floor's theme.
    /// Falls back to <see cref="GoblinCaves"/> for any unrecognised floor number.
    /// </returns>
    public static string[] ForFloor(int floor) => floor switch
    {
        1 => GoblinCaves,
        2 => SkeletonCatacombs,
        3 => TrollWarrens,
        4 => ShadowRealm,
        5 => DragonsLair,
        6 => VoidAntechamber,
        7 => BonePalace,
        8 => FinalSanctum,
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
        "Rows of sharpened stakes line a shallow pit just inside the doorway. Someone was expecting visitors.",
        // WI-E2 additions — The Forgotten Garrison
        "Rusted armor hangs from broken racks. The phantom of a soldier's last march echoes in the dust.",
        "Faded banners of a fallen kingdom drape the walls. Whatever they once fought for has been forgotten.",
        "The barracks reek of old iron and stale fear. Boot-shaped depressions in the dust mark where men once stood watch.",
        "A shattered war drum lies in the corner. Someone — or something — tried to call for help here, once."
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
        "A chime sounds softly somewhere above. The echo takes far too long to fade.",
        // WI-E2 additions — The Bone Catacombs
        "Ossuaries line the walls, packed with the pious dead. They weren't pious enough.",
        "The ceiling drips. Each drop echoes like a heartbeat in this cathedral of bones.",
        "Burial niches have been torn open from the inside. Whatever they buried here didn't stay buried.",
        "Candles that should have burned out centuries ago still flicker in the dark. They illuminate nothing good."
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
        "Troll-sized handprints in dark rust cover the ceiling. You don't want to imagine the context.",
        // WI-E2 additions — The Fungal Chasm
        "Bioluminescent spores drift on air currents that shouldn't exist this deep underground.",
        "The mycelium network underfoot pulses faintly. You get the sense something vast and patient is thinking.",
        "Alien flowers bloom from the skulls of creatures you can't identify. They're beautiful. You don't trust them.",
        "The air tastes like copper and cloves. Your head swims slightly. You press on."
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
        "Time feels wrong in this room. You may have been standing here for seconds or hours.",
        // WI-E2 additions — The Drowned Halls
        "Waterlogged scripture floats past your ankles. Whatever god was worshipped here, it drowned with the temple.",
        "Statues of forgotten deities stand knee-deep in black water. Their faces have been methodically removed.",
        "The current is gentle but insistent, as if the dungeon itself wants you to leave.",
        "A submerged altar glows faintly. The offering bowl still holds coins from a civilization that no longer exists."
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
        "Sulphur hangs thick in the air. Every breath tastes of brimstone and old fire.",
        // WI-E2 additions — The Forge of the Damned
        "The heat here is wrong — not natural, not fire. Something fundamental about this place burns.",
        "Weapon racks hold blades that were forged and never meant to be used by living hands.",
        "The anvil at the center of the room is cracked down the middle. Whatever was made on it broke the anvil.",
        "Slag and ash carpet the floor. You can hear the echo of hammers that stopped striking before you were born."
    };

    /// <summary>Floor 6 — the Void Antechamber, where geometry and reality grow unreliable.</summary>
    public static readonly string[] VoidAntechamber =
    {
        "The geometry here is wrong. Rooms connect to themselves. You've learned not to think about it.",
        "Your shadow falls in three directions simultaneously. None of them are yours.",
        "The walls breathe. Slowly. You're not sure if the dungeon is alive, or if you're already dead.",
        "Reality is thin here. Through the cracks, you see something looking back."
    };

    /// <summary>Floor 7 — the Bone Palace, built from the remains of the unimaginably large dead.</summary>
    public static readonly string[] BonePalace =
    {
        "Titanic femurs form the vaulted ceiling. Whatever died to build this palace, it wasn't human.",
        "The throne room of a necromancer king. Every surface is etched with names of the dead. Thousands of them.",
        "Chandeliers of compacted skulls hang overhead, lit by cold fire that casts no warmth.",
        "You step over the threshold and feel the weight of ten thousand souls press against your chest."
    };

    /// <summary>Floor 8 — the Final Sanctum, the oldest and most terrible part of the dungeon.</summary>
    public static readonly string[] FinalSanctum =
    {
        "The dungeon breathes around you. It knows you're here. It has always known.",
        "This is the heart. The walls are not stone — they're compressed darkness, given form by something ancient and hungry.",
        "There is no echo here. Sound dies the moment it leaves your lips. Even your footsteps are silent.",
        "You've come to the end. The air tastes like finality. Whatever waits ahead has been waiting for you specifically."
    };
}
