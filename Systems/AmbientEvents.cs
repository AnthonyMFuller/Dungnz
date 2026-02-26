namespace Dungnz.Systems;

/// <summary>
/// Static pools of atmospheric one-liner flavor messages shown at random during room transitions,
/// organised by dungeon floor.
/// </summary>
public static class AmbientEvents
{
    /// <summary>
    /// Returns the ambient event pool for the given <paramref name="floor"/>.
    /// Falls back to <see cref="GoblinCaves"/> for any floor outside the range 1–8.
    /// </summary>
    /// <param name="floor">The current dungeon floor (1–8).</param>
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

    /// <summary>Flavor messages for Floor 1 — the Goblin Caves.</summary>
    public static readonly string[] GoblinCaves =
    {
        "Distant goblin chatter echoes through the tunnels.",
        "A rat scurries across your boot.",
        "You smell cooking — something unidentifiable and probably best left that way.",
        "The torches flicker in a cold draft from somewhere deeper.",
        "Claw marks on the wall suggest something large dragged itself through here recently.",
        "Crude stick-figure drawings on the wall seem to be warning signs. Or art. Hard to say.",
        "A pile of gnawed bones sits in the corner. Something ate well here.",
        "You hear distant, high-pitched cackling cut short by a loud thump.",
        "Water drips steadily from the ceiling — each drop echoing like a tiny bell.",
        "A crude tripwire crosses the passage. Fortunately, you spot it just in time.",
    };

    /// <summary>Flavor messages for Floor 2 — the Skeleton Catacombs.</summary>
    public static readonly string[] SkeletonCatacombs =
    {
        "Somewhere ahead, bones scrape against stone.",
        "A cold draft carries the faint sound of whispering — words you can't quite make out.",
        "The blue-lit runes on the walls pulse once, then go still.",
        "An empty eye socket in the wall watches you pass. Definitely just a carving.",
        "The air tastes of chalk and old death.",
        "Something shifts in a sealed alcove behind an iron grate. You keep moving.",
        "A distant grinding — stone on stone — reverberates through the floor beneath your feet.",
        "The candles here burn with a pale, cold flame that gives no warmth.",
        "You feel certain you are being watched, though no living eyes are upon you.",
        "A skeletal hand protrudes from the wall, fingers curled as if beckoning.",
    };

    /// <summary>Flavor messages for Floor 3 — the Troll Warrens.</summary>
    public static readonly string[] TrollWarrens =
    {
        "A distant boom — something massive moved somewhere in the dark.",
        "The smell hits you before the room does. Something large lives nearby.",
        "You hear a wet, tearing sound from somewhere above. Best not to investigate.",
        "Heavy footsteps — definitely not yours — thunder past somewhere on the other side of the wall.",
        "A low, bestial grunt rolls through the tunnels like distant thunder.",
        "The ceiling here bears deep gouges at a height that makes you feel very small.",
        "Something moves in the dark at the edge of your torchlight, then retreats.",
        "The floor is sticky underfoot. You decide not to think about why.",
        "A crude troll-sized club leans against the wall, casually forgotten.",
        "Somewhere in the dark, something is breathing — slow, deep, and patient.",
    };

    /// <summary>Flavor messages for Floor 4 — the Shadow Realm.</summary>
    public static readonly string[] ShadowRealm =
    {
        "Your shadow moves a half-second after you do.",
        "A sound like glass breaking, very close. Nothing is broken.",
        "The corridor behind you looks longer than it should.",
        "You blink, and for a moment, the walls are a different color.",
        "A whisper speaks your name — then insists it didn't.",
        "The geometry of this place refuses to stay consistent.",
        "Your reflection in a dark puddle doesn't quite mirror your movements.",
        "Gravity here feels like a suggestion more than a rule.",
        "A door appears, solid and certain. When you blink it is a wall.",
        "The silence here has texture — thick, deliberate, aware.",
    };

    /// <summary>Flavor messages for Floor 5 — the Dragon's Lair.</summary>
    public static readonly string[] DragonsLair =
    {
        "The floor is warm under your boots.",
        "Somewhere in the darkness, something breathes. Slowly. Enormously.",
        "Gold coins catch the light — half-melted into the stone floor.",
        "A sound like distant thunder. Nothing like thunder at all.",
        "The air shimmers with heat that has no visible source.",
        "Scorch marks blacken the ceiling in great sweeping arcs.",
        "A faint smell of sulfur and something ancient that has no name.",
        "The walls here are smooth — fused by heat into a glassy surface.",
        "Your torch gutters in a breath of wind far too large to be natural.",
        "Every instinct you have is screaming the same word: leave.",
    };

    /// <summary>Flavor messages for Floor 6 — the Void Antechamber.</summary>
    public static readonly string[] VoidAntechamber =
    {
        "Your shadow flickers. You didn't move.",
        "The corridor behind you looks different from how you remember it.",
        "A sound like tearing paper echoes from nowhere. Or everywhere.",
        "Reality hiccups. You feel it in your teeth.",
        "Something in your peripheral vision vanishes the moment you turn.",
        "The air tastes of static and old endings.",
        "The walls are closer than they were. You think.",
        "Something that may have been laughter echoes and immediately stops.",
        "You feel watched. You are being watched. Whatever is watching does not blink.",
        "The torch casts your shadow backward.",
    };

    /// <summary>Flavor messages for Floor 7 — the Bone Palace.</summary>
    public static readonly string[] BonePalace =
    {
        "The bones underfoot are all oriented in the same direction. Away from here.",
        "Cold fire in iron sconces burns blue and casts no warmth.",
        "Somewhere above, something vast shifts and settles.",
        "An inscription in an unknown tongue runs the length of the wall. You don't read it.",
        "The architecture is deliberate and wrong. Whoever built this had a philosophy.",
        "You hear nothing. The silence here is structural, load-bearing.",
        "The air smells of chalk and dead millennia.",
        "A chandelier of skulls hangs overhead. It sways, though there is no wind.",
        "Names are carved into every surface. Thousands of names.",
        "The palace remembers every person who has ever entered it. It forgets nothing.",
    };

    /// <summary>Flavor messages for Floor 8 — the Final Sanctum.</summary>
    public static readonly string[] FinalSanctum =
    {
        "The dungeon is breathing. You can feel it.",
        "Sound does not echo here. It just stops.",
        "You have the distinct sense that this room knows you.",
        "The air is wrong in a way that has no name.",
        "Something ancient and enormous is paying attention to you specifically.",
        "Whatever you came to do — you are doing it in the sight of something older than stone.",
        "Each step feels like a decision you cannot take back.",
        "The darkness here is not an absence of light. It is a presence.",
        "You are very close now. You can feel the weight of it.",
        "There is no echo. There is only you, and what waits at the end.",
    };
}
