namespace Dungnz.Models;

public class Item
{
    public string Name { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public int StatModifier { get; set; }
    public string Description { get; set; } = string.Empty;
    public int AttackBonus { get; set; }
    public int DefenseBonus { get; set; }
    public int HealAmount { get; set; }
    
    public bool IsEquippable => Type is ItemType.Weapon or ItemType.Armor;
}
