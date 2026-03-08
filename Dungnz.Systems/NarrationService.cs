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
    /// <summary>
    /// Combat phase identifiers for phase-aware attack narration.
    /// </summary>
    private enum CombatPhase
    {
        Opening,    // Turns 1-3, both sides near full HP
        MidFight,   // Turns 4+, moderate HP or tension
        Desperate   // Either side below 30% HP, or turn 8+
    }

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

    /// <summary>Returns a random critical hit reaction line for the given enemy name.</summary>
    public string GetEnemyCritReaction(string enemyName)
    {
        var reactions = EnemyNarration.GetCritReactions(enemyName);
        return Pick(reactions);
    }

    /// <summary>Returns a random mid-combat idle taunt line for the given enemy name. Returns null if no custom line is defined.</summary>
    public string? GetEnemyIdleTaunt(string enemyName)
    {
        var taunts = EnemyNarration.GetIdleTaunts(enemyName);
        return taunts.Length > 0 ? Pick(taunts) : null;
    }

    /// <summary>Returns a random desperation line (< 25% HP) for the given enemy name. Returns null if no custom line is defined.</summary>
    public string? GetEnemyDesperationLine(string enemyName)
    {
        var lines = EnemyNarration.GetDesperationLines(enemyName);
        return lines.Length > 0 ? Pick(lines) : null;
    }

    /// <summary>Returns a phase-appropriate attack narration string based on combat progression.</summary>
    /// <param name="turnNumber">Current combat turn (1-indexed).</param>
    /// <param name="playerHpPercent">Player HP as 0.0–1.0.</param>
    /// <param name="enemyHpPercent">Enemy HP as 0.0–1.0.</param>
    public string GetPhaseAwareAttackNarration(int turnNumber, double playerHpPercent, double enemyHpPercent)
    {
        CombatPhase phase = DeterminePhase(turnNumber, playerHpPercent, enemyHpPercent);
        return phase switch
        {
            CombatPhase.Opening => Pick(_openingAttackPool),
            CombatPhase.MidFight => Pick(_midFightAttackPool),
            CombatPhase.Desperate => Pick(_desperateAttackPool),
            _ => Pick(_midFightAttackPool)
        };
    }

    // TODO(Barton): Call GetEnemyIdleTaunt(enemy.Name) every 3-4 turns in CombatEngine.PerformEnemyTurn() to display periodic mid-combat banter when no special action is taken
    // TODO(Barton): Call GetEnemyDesperationLine(enemy.Name) in CombatEngine.PerformEnemyTurn() when enemy HP < 25% of MaxHP (before the turn action) to display final stand desperation
    // TODO(Barton): Call GetPhaseAwareAttackNarration() from CombatEngine during player attack turn and display the result

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

    private static readonly string[] _openingAttackPool = new[]
    {
        "You press the attack with confidence!",
        "Your strike lands true — this fight is yours to win!",
        "First blood to you — the dungeon will remember this.",
        "You dance in, pressing your advantage. They won't recover quickly.",
        "Your blade finds its mark. The tide turns in your favor.",
        "You seize the moment, striking when they least expect it!"
    };

    private static readonly string[] _midFightAttackPool = new[]
    {
        "The exchange grows brutal — neither side giving ground.",
        "You find a gap in its guard. The price has been paid in bruises.",
        "Both combatants are bloodied but unbroken.",
        "You press your attack, testing their defenses.",
        "Sweat stings your eyes, but you keep fighting.",
        "Every blow counts now. Neither of you can afford a mistake."
    };

    private static readonly string[] _desperateAttackPool = new[]
    {
        "Against all odds, your blade finds its mark!",
        "The end is near for one of you — make it count!",
        "You fight with the fury of someone who has nothing left to lose.",
        "One final stand — your strike burns with desperation and resolve.",
        "You drive forward, knowing this could be your last chance.",
        "The air crackles with finality. Your attack could decide everything."
    };

    /// <summary>Determines the current combat phase based on turn number and HP percentages.</summary>
    private static CombatPhase DeterminePhase(int turnNumber, double playerHpPercent, double enemyHpPercent)
    {
        // Desperate phase: either side below 30% HP, or turn 8+
        if (playerHpPercent < 0.30 || enemyHpPercent < 0.30 || turnNumber >= 8)
            return CombatPhase.Desperate;

        // Opening phase: turns 1-3, both sides near full HP
        if (turnNumber <= 3 && playerHpPercent > 0.70 && enemyHpPercent > 0.70)
            return CombatPhase.Opening;

        // Mid-fight: everything else
        return CombatPhase.MidFight;
    }
}
