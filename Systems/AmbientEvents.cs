namespace Dungnz.Systems;

/// <summary>
/// Static pools of atmospheric one-liner flavor messages shown at random during room transitions,
/// organised by dungeon floor.
/// </summary>
public static class AmbientEvents
{
    /// <summary>
    /// Returns the ambient event pool for the given <paramref name="floor"/>.
    /// Falls back to <see cref="GoblinCaves"/> for any floor outside the range 1–5.
    /// </summary>
    /// <param name="floor">The current dungeon floor (1–5).</param>
    public static string[] ForFloor(int floor) => floor switch
    {
        1 => GoblinCaves,
        2 => SkeletonCatacombs,
        3 => TrollWarrens,
        4 => ShadowRealm,
        5 => DragonsLair,
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
}
