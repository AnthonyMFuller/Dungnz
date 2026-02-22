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

    private static readonly string[] _defaultIntro = { "The {0} attacks!" };
    private static readonly string[] _defaultDeath = { "The {0} falls." };

    /// <summary>Returns the pool of encounter introduction lines for the given enemy name.</summary>
    public static string[] GetIntros(string enemyName) =>
        _intros.GetValueOrDefault(enemyName, _defaultIntro);

    /// <summary>Returns the pool of death narration lines for the given enemy name.</summary>
    public static string[] GetDeaths(string enemyName) =>
        _deaths.GetValueOrDefault(enemyName, _defaultDeath);
}
