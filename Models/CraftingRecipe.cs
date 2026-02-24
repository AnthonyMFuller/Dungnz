namespace Dungnz.Models;

/// <summary>Defines a single crafting recipe loaded from JSON, specifying ingredients, gold cost, and the result item produced.</summary>
public record CraftingRecipe
{
    /// <summary>Gets the unique slug identifier for this recipe (e.g. "health-elixir").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the display name of this recipe shown to the player.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the list of ingredients required to craft this recipe.</summary>
    public List<RecipeIngredient> Ingredients { get; init; } = new();

    /// <summary>Gets the gold cost paid in addition to providing the required ingredients.</summary>
    public int GoldCost { get; init; } = 0;

    /// <summary>Gets flavour or mechanical description text shown in the recipe list.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the stats of the item produced when this recipe is crafted.</summary>
    public RecipeResult Result { get; init; } = new();
}

/// <summary>Describes a single ingredient required by a <see cref="CraftingRecipe"/>.</summary>
public record RecipeIngredient
{
    /// <summary>Gets the slug identifier of the required item (e.g. "health-potion").</summary>
    public string ItemId { get; init; } = string.Empty;

    /// <summary>Gets the display name used for inventory matching and error messages.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the number of this ingredient required.</summary>
    public int Count { get; init; } = 1;
}

/// <summary>Describes the item produced when a <see cref="CraftingRecipe"/> is successfully crafted.</summary>
public record RecipeResult
{
    /// <summary>Gets the slug identifier of the produced item.</summary>
    public string ItemId { get; init; } = string.Empty;

    /// <summary>Gets the display name of the produced item.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the item type string (e.g. "Consumable", "Weapon", "Armor").</summary>
    public string Type { get; init; } = "Consumable";

    /// <summary>Gets the item tier string (e.g. "Common", "Uncommon", "Rare").</summary>
    public string Tier { get; init; } = "Common";

    /// <summary>Gets the HP restored when this consumable is used.</summary>
    public int HealAmount { get; init; } = 0;

    /// <summary>Gets the attack bonus granted when this weapon is equipped.</summary>
    public int AttackBonus { get; init; } = 0;

    /// <summary>Gets the defense bonus granted when this armor is equipped.</summary>
    public int DefenseBonus { get; init; } = 0;

    /// <summary>Gets whether this item can be placed in an equipment slot.</summary>
    public bool IsEquippable { get; init; } = false;

    /// <summary>Gets the flavour or mechanical description shown in inventory.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Constructs a runtime <see cref="Item"/> from this result definition.</summary>
    public Item ToItem() => new Item
    {
        Name = Name,
        Type = Enum.TryParse<ItemType>(Type, ignoreCase: true, out var t) ? t : ItemType.Consumable,
        HealAmount = HealAmount,
        AttackBonus = AttackBonus,
        DefenseBonus = DefenseBonus,
        IsEquippable = IsEquippable,
        Description = Description,
        Tier = Enum.TryParse<ItemTier>(Tier, ignoreCase: true, out var tier) ? tier : ItemTier.Common
    };
}
