namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

/// <summary>
/// Represents the raw stat values for a single item as read from the JSON configuration file.
/// Immutable once deserialised; use <see cref="ItemConfig.CreateItem"/> to convert to an <see cref="Item"/>.
/// </summary>
public record ItemStats
{
    /// <summary>The unique kebab-case identifier for this item, used for cross-referencing (e.g. in merchant inventory config).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The display name of the item as shown to the player.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The item category string (e.g. "Weapon", "Armor", "Consumable") that maps to <see cref="ItemType"/>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>The amount of HP restored when this item is consumed. Zero for non-consumable items.</summary>
    public int HealAmount { get; init; }

    /// <summary>The flat bonus added to the player's attack stat when this item is equipped as a weapon.</summary>
    public int AttackBonus { get; init; }

    /// <summary>The flat bonus added to the player's defense stat when this item is equipped as armour.</summary>
    public int DefenseBonus { get; init; }

    /// <summary>A general-purpose modifier applied to a specific derived stat for special items.</summary>
    public int StatModifier { get; init; }

    /// <summary>A short flavour or mechanical description of the item displayed in the player's inventory.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Whether this item can be placed in an equipment slot (weapons, armour, accessories).</summary>
    public bool IsEquippable { get; init; }

    /// <summary>The power tier of this item; defaults to <see cref="ItemTier.Common"/> when absent from the JSON.</summary>
    public string Tier { get; init; } = "Common";

    /// <summary>The flat bonus added to the player's dodge chance when this item is equipped, expressed in [0, 1].</summary>
    public float DodgeBonus { get; init; }

    /// <summary>The bonus added to the player's maximum mana when this item is equipped.</summary>
    public int MaxManaBonus { get; init; }

    /// <summary>The amount of mana restored to the player when this consumable item is used.</summary>
    public int ManaRestore { get; init; }

    /// <summary>The carry weight of this item. Defaults to <c>1</c>.</summary>
    public int Weight { get; init; } = 1;

    /// <summary>When true, this item is only available from merchants and must not appear in loot pools or enemy drops.</summary>
    public bool MerchantExclusive { get; init; }

    /// <summary>The gold the player receives when selling this item to a merchant.</summary>
    public int SellPrice { get; init; }
}

/// <summary>
/// The top-level wrapper object deserialised from the items JSON configuration file,
/// containing the list of all item stat definitions.
/// </summary>
public record ItemConfigData
{
    /// <summary>All item stat entries loaded from the configuration file.</summary>
    public List<ItemStats> Items { get; init; } = new();
}

/// <summary>
/// Provides static helpers for loading and validating item definitions from a JSON file
/// and for converting raw <see cref="ItemStats"/> records into runtime <see cref="Item"/> instances.
/// </summary>
public static class ItemConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Reads the specified JSON file, deserialises the item list, and validates that each entry
    /// has a non-empty name within the 30-character limit, a recognised <see cref="ItemType"/>,
    /// a recognised <see cref="ItemTier"/>, and non-negative stat values.
    /// </summary>
    /// <param name="path">Absolute or relative path to the items JSON configuration file.</param>
    /// <returns>A validated list of <see cref="ItemStats"/> records.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist at <paramref name="path"/>.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file is empty, invalid, or contains malformed item data.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an item has an unrecognised Tier string.</exception>
    public static List<ItemStats> Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Item config file not found: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ItemConfigData>(json, JsonOptions);

            if (config == null || config.Items.Count == 0)
            {
                throw new InvalidDataException($"Item config file is empty or invalid: {path}");
            }

            // Validate each item has required fields
            foreach (var item in config.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new InvalidDataException("Item missing required field: Name");
                }
                if (string.IsNullOrWhiteSpace(item.Type))
                {
                    throw new InvalidDataException($"Item '{item.Name}' missing required field: Type");
                }
                if (!Enum.TryParse<ItemType>(item.Type, ignoreCase: true, out _))
                {
                    throw new InvalidDataException($"Item '{item.Name}' has invalid Type: {item.Type}");
                }
                if (item.Name.Length > 30)
                {
                    throw new InvalidDataException($"Item name '{item.Name}' exceeds 30 character limit");
                }
                if (!Enum.TryParse<ItemTier>(item.Tier, ignoreCase: true, out _))
                {
                    throw new InvalidOperationException($"Unknown ItemTier '{item.Tier}' in item '{item.Name}'");
                }
                if (item.HealAmount < 0 || item.AttackBonus < 0 || item.DefenseBonus < 0)
                {
                    throw new InvalidDataException($"Item '{item.Name}' has negative stat values");
                }
            }

            return config.Items;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to parse item config file '{path}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns all items from <paramref name="items"/> whose <see cref="ItemStats.Tier"/> matches
    /// <paramref name="tier"/>, converted to runtime <see cref="Item"/> instances.
    /// The Boss Key is always excluded so it cannot appear in random loot pools.
    /// </summary>
    /// <param name="items">The full item list returned by <see cref="Load"/>.</param>
    /// <param name="tier">The tier to filter by.</param>
    /// <returns>A read-only list of <see cref="Item"/> objects for the requested tier.</returns>
    public static IReadOnlyList<Item> GetByTier(List<ItemStats> items, ItemTier tier)
    {
        return items
            .Where(s => s.Tier.Equals(tier.ToString(), StringComparison.OrdinalIgnoreCase)
                        && s.Name != "Boss Key"
                        && !s.MerchantExclusive)
            .Select(CreateItem)
            .ToList();
    }

    /// <summary>
    /// Constructs a runtime <see cref="Item"/> from a validated <see cref="ItemStats"/> record,
    /// parsing the type string into the corresponding <see cref="ItemType"/> enum value.
    /// </summary>
    /// <param name="stats">The item stats loaded from config.</param>
    /// <returns>A new <see cref="Item"/> populated with the provided stats.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stats"/> contains an unrecognised item type string.</exception>
    public static Item CreateItem(ItemStats stats)
    {
        if (!Enum.TryParse<ItemType>(stats.Type, ignoreCase: true, out var itemType))
        {
            throw new ArgumentException($"Invalid item type: {stats.Type}");
        }

        return new Item
        {
            Id = stats.Id,
            Name = stats.Name,
            Type = itemType,
            HealAmount = stats.HealAmount,
            AttackBonus = stats.AttackBonus,
            DefenseBonus = stats.DefenseBonus,
            StatModifier = stats.StatModifier,
            Description = stats.Description,
            IsEquippable = stats.IsEquippable,
            Tier = Enum.TryParse<ItemTier>(stats.Tier, ignoreCase: true, out var tier) ? tier : ItemTier.Common,
            DodgeBonus = stats.DodgeBonus,
            MaxManaBonus = stats.MaxManaBonus,
            ManaRestore = stats.ManaRestore,
            Weight = stats.Weight,
            MerchantExclusive = stats.MerchantExclusive,
            SellPrice = stats.SellPrice > 0 ? stats.SellPrice : ComputeSellPrice(stats)
        };
    }

    private static int ComputeSellPrice(ItemStats stats) => stats.Tier switch
    {
        "Common"   => Math.Max(1, 10 + stats.HealAmount / 4 + (stats.AttackBonus + stats.DefenseBonus) * 2),
        "Uncommon" => Math.Max(5, 20 + stats.HealAmount / 4 + (stats.AttackBonus + stats.DefenseBonus) * 3),
        "Rare"     => Math.Max(10, 35 + stats.HealAmount / 4 + (stats.AttackBonus + stats.DefenseBonus) * 4),
        _          => 5
    };

}
