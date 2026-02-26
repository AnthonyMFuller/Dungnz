namespace Dungnz.Systems;

/// <summary>
/// Defines per-floor enemy spawn pools for an 8-floor dungeon.
/// Each floor has Common (60%), Uncommon (30%), and Rare (10%) pools.
/// Returns enemy type keys compatible with <see cref="Dungnz.Engine.EnemyFactory.CreateScaled"/>.
/// </summary>
public static class FloorSpawnPools
{
    private static readonly (string[] Common, string[] Uncommon, string[] Rare)[] Pools =
    {
        // Floor 1
        (
            Common:   new[] { "goblin", "giantrat", "skeleton" },
            Uncommon: new[] { "cursedzombie", "shadowimp" },
            Rare:     new[] { "goblinshaman" }
        ),
        // Floor 2
        (
            Common:   new[] { "goblin", "giantrat", "cursedzombie" },
            Uncommon: new[] { "bloodhound", "shadowimp" },
            Rare:     new[] { "skeleton", "carrioncrawler" }
        ),
        // Floor 3
        (
            Common:   new[] { "bloodhound", "wraith", "troll", "carrioncrawler" },
            Uncommon: new[] { "darkknight", "goblinshaman", "bonearcher", "darksorcerer" },
            Rare:     new[] { "cursedzombie", "shadowimp" }
        ),
        // Floor 4
        (
            Common:   new[] { "ironguard", "troll", "darkknight", "plaguebear", "manaleech" },
            Uncommon: new[] { "vampirelord", "stonegolem", "darksorcerer", "bonearcher" },
            Rare:     new[] { "nightstalker", "shieldbreaker" }
        ),
        // Floor 5
        (
            Common:   new[] { "nightstalker", "vampirelord", "darkknight", "cryptpriest", "bladedancer" },
            Uncommon: new[] { "ironguard", "stonegolem", "shieldbreaker", "manaleech" },
            Rare:     new[] { "chaosknight", "siegeogre" }
        ),
        // Floor 6
        (
            Common:   new[] { "frostwyvern", "vampirelord", "stonegolem", "cryptpriest", "siegeogre" },
            Uncommon: new[] { "chaosknight", "nightstalker", "bladedancer", "shieldbreaker" },
            Rare:     new[] { "frostwyvern", "manaleech" }
        ),
        // Floor 7
        (
            Common:   new[] { "chaosknight", "nightstalker", "ironguard", "shieldbreaker", "manaleech" },
            Uncommon: new[] { "stonegolem", "frostwyvern", "siegeogre", "darksorcerer" },
            Rare:     new[] { "frostwyvern", "bladedancer" }
        ),
        // Floor 8
        (
            Common:   new[] { "chaosknight", "frostwyvern", "nightstalker", "bladedancer", "shieldbreaker" },
            Uncommon: new[] { "nightstalker", "stonegolem", "manaleech", "siegeogre" },
            Rare:     new[] { "chaosknight", "cryptpriest" }
        ),
    };

    /// <summary>
    /// Picks a random enemy type key for the given dungeon floor using a
    /// 60% / 30% / 10% Common / Uncommon / Rare distribution.
    /// </summary>
    public static string GetRandomEnemyForFloor(int floor, Random rng)
    {
        int index = Math.Clamp(floor - 1, 0, Pools.Length - 1);
        var pool = Pools[index];

        int roll = rng.Next(100);
        string[] candidates =
            roll < 60 ? pool.Common :
            roll < 90 ? pool.Uncommon :
            pool.Rare;

        if (candidates.Length == 0) candidates = pool.Common;
        if (candidates.Length == 0) candidates = new[] { "goblin" };

        return candidates[rng.Next(candidates.Length)];
    }

    /// <summary>
    /// Returns the elite spawn chance (0–100) for the given floor.
    /// </summary>
    public static int GetEliteChanceForFloor(int floor) =>
        floor >= 8 ? 10 : floor >= 4 ? 5 : 0;
}
