namespace Dungnz.Systems;

/// <summary>
/// Room state identifiers for context-aware narration.
/// </summary>
public enum RoomNarrationState
{
    FirstVisit,
    ActiveEnemies,
    Cleared,
    Merchant,
    Shrine,
    Boss
}

/// <summary>
/// Central service for retrieving varied narrative text from weighted pools.
/// </summary>
public class NarrationService
{
    private readonly Random _rng;

    /// <summary>Initialises the service with an optional <see cref="Random"/> instance.</summary>
    public NarrationService(Random? rng = null)
    {
        _rng = rng ?? new Random();
    }

    /// <summary>Picks a random entry from the pool. Returns empty string if pool is null or empty.</summary>
    public string Pick(string[] pool)
    {
        if (pool == null || pool.Length == 0) return string.Empty;
        return pool[_rng.Next(pool.Length)];
    }

    /// <summary>Picks a random entry, formatting with the provided args. Returns empty string if pool is null or empty.</summary>
    public string Pick(string[] pool, params object[] args)
    {
        if (pool == null || pool.Length == 0) return string.Empty;
        return string.Format(Pick(pool), args);
    }

    /// <summary>Returns a random entry from a weighted pool. Higher weight = more likely.</summary>
    public string PickWeighted(IEnumerable<(string text, int weight)> pool)
    {
        var items = pool.ToList();
        int total = items.Sum(x => x.weight);
        int roll = _rng.Next(total);
        int cumulative = 0;
        foreach (var (text, weight) in items)
        {
            cumulative += weight;
            if (roll < cumulative) return text;
        }
        return items.Last().text;
    }

    /// <summary>Returns true with the given probability (0.0–1.0).</summary>
    public bool Chance(double probability) => _rng.NextDouble() < probability;

    /// <summary>Returns a random room entry narration line based on the current room state.</summary>
    public string GetRoomEntryNarration(RoomNarrationState state)
    {
        return state switch
        {
            RoomNarrationState.FirstVisit => Pick(_firstVisitPool),
            RoomNarrationState.ActiveEnemies => Pick(_activeEnemiesPool),
            RoomNarrationState.Cleared => Pick(_clearedPool),
            RoomNarrationState.Merchant => Pick(_merchantPool),
            RoomNarrationState.Shrine => Pick(_shrinePool),
            RoomNarrationState.Boss => Pick(_bossPool),
            _ => string.Empty
        };
    }

    /// <summary>Returns a random critical hit reaction line for the given enemy name. Returns null if no custom reaction is defined.</summary>
    public string? GetEnemyCritReaction(string enemyName)
    {
        var reactions = EnemyNarration.GetCritReactions(enemyName);
        return reactions.Length > 0 ? Pick(reactions) : null;
    }

    private static readonly string[] _firstVisitPool = new[]
    {
        "You step into shadow-drenched stone. The air tastes of rust and old death.",
        "Torch-flicker reveals scratches on the walls—tally marks, or warnings.",
        "Your footsteps echo. Something listens in the dark.",
        "The corridor yawns ahead. Every dungeon begins with a single step—and every step could be your last.",
        "Cold stone beneath your boots. This place remembers every soul who fell here.",
        "You push deeper. The dungeon doesn't care if you're ready.",
        "Dust swirls in the dim light. No one's walked this path in years—maybe for good reason."
    };

    private static readonly string[] _activeEnemiesPool = new[]
    {
        "Eyes gleam from the shadows. They've been waiting.",
        "Movement—sharp, hungry. Time to earn your passage.",
        "Something shifts in the dark. It's seen you. It's not running.",
        "You hear breath that isn't yours. Steel meets flesh in three… two…",
        "Claws scrape stone. They smell blood—yours, if you're not careful.",
        "The chamber erupts. You're not the hunter here anymore.",
        "Snarls echo off the walls. No negotiation. Only violence."
    };

    private static readonly string[] _clearedPool = new[]
    {
        "Silence settles over broken bodies. You've earned a breath.",
        "Blood pools on ancient stone. The room is yours—for now.",
        "You step over the dead. The dungeon will remember this.",
        "Victory tastes like iron and ash. Keep moving.",
        "The last enemy falls. Enjoy the quiet while it lasts.",
        "Corpses litter the floor. You're still standing. That's what matters.",
        "The fight is done. Loot fast, leave faster."
    };

    private static readonly string[] _merchantPool = new[]
    {
        "A campfire flickers. Someone's set up shop in hell itself.",
        "The merchant grins through crooked teeth. 'Coin spends the same, even down here.'",
        "Trade goods glint in the firelight. Survival has a price.",
        "You spot a familiar face—or what passes for one this deep. Time to haggle.",
        "A voice calls out: 'You look like you need supplies. Lucky me.'",
        "The merchant's wares sprawl across a worn blanket. Business as usual in the abyss.",
        "Somehow, commerce thrives where heroes die. Capitalism never sleeps."
    };

    private static readonly string[] _shrinePool = new[]
    {
        "Ancient power hums in the air. Something older than stone dwells here.",
        "A shrine—cracked, forgotten, but still breathing with faint divine light.",
        "You feel the weight of old gods. They offer gifts, but nothing is free.",
        "Sacred ground, soaked in centuries of desperate prayers. Will yours be answered?",
        "The shrine pulses softly. Miracles and curses wear the same face down here.",
        "Candles burn with no wick. This place defies the dungeon's hunger.",
        "You stand before the altar. Blessing or bargain—what's the difference anymore?"
    };

    private static readonly string[] _bossPool = new[]
    {
        "The chamber opens wide. Something massive stirs in the depths.",
        "Your heart pounds. This is it—the monster at the end of the story.",
        "The air itself recoils. You've found what the dungeon's been hiding.",
        "A roar shakes dust from the ceiling. No running now.",
        "You step into the arena. Legends are written in blood—yours or theirs.",
        "The boss rises. Every fight before this was a warm-up.",
        "This is the moment. Win and ascend. Lose and feed the dark."
    };
}
