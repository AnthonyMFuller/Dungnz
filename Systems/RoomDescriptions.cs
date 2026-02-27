namespace Dungnz.Systems;

/// <summary>Identifies the functional role of a room for context-aware narration.</summary>
public enum RoomContext
{
    /// <summary>No enemy, merchant, shrine, or special interaction.</summary>
    Empty,
    /// <summary>An enemy is present -- pre-battle entry.</summary>
    Enemy,
    /// <summary>A wandering merchant occupies the room.</summary>
    Merchant,
    /// <summary>A healing shrine is the room focal point.</summary>
    Shrine,
    /// <summary>A special interactive feature (library, armory, shrine).</summary>
    Special,
}

/// <summary>
/// Provides floor-themed room description pools for procedural dungeon generation.
/// Each floor has atmospheric descriptions, plus context-aware pools keyed by
/// RoomContext and floor-theme band.
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

    // -----------------------------------------------------------------------
    // Context-aware description pools (Phase 8-E1)
    // Band 1 = F1-2  (Shallow Dungeon)
    // Band 2 = F3-4  (Armory / Barracks)
    // Band 3 = F5-6  (Undead / Lich Domain)
    // Band 4 = F7-8  (Volcanic / Abyssal)
    // -----------------------------------------------------------------------

    /// <summary>Band 1 -- empty room: damp stone, torchlight, quiet.</summary>
    public static readonly string[] ShallowEmpty =
    {
        "Water drips somewhere in the dark. The torch gutters, then holds.",
        "Damp stone walls sweat in the torchlight. Nothing here but silence.",
        "A tallow torch in a rusted bracket burns low. Empty, for now.",
        "Mud and gravel underfoot. The air smells of wet earth and old smoke.",
        "Abandoned. A stool and a cold fire pit are all that remain.",
    };

    /// <summary>Band 1 -- enemy room: threat in the shallow dark.</summary>
    public static readonly string[] ShallowEnemy =
    {
        "Muddy boot-prints lead in. They don't lead back out.",
        "Something has been eating here recently. The bones are still warm.",
        "A guttering torch casts moving shadows. One of them isn't a shadow.",
        "Crude claw marks scar the door frame. Whatever made them, it's still here.",
        "The dripping water masks a second sound -- shallow, ragged breathing.",
    };

    /// <summary>Band 1 -- merchant room: a trader making do in the damp.</summary>
    public static readonly string[] ShallowMerchant =
    {
        "A lantern on a cracked crate. Someone has been living here a while.",
        "Rope and canvas mark off a corner as a trading post.",
        "The smell of tallow and unwashed wool. A familiar kind of misery.",
        "A merchant's table -- battered, functional, incongruously hopeful.",
    };

    /// <summary>Band 1 -- shrine room: ancient devotion in the shallow dark.</summary>
    public static readonly string[] ShallowShrine =
    {
        "A niche in the wall holds a stone figure worn smooth by old hands.",
        "Dried flowers and a cracked bowl rest on a ledge. Offerings.",
        "Someone scratched a prayer into the stone long before you arrived.",
        "A single candle burns in the corner. It has no right to still be lit.",
    };

    /// <summary>Band 1 -- special room: something unusual in the dungeon shallows.</summary>
    public static readonly string[] ShallowSpecial =
    {
        "This room is different. The stonework is older, more deliberate.",
        "The air is cooler here. Something about this place resists the damp.",
        "Whoever built this room intended it to last. It has.",
        "Strange markings ring the walls -- not goblin-work. Something older.",
    };

    /// <summary>Band 2 -- empty room: abandoned military infrastructure.</summary>
    public static readonly string[] ArmoryEmpty =
    {
        "Rusted weapon racks line the wall. The blades are long past saving.",
        "Barracks cots, smashed flat. Whatever slept here stopped needing rest.",
        "A training dummy, hacked to splinters. The training never helped.",
        "Bloodstains on the floor map a fight that ended one-sidedly.",
        "Iron manacles on the wall. The prisoner they held is not here now.",
    };

    /// <summary>Band 2 -- enemy room: a soldier's killing ground.</summary>
    public static readonly string[] ArmoryEnemy =
    {
        "The room smells of iron and anticipation. Eyes in the dark.",
        "Weapon racks on three walls. Whatever's here knows how to use them.",
        "Boot-steps on stone -- steady, controlled. Something trained is waiting.",
        "Shield-wall marks on the flagstones. Used for slaughter.",
        "A war drum in the corner. The tension here is its own kind of sound.",
    };

    /// <summary>Band 2 -- merchant room: an opportunist among the ruins.</summary>
    public static readonly string[] ArmoryMerchant =
    {
        "A trader among the weapon racks, apparently unbothered by the carnage.",
        "The smell of oil and metal. Someone is maintaining gear -- profitably.",
        "A lantern on a shield-face. The merchant looks at home here.",
        "Salvage, sorted and priced. The war was someone else's problem.",
    };

    /// <summary>Band 2 -- shrine room: a soldier's chapel in the barracks.</summary>
    public static readonly string[] ArmoryShrine =
    {
        "A small altar bears a broken sword laid flat -- an old soldier's offering.",
        "Scratched battle-prayers cover the stone above the shrine.",
        "Dog-tags and belt buckles form an offering around the central figure.",
        "The shrine is tended, despite everything. Fresh oil in the lamp.",
    };

    /// <summary>Band 2 -- special room: a purpose-built chamber in the barracks.</summary>
    public static readonly string[] ArmorySpecial =
    {
        "This room was built for something specific. The fixtures say as much.",
        "A sealed vault door, long since forced. Something worth protecting was here.",
        "The engineering here is deliberate -- this wasn't thrown together.",
        "Whatever this room was for, it's important enough to still be intact.",
    };

    /// <summary>Band 3 -- empty room: bone dust and necromantic residue.</summary>
    public static readonly string[] UndeadEmpty =
    {
        "Bone dust coats every surface. The air tastes of it.",
        "Necromantic runes glow faint blue around the perimeter. Ancient work.",
        "An empty coffin lies open, lid cast aside. Long emptied.",
        "Soul-fire torches burn cold and still. No wind. No warmth.",
        "The silence here is wrong -- too complete, like something swallowed it.",
    };

    /// <summary>Band 3 -- enemy room: undead threat in the lich's halls.</summary>
    public static readonly string[] UndeadEnemy =
    {
        "The runes on the walls pulse faster as you enter. Something activates.",
        "A rattling sound fills the room -- hollow, rhythmic, patient.",
        "Coffin lids have been pushed aside. Whatever lay in them is up.",
        "Cold soul-fire casts the room in a light that makes everything look dead.",
        "Unholy altar at the far wall, still active. You're not alone here.",
    };

    /// <summary>Band 3 -- merchant room: commerce in the shadow of the Lich.</summary>
    public static readonly string[] UndeadMerchant =
    {
        "A living person in these halls. You're both surprised and suspicious.",
        "The trader has soul-fire lanterns. They don't seem to bother them.",
        "Bones swept to the walls to make space for a trading table. Pragmatic.",
        "The merchant smells of embalming herbs and doesn't explain why.",
    };

    /// <summary>Band 3 -- shrine room: a defiant holy place in the lich's domain.</summary>
    public static readonly string[] UndeadShrine =
    {
        "A shrine standing intact amid the necromantic ruin. Something protects it.",
        "Holy symbols carved into stone that tries to absorb them. They hold.",
        "The light here is warm. It has no business being warm, but it is.",
        "Someone lit a candle on this altar and it burned back the dark.",
    };

    /// <summary>Band 3 -- special room: a purpose-built chamber in the lich's domain.</summary>
    public static readonly string[] UndeadSpecial =
    {
        "This room was engineered, not carved. The Lich's hand is in this.",
        "Ritual circles within ritual circles. Something was bound here repeatedly.",
        "The runes here are denser. More deliberate. You feel observed.",
        "A sealed archive of bone-vellum scrolls. Knowledge as dangerous as any weapon.",
    };

    /// <summary>Band 4 -- empty room: lava-seamed stone and sulfur heat.</summary>
    public static readonly string[] VolcanicEmpty =
    {
        "Lava seams glow in the cracks between the flagstones underfoot.",
        "The sulfur smell is thick enough to taste. Your eyes water.",
        "Demonic carvings cover the walls -- territorial, not decorative.",
        "The heat here is oppressive. The air shimmers near the walls.",
        "Obsidian-smooth stone polished by centuries of volcanic breath.",
    };

    /// <summary>Band 4 -- enemy room: demonic presence in volcanic halls.</summary>
    public static readonly string[] VolcanicEnemy =
    {
        "The heat spikes as you enter. Something runs hotter than the stone.",
        "Demonic runes on the far wall pulse in time with something breathing.",
        "Claw marks in the obsidian floor. Deep. Deliberate. Recent.",
        "Brimstone and shadow. Two things in this room, and one of them is you.",
        "The lava seams trace outward from a central point. From where it stands.",
    };

    /// <summary>Band 4 -- merchant room: trade at the edge of the abyss.</summary>
    public static readonly string[] VolcanicMerchant =
    {
        "Someone is selling things this deep. You have questions you won't ask.",
        "The trader stands over a lava seam, apparently at ease. Worrying.",
        "Fireproof cases, obsidian weights, a smile that's seen worse than this.",
        "A merchant at the end of the world. You've both made interesting choices.",
    };

    /// <summary>Band 4 -- shrine room: a stubborn holy place near the abyss.</summary>
    public static readonly string[] VolcanicShrine =
    {
        "A shrine in the volcanic dark. The heat doesn't touch it.",
        "Holy stone in a demonic place -- stubbornly, impossibly intact.",
        "Cool air radiates from the shrine. In here, the sulfur retreats.",
        "The flame on this altar burns the right color. That matters, down here.",
    };

    /// <summary>Band 4 -- special room: an engineered chamber at the dungeon's root.</summary>
    public static readonly string[] VolcanicSpecial =
    {
        "Something was built here with intent. In a place that unmakes things.",
        "The stonework resists the heat. Whoever made this planned for eternity.",
        "Demonic architecture, functional and terrible. You are a guest here.",
        "The deepest things in the dungeon were put here deliberately. By something.",
    };

    /// <summary>
    /// Returns context-aware descriptions for the given floor and room role.
    /// Falls back to the generic per-floor pool for unrecognised combinations.
    /// </summary>
    public static string[] ForFloorAndContext(int floor, RoomContext ctx)
    {
        return (floor, ctx) switch
        {
            (1 or 2, RoomContext.Empty)    => ShallowEmpty,
            (1 or 2, RoomContext.Enemy)    => ShallowEnemy,
            (1 or 2, RoomContext.Merchant) => ShallowMerchant,
            (1 or 2, RoomContext.Shrine)   => ShallowShrine,
            (1 or 2, RoomContext.Special)  => ShallowSpecial,
            (3 or 4, RoomContext.Empty)    => ArmoryEmpty,
            (3 or 4, RoomContext.Enemy)    => ArmoryEnemy,
            (3 or 4, RoomContext.Merchant) => ArmoryMerchant,
            (3 or 4, RoomContext.Shrine)   => ArmoryShrine,
            (3 or 4, RoomContext.Special)  => ArmorySpecial,
            (5 or 6, RoomContext.Empty)    => UndeadEmpty,
            (5 or 6, RoomContext.Enemy)    => UndeadEnemy,
            (5 or 6, RoomContext.Merchant) => UndeadMerchant,
            (5 or 6, RoomContext.Shrine)   => UndeadShrine,
            (5 or 6, RoomContext.Special)  => UndeadSpecial,
            (7 or 8, RoomContext.Empty)    => VolcanicEmpty,
            (7 or 8, RoomContext.Enemy)    => VolcanicEnemy,
            (7 or 8, RoomContext.Merchant) => VolcanicMerchant,
            (7 or 8, RoomContext.Shrine)   => VolcanicShrine,
            (7 or 8, RoomContext.Special)  => VolcanicSpecial,
            _ => ForFloor(floor),
        };
    }
}
