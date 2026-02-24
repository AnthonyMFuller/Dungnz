namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

/// <summary>
/// Raw per-floor merchant stock definition as read from merchant-inventory.json.
/// </summary>
public record MerchantFloorConfig
{
    /// <summary>The dungeon floor number this config applies to.</summary>
    public int Floor { get; init; }

    /// <summary>Item IDs that are always included in the merchant's stock.</summary>
    public List<string> Guaranteed { get; init; } = new();

    /// <summary>Pool of item IDs from which additional stock is randomly drawn.</summary>
    public List<string> Pool { get; init; } = new();

    /// <summary>Total number of items the merchant stocks (guaranteed + random fill).</summary>
    public int StockCount { get; init; }
}

/// <summary>
/// Top-level wrapper deserialised from merchant-inventory.json.
/// </summary>
public record MerchantInventoryData
{
    /// <summary>Per-floor merchant stock configurations.</summary>
    public List<MerchantFloorConfig> Floors { get; init; } = new();
}

/// <summary>
/// Loads and resolves merchant stock from Data/merchant-inventory.json.
/// Provides floor-appropriate item selection, falling back gracefully if the file is unavailable.
/// </summary>
public static class MerchantInventoryConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static MerchantInventoryData? _cachedData;

    /// <summary>
    /// Computes a sale price for an item based on its tier and primary stats.
    /// </summary>
    private static int ComputePrice(Item item) => item.Tier switch
    {
        ItemTier.Common    => 15 + item.HealAmount + (item.AttackBonus + item.DefenseBonus) * 5,
        ItemTier.Uncommon  => 40 + item.HealAmount + (item.AttackBonus + item.DefenseBonus) * 6,
        ItemTier.Rare      => 80 + item.HealAmount + (item.AttackBonus + item.DefenseBonus) * 8,
        _                  => 20
    };

    /// <summary>
    /// Returns merchant stock appropriate for the given floor by resolving item IDs against
    /// <paramref name="allItems"/>. Guaranteed items are always included; additional slots are
    /// filled randomly from the pool up to <c>StockCount</c>. Floors above 5 use the floor 5 config.
    /// Falls back to an empty list if the JSON file cannot be loaded.
    /// </summary>
    /// <param name="floor">The dungeon floor number (1–5).</param>
    /// <param name="allItems">All available items, used to resolve IDs to <see cref="Item"/> instances.</param>
    /// <param name="rng">Random number generator for pool selection.</param>
    /// <returns>A list of <see cref="MerchantItem"/> instances ready to stock a <see cref="Merchant"/>.</returns>
    public static List<MerchantItem> GetStockForFloor(int floor, IReadOnlyList<Item> allItems, Random rng)
    {
        var data = LoadData();
        if (data == null) return new List<MerchantItem>();

        var clampedFloor = Math.Clamp(floor, 1, 5);
        var config = data.Floors.Find(f => f.Floor == clampedFloor)
                     ?? data.Floors.Find(f => f.Floor == 1);

        if (config == null) return new List<MerchantItem>();

        var byId = allItems.Where(i => !string.IsNullOrEmpty(i.Id))
                           .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

        var stock = new List<MerchantItem>();

        // Add guaranteed items first
        foreach (var id in config.Guaranteed)
        {
            if (byId.TryGetValue(id, out var item))
                stock.Add(new MerchantItem { Item = item, Price = ComputePrice(item) });
        }

        // Fill remaining slots from pool (exclude already-stocked IDs)
        var stockedIds = new HashSet<string>(config.Guaranteed, StringComparer.OrdinalIgnoreCase);
        var available = config.Pool
            .Where(id => !stockedIds.Contains(id) && byId.ContainsKey(id))
            .OrderBy(_ => rng.Next())
            .ToList();

        int remaining = Math.Max(0, config.StockCount - stock.Count);
        foreach (var id in available.Take(remaining))
        {
            if (byId.TryGetValue(id, out var item))
                stock.Add(new MerchantItem { Item = item, Price = ComputePrice(item) });
        }

        return stock;
    }

    private static MerchantInventoryData? LoadData()
    {
        if (_cachedData != null) return _cachedData;

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "merchant-inventory.json");
        if (!File.Exists(path))
            path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "merchant-inventory.json");

        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            _cachedData = JsonSerializer.Deserialize<MerchantInventoryData>(json, JsonOptions);
            return _cachedData;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Clears the cached config data (used in tests to force a fresh load).</summary>
    internal static void ClearCache() => _cachedData = null;
}
