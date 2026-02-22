namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

/// <summary>
/// Represents the raw stat values for a single item as read from the JSON configuration file.
/// Immutable once deserialised; use <see cref="ItemConfig.CreateItem"/> to convert to an <see cref="Item"/>.
/// </summary>
public record ItemStats
{
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
    /// has a non-empty name, a recognised <see cref="ItemType"/>, and non-negative stat values.
    /// </summary>
    /// <param name="path">Absolute or relative path to the items JSON configuration file.</param>
    /// <returns>A validated list of <see cref="ItemStats"/> records.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist at <paramref name="path"/>.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file is empty, invalid, or contains malformed item data.</exception>
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
            Name = stats.Name,
            Type = itemType,
            HealAmount = stats.HealAmount,
            AttackBonus = stats.AttackBonus,
            DefenseBonus = stats.DefenseBonus,
            StatModifier = stats.StatModifier,
            Description = stats.Description,
            IsEquippable = stats.IsEquippable,
            Tier = Enum.TryParse<ItemTier>(stats.Tier, ignoreCase: true, out var tier) ? tier : ItemTier.Common
        };
    }
}
