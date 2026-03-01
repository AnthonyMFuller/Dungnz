using Dungnz.Models;

namespace Dungnz.Tests.Builders;

/// <summary>
/// Fluent builder for creating <see cref="Item"/> instances in tests.
/// </summary>
public class ItemBuilder
{
    private string _name = "Test Item";
    private string _id = "test-item";
    private ItemType _type = ItemType.Weapon;
    private int _attackBonus;
    private int _defenseBonus;
    private int _healAmount;
    private int _sellPrice;
    private ItemTier _tier = ItemTier.Common;
    private bool _isEquippable = true;
    private ArmorSlot _slot = ArmorSlot.None;
    private string _description = "A test item.";

    public ItemBuilder Named(string name) { _name = name; return this; }
    public ItemBuilder WithId(string id) { _id = id; return this; }
    public ItemBuilder OfType(ItemType type) { _type = type; return this; }
    public ItemBuilder WithDamage(int atk) { _attackBonus = atk; _type = ItemType.Weapon; return this; }
    public ItemBuilder WithDefense(int def) { _defenseBonus = def; _type = ItemType.Armor; return this; }
    public ItemBuilder WithHeal(int amount) { _healAmount = amount; _type = ItemType.Consumable; _isEquippable = false; return this; }
    public ItemBuilder WithSellPrice(int price) { _sellPrice = price; return this; }
    public ItemBuilder WithTier(ItemTier tier) { _tier = tier; return this; }
    public ItemBuilder AsEquippable(bool equippable = true) { _isEquippable = equippable; return this; }
    public ItemBuilder WithSlot(ArmorSlot slot) { _slot = slot; return this; }
    public ItemBuilder WithDescription(string desc) { _description = desc; return this; }

    public Item Build()
    {
        return new Item
        {
            Id = _id,
            Name = _name,
            Type = _type,
            AttackBonus = _attackBonus,
            DefenseBonus = _defenseBonus,
            HealAmount = _healAmount,
            SellPrice = _sellPrice,
            Tier = _tier,
            IsEquippable = _isEquippable,
            Slot = _slot,
            Description = _description
        };
    }
}
