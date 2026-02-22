namespace Dungnz.Systems;

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

    /// <summary>Picks a random entry from the pool.</summary>
    public string Pick(string[] pool) => pool[_rng.Next(pool.Length)];

    /// <summary>Picks a random entry, formatting with the provided args.</summary>
    public string Pick(string[] pool, params object[] args) => string.Format(Pick(pool), args);

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
}
