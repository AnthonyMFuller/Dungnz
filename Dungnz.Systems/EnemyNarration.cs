namespace Dungnz.Systems;

/// <summary>
/// Provides per-enemy encounter introduction and death narration line pools.
/// Note: DungeonBoss subclasses are routed to <see cref="BossNarration"/> by CombatEngine
/// and do not use this class.
/// </summary>
public static class EnemyNarration
{
    private static readonly Dictionary<string, string[]> _intros = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "A snarling goblin leaps from the shadows, blade glinting!",
            "A filthy goblin scrambles toward you, cackling with glee!",
            "A goblin darts out from behind a pillar, teeth bared!"
        },
        ["Skeleton"] = new[]
        {
            "Bones clatter as a skeleton lurches forward, hollow eyes fixed on you!",
            "A skeleton rises from the dust, jaw snapping with dry malice!",
            "The skeleton advances, sword arm raised, death rattling with every step!"
        },
        ["Troll"] = new[]
        {
            "The ground shakes as a massive troll heaves into view, knuckles dragging stone!",
            "A foul-smelling troll rounds the corner and lets out a thunderous roar!",
            "A troll slams its fists together with a boom that echoes through the dungeon!"
        },
        ["Dark Knight"] = new[]
        {
            "Armour black as midnight, a Dark Knight charges forward with lethal purpose!",
            "A Dark Knight steps from the darkness, visor down, blade levelled at your throat!",
            "The clink of dark steel announces the Dark Knight before you see it — too late to run!"
        },
        ["Goblin Shaman"] = new[]
        {
            "A hunched Goblin Shaman cackles, arcane sparks dancing between its claws!",
            "The Goblin Shaman raises a gnarled staff, chanting in a guttural tongue!",
            "Reeking of sulfur, a Goblin Shaman eyes you and begins weaving a dark hex!"
        },
        ["Stone Golem"] = new[]
        {
            "With a grinding groan the Stone Golem lurches to life, its gaze empty and inevitable!",
            "Boulders scrape together as the Stone Golem assembles itself in your path!",
            "A Stone Golem rises from the rubble, fists raised like battering rams!"
        },
        ["Wraith"] = new[]
        {
            "A cold shriek tears the air as the Wraith phases through the wall toward you!",
            "The torches snuff out — a Wraith drifts from the dark, trailing shadow!",
            "The temperature plummets as a Wraith coalesces before you, hungry and hateful!"
        },
        ["Vampire Lord"] = new[]
        {
            "A Vampire Lord descends from the vaulted ceiling, cape unfurling like wings!",
            "Red eyes open in the dark — the Vampire Lord smiles and reveals its fangs!",
            "The Vampire Lord glides forward, voice like silk: 'Your blood smells… exquisite.'"
        },
        ["Mimic"] = new[]
        {
            "The treasure chest you were reaching for SNAPS open — it's a Mimic!",
            "What you took for a pile of gold unfurls, rows of teeth gleaming — a Mimic!",
            "The innocent-looking chest suddenly sprouts legs and lunges — Mimic!",
        }
    };

    private static readonly Dictionary<string, string[]> _deaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "The goblin crumples with a pitiful shriek.",
            "The goblin collapses, its foul laughter silenced.",
            "The goblin slumps to the ground, twitching."
        },
        ["Skeleton"] = new[]
        {
            "The skeleton shatters into a heap of rattling bones.",
            "With a dry crack the skeleton falls, the light leaving its empty sockets.",
            "The skeleton collapses, its bones clattering across the dungeon floor."
        },
        ["Troll"] = new[]
        {
            "The troll topples like a felled oak, shaking the floor on impact.",
            "The troll lets out a final, confused grunt and crashes down.",
            "The massive troll slumps, its regeneration finally overwhelmed."
        },
        ["Dark Knight"] = new[]
        {
            "The Dark Knight staggers, then buckles — armour clanging as it hits the stone.",
            "The Dark Knight falls to one knee and is still, shadow fading from its visor.",
            "With a hollow groan the Dark Knight crumples, black steel ringing on the floor."
        },
        ["Goblin Shaman"] = new[]
        {
            "The Goblin Shaman's hex fizzles out as it topples, staff clattering away.",
            "The Goblin Shaman shrieks once, then crumples in a heap of robes and bones.",
            "With its last breath the Goblin Shaman curses you — then goes limp."
        },
        ["Stone Golem"] = new[]
        {
            "The Stone Golem groans, cracks spreading across its body, then shatters into gravel.",
            "The Stone Golem freezes mid-swing, crumbles, and collapses into rubble.",
            "With a grinding roar the Stone Golem breaks apart, chunks of rock raining down."
        },
        ["Wraith"] = new[]
        {
            "The Wraith releases a final banshee wail and dissolves into cold mist.",
            "The Wraith flickers, screams, and is torn apart by its own darkness.",
            "Light floods back as the Wraith unravels, its shriek fading into silence."
        },
        ["Vampire Lord"] = new[]
        {
            "The Vampire Lord recoils, then bursts into a cloud of ash and crimson mist.",
            "The Vampire Lord hisses, 'Impossible…' and crumbles to dust before you.",
            "The Vampire Lord's eyes go dark and it collapses, its ancient body dissolving."
        },
        ["Mimic"] = new[]
        {
            "The Mimic's disguise falls apart as it expires — just a pile of teeth and wood.",
            "The Mimic snaps once more, weakly, then lies still — its treasure finally real.",
            "The Mimic shudders, its lid slamming shut forever."
        }
    };

    private static readonly Dictionary<string, string[]> _critReactions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "HEHEHE! Didn't see that coming, did ya?!",
            "Lucky shot? No. I'm just better than you.",
            "That one's gonna leave a mark!"
        },
        ["Skeleton"] = new[]
        {
            "Foolish mortal. Your bones will join mine.",
            "The curse grows stronger with each wound.",
            "Death finds you, whether you like it or not."
        },
        ["Troll"] = new[]
        {
            "Your weakness pleases me. Feel the forest's wrath!",
            "Smash! Again and again until you BREAK!",
            "Stone and muscle — you have neither."
        },
        ["Dark Knight"] = new[]
        {
            "Pathetic. I've cleaved kingdoms apart.",
            "Your end comes swiftly, little worm.",
            "The darkness hungers, and I am its instrument."
        },
        ["Malachar the Undying"] = new[]
        {
            "Your defenses crumble before eternity's hunger.",
            "I have fed on stronger souls than you.",
            "Despair, mortal. I cannot be stopped."
        },
        ["Goblin Shaman"] = new[]
        {
            "The hex deepens! Your doom is WRITTEN!",
            "Dark magic flows through my claws!",
            "Feel the pain of a thousand curses!"
        },
        ["Stone Golem"] = new[]
        {
            "Granite meets flesh. There is no contest.",
            "My fist descends like an avalanche.",
            "Unmovable. Unstoppable. Inevitable."
        },
        ["Wraith"] = new[]
        {
            "Your life force splinters beneath my touch!",
            "The void hungers and feeds on your suffering.",
            "Scream — none will hear you in the shadow."
        },
        ["Vampire Lord"] = new[]
        {
            "Exquisite! Your blood sings to me.",
            "Centuries of hunger made me unstoppable.",
            "I have bathed in the lifeblood of empires."
        },
        ["Mimic"] = new[]
        {
            "You thought it treasure. It WAS your doom!",
            "My hunger runs deeper than any gold.",
            "Swallow your greed whole!"
        },
        ["Giant Rat"] = new[]
        {
            "SQUEAK! Your flesh tastes EXQUISITE!",
            "The plague runs through you now!",
            "My teeth find every weakness!"
        },
        ["Cursed Zombie"] = new[]
        {
            "The curse SPREADS through you like rot!",
            "Your end has been written since before birth.",
            "Death walks. Death festers. Death WINS."
        },
        ["Blood Hound"] = new[]
        {
            "Your blood sings to me like a siren's call!",
            "I taste your weakness, your FEAR!",
            "Pack hunter. Lone prey. How predictable."
        },
        ["Iron Guard"] = new[]
        {
            "Steel discipline meets your reckless thrashing.",
            "Ten thousand soldiers stand in me. You stand alone.",
            "Form. Discipline. Victory."
        },
        ["Night Stalker"] = new[]
        {
            "From shadow I strike. You never saw it coming.",
            "Darkness is my ally. You have none.",
            "Every moment you live is borrowed."
        },
        ["Frost Wyvern"] = new[]
        {
            "Ice crystallizes in your veins. Surrender to cold.",
            "Centuries of winter made me merciless.",
            "Your warmth fades. The frost takes all."
        },
        ["Chaos Knight"] = new[]
        {
            "Entropy bends to my will. You are CHAOS FOOD!",
            "Reality shatters where I tread.",
            "Order dies screaming. Your scream is next."
        },
        ["Lich King"] = new[]
        {
            "Millennia of dark magic crackle in my touch.",
            "You were dust long before you were born.",
            "Crown me with your suffering, mortal."
        },
        ["Shadow Imp"] = new[]
        {
            "The shadows MULTIPLY! We are LEGION!",
            "Many as shadows. Quick as thought. Merciless.",
            "You twitch while WE feast!"
        },
        ["Carrion Crawler"] = new[]
        {
            "Poison courses through me — through YOU now!",
            "Decay spreads at my touch. Resist it. You CAN'T!",
            "The swarm devours all."
        },
        ["Dark Sorcerer"] = new[]
        {
            "Arcane forces rend your flesh asunder!",
            "Magic bends to my will alone.",
            "You are a candle. I am the abyss."
        },
        ["Bone Archer"] = new[]
        {
            "From the shadows, death finds its mark AGAIN.",
            "My arrows never miss. Your luck has run out.",
            "One shot. Precisely placed. Your end."
        },
        ["Crypt Priest"] = new[]
        {
            "Prayer cannot save you from the JUDGMENT I deliver!",
            "Death is divine, and I am its vessel.",
            "Repent. It changes nothing."
        },
        ["Plague Bear"] = new[]
        {
            "Disease ravages your body with EVERY wound!",
            "The rot spreads. Soon there's nothing left.",
            "I am pestilence given flesh and rage."
        },
        ["Siege Ogre"] = new[]
        {
            "Siege walls crumble under my strength. So do YOU!",
            "Brute force. Endless. Unstoppable.",
            "Buckle. Break. SHATTER!"
        },
        ["Blade Dancer"] = new[]
        {
            "Grace and steel — your end is BEAUTIFUL!",
            "I dance circles around you. You cannot follow.",
            "Precision. Momentum. Victory in every stroke."
        },
        ["Mana Leech"] = new[]
        {
            "Your power flows INTO me now!",
            "I grow stronger as you fade.",
            "Empty yourself. Feel the hunger consume you."
        },
        ["Shield Breaker"] = new[]
        {
            "Your defenses are MINE to shatter!",
            "No armor can hold against my resolve.",
            "Strip you bare. Leave you vulnerable. Finish you."
        },
        ["Archlich Sovereign"] = new[]
        {
            "Skeletal guardians empowered by MY magic crush you!",
            "Centuries of conquest echo in this blow.",
            "I am sovereign. You are NOTHING."
        },
        ["Abyssal Leviathan"] = new[]
        {
            "Pressure builds. Your bones creak under the ABYSS!",
            "Depth and darkness are my strengths. You have neither.",
            "The deep rises to claim its due."
        },
        ["Infernal Dragon"] = new[]
        {
            "FLAMES CONSUME! Ash is all you'll leave behind!",
            "I am the fire's fury incarnate!",
            "Your flesh MELTS before true heat!"
        }
    };

    private static readonly string[] _defaultIntro = { "The {0} attacks!" };
    private static readonly string[] _defaultDeath = { "The {0} falls." };
    private static readonly string[] _defaultCritReaction = { "A brutal critical strike lands!" };

    /// <summary>Returns the pool of encounter introduction lines for the given enemy name.</summary>
    public static string[] GetIntros(string enemyName) =>
        _intros.GetValueOrDefault(enemyName, _defaultIntro);

    /// <summary>Returns the pool of death narration lines for the given enemy name.</summary>
    public static string[] GetDeaths(string enemyName) =>
        _deaths.GetValueOrDefault(enemyName, _defaultDeath);

    /// <summary>Returns the pool of critical hit reaction lines for the given enemy name.</summary>
    public static string[] GetCritReactions(string enemyName) =>
        _critReactions.GetValueOrDefault(enemyName, _defaultCritReaction);
}
