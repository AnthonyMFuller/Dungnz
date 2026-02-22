namespace Dungnz.Systems;

using System;
using System.Linq;
using Dungnz.Display;
using Dungnz.Models;

/// <summary>
/// Handles EQUIP, UNEQUIP, and EQUIPMENT commands, extracted from <see cref="Dungnz.Engine.GameLoop"/>.
/// </summary>
public class EquipmentManager
{
    private readonly IDisplayService _display;

    /// <summary>Initialises a new <see cref="EquipmentManager"/> with the given display service.</summary>
    /// <param name="display">The display service used to output messages and errors to the player.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="display"/> is <see langword="null"/>.</exception>
    public EquipmentManager(IDisplayService display)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
    }

    /// <summary>Finds an item in the player's inventory by name (case-insensitive contains), validates it, and equips it.</summary>
    public void HandleEquip(Player player, string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            _display.ShowError("Equip what? Specify an item name.");
            return;
        }

        var itemNameLower = itemName.ToLowerInvariant();
        var item = player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            _display.ShowError($"You don't have '{itemName}' in your inventory.");
            return;
        }

        if (!item.IsEquippable)
        {
            _display.ShowError($"{item.Name} cannot be equipped.");
            return;
        }

        try
        {
            player.EquipItem(item);
            _display.ShowMessage($"You equip {item.Name}. Attack: {player.Attack}, Defense: {player.Defense}");
            if (!string.IsNullOrEmpty(item.Description))
                _display.ShowMessage(item.Description);
        }
        catch (ArgumentException ex)
        {
            _display.ShowError(ex.Message);
        }
    }

    /// <summary>Unequips the item in the specified slot and returns it to inventory.</summary>
    public void HandleUnequip(Player player, string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName))
        {
            _display.ShowError("Unequip what? Specify WEAPON, ARMOR, or ACCESSORY.");
            return;
        }

        try
        {
            var item = player.UnequipItem(slotName);
            if (item == null)
                _display.ShowMessage($"The {slotName} slot is already empty.");
            else
                _display.ShowMessage($"You unequip {item.Name} and return it to your inventory.");
        }
        catch (InvalidOperationException ex)
        {
            _display.ShowError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _display.ShowError(ex.Message);
        }
    }

    /// <summary>Displays the player's currently equipped weapon, armor, and accessory.</summary>
    public void ShowEquipment(Player player)
    {
        _display.ShowMessage("=== EQUIPMENT ===");

        if (player.EquippedWeapon != null)
        {
            var w = player.EquippedWeapon;
            var wStats = new System.Collections.Generic.List<string> { $"Attack +{w.AttackBonus}" };
            if (w.DodgeBonus > 0) wStats.Add($"+{w.DodgeBonus:P0} dodge");
            if (w.PoisonImmunity) wStats.Add("poison immune");
            if (w.MaxManaBonus > 0) wStats.Add($"+{w.MaxManaBonus} max mana");
            _display.ShowMessage($"Weapon: {w.Name} ({string.Join(", ", wStats)})");
        }
        else
        {
            _display.ShowMessage("Weapon: (empty)");
        }

        if (player.EquippedArmor != null)
        {
            var a = player.EquippedArmor;
            var aStats = new System.Collections.Generic.List<string> { $"Defense +{a.DefenseBonus}" };
            if (a.DodgeBonus > 0) aStats.Add($"+{a.DodgeBonus:P0} dodge");
            if (a.PoisonImmunity) aStats.Add("poison immune");
            if (a.MaxManaBonus > 0) aStats.Add($"+{a.MaxManaBonus} max mana");
            _display.ShowMessage($"Armor: {a.Name} ({string.Join(", ", aStats)})");
        }
        else
        {
            _display.ShowMessage("Armor: (empty)");
        }

        if (player.EquippedAccessory != null)
        {
            var acc = player.EquippedAccessory;
            var stats = new System.Collections.Generic.List<string>();
            if (acc.AttackBonus != 0) stats.Add($"Attack +{acc.AttackBonus}");
            if (acc.DefenseBonus != 0) stats.Add($"Defense +{acc.DefenseBonus}");
            if (acc.StatModifier != 0) stats.Add($"HP +{acc.StatModifier}");
            if (acc.DodgeBonus > 0) stats.Add($"+{acc.DodgeBonus:P0} dodge");
            if (acc.PoisonImmunity) stats.Add("poison immune");
            if (acc.MaxManaBonus > 0) stats.Add($"+{acc.MaxManaBonus} max mana");
            _display.ShowMessage($"Accessory: {acc.Name} ({string.Join(", ", stats)})");
        }
        else
        {
            _display.ShowMessage("Accessory: (empty)");
        }
    }
}
