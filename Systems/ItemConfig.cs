namespace Dungnz.Systems;

using System.Text.Json;
using Dungnz.Models;

public record ItemStats
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int HealAmount { get; init; }
    public int AttackBonus { get; init; }
    public int DefenseBonus { get; init; }
    public int StatModifier { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record ItemConfigData
{
    public List<ItemStats> Items { get; init; } = new();
}

public static class ItemConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

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
            Description = stats.Description
        };
    }
}
