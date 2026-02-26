namespace Dungnz.Systems;

/// <summary>
/// Defines per-floor enemy spawn pools for an 8-floor dungeon.
/// Each floor has Common (60%), Uncommon (30%), and Rare (10%) pools.
/// Returns enemy type keys compatible with <see cref="Dungnz.Engine.EnemyFactory.CreateScaled"/>.
/// </summary>
public static class FloorSpawnPools
{
    // Each floor entry: (common[], uncommon[], rare[])
    // Only enemies present in enemy-stats.json are included.
    private static readonly (string[] Common, string[] Uncommon, string[] Rare)[] Pools =
    {
        // Floor 1
        (
            Common:   new[] { "goblin", "giantrat", "skeleton" },
            Uncommon: new[] { "cursedzombie" },
            Rare:     new[] { "goblinshaman" }
            // Note: "Slime" not in enemy-stats.json
        ),
        // Floor 2
        (
            Common:   new[] { "goblin", "giantrat", "cursedzombie" },
            Uncommon: new[] { "bloodhound" },
            // Note: "Shadow Imp" not in enemy-stats.json
            Rare:     new[] { "skeleton" }
        ),
        // Floor 3
        (
            Common:   new[] { "bloodhound", "wraith", "troll" },
            // Note: "Carrion Crawler", "Shadow Imp", "Dark Sorcerer", "Bone Archer" not in enemy-stats.json
            Uncommon: new[] { "darkknight", "goblinshaman" },
            Rare:     new[] { "cursedzombie" }
        ),
        // Floor 4
        (
            // Note: "Plague Bearer", "Mana Leech", "Shield Breaker" not in enemy-stats.json
            Common:   new[] { "ironguard", "troll", "darkknight" },
            Uncommon: new[] { "vampirelord", "stonegolem" },
            Rare:     new[] { "nightstalker" }
        ),
        // Floor 5
        (
            // Note: "Crypt Priest", "Blade Dancer" not in enemy-stats.json
            Common:   new[] { "nightstalker", "vampirelord", "darkknight" },
            // Note: "Siege Ogre" not in enemy-stats.json
            Uncommon: new[] { "ironguard", "stonegolem" },
            Rare:     new[] { "chaosknight" }
        ),
        // Floor 6
        (
            // Note: "Crypt Priest", "Siege Ogre" not in enemy-stats.json
            Common:   new[] { "frostwyvern", "vampirelord", "stonegolem" },
            // Note: "Blade Dancer" not in enemy-stats.json
            Uncommon: new[] { "chaosknight", "nightstalker" },
            Rare:     new[] { "frostwyvern" }  // elite variant applied by caller
        ),
        // Floor 7
        (
            // Note: "Shield Breaker", "Mana Leech" not in enemy-stats.json
            Common:   new[] { "chaosknight", "nightstalker", "ironguard" },
            // Note: "Siege Ogre", "Dark Sorcerer" not in enemy-stats.json
            Uncommon: new[] { "stonegolem", "frostwyvern" },
            Rare:     new[] { "frostwyvern" }
        ),
        // Floor 8
        (
            // Note: "Shield Breaker", "Blade Dancer" not in enemy-stats.json
            Common:   new[] { "chaosknight", "frostwyvern", "nightstalker" },
            // Note: "Mana Leech", "Siege Ogre" not in enemy-stats.json
            Uncommon: new[] { "nightstalker", "stonegolem" },
            Rare:     new[] { "chaosknight" }  // elite variant applied by caller
        ),
    };

    /// <summary>
    /// Picks a random enemy type key for the given dungeon floor using a
    /// 60% / 30% / 10% Common / Uncommon / Rare distribution.
    /// Falls back to a higher-availability pool when the selected tier is empty.
    /// </summary>
    /// <param name="floor">Dungeon floor number (1–8). Values outside this range are clamped.</param>
    /// <param name="rng">Random instance used for all rolls.</param>
    /// <returns>A lowercase enemy type key suitable for <see cref="Dungnz.Engine.EnemyFactory.CreateScaled"/>.</returns>
    public static string GetRandomEnemyForFloor(int floor, Random rng)
    {
        int index = Math.Clamp(floor - 1, 0, Pools.Length - 1);
        var pool = Pools[index];

        int roll = rng.Next(100);
        string[] candidates =
            roll < 60 ? pool.Common :
            roll < 90 ? pool.Uncommon :
            pool.Rare;

        // Fall back to Common pool if selected tier has no entries
        if (candidates.Length == 0) candidates = pool.Common;
        if (candidates.Length == 0) candidates = new[] { "goblin" }; // ultimate safety net

        return candidates[rng.Next(candidates.Length)];
    }

    /// <summary>
    /// Returns the elite spawn chance (0–100) for the given floor.
    /// Floors 1–3 have no elite chance. Floors 4–7 have a 5% chance. Floor 8 has a 10% chance.
    /// </summary>
    /// <param name="floor">Dungeon floor number (1–8).</param>
    /// <returns>Elite threshold as an integer percentage in the range [0, 100].</returns>
    public static int GetEliteChanceForFloor(int floor) =>
        floor >= 8 ? 10 : floor >= 4 ? 5 : 0;
}
