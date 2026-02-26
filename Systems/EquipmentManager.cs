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

        if (item.ClassRestriction != null && item.ClassRestriction.Length > 0
            && !item.ClassRestriction.Contains(player.Class.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            var allowed = string.Join(", ", item.ClassRestriction);
            _display.ShowError($"Only {allowed} can equip the {item.Name}.");
            return;
        }

        // Weight check: equipping swaps new item into equipped slot and old item back into
        // inventory. If the old item is heavier than the new one, inventory weight increases.
        var currentlyEquipped = item.Type switch
        {
            ItemType.Weapon    => player.EquippedWeapon,
            ItemType.Armor     => player.GetArmorSlot(item.Slot == ArmorSlot.None ? ArmorSlot.Chest : item.Slot),
            ItemType.Accessory => player.EquippedAccessory,
            _                  => null
        };
        int inventoryWeightAfterSwap = player.Inventory.Sum(i => i.Weight)
            - item.Weight
            + (currentlyEquipped?.Weight ?? 0);
        if (inventoryWeightAfterSwap > InventoryManager.MaxWeight)
        {
            _display.ShowError($"Equipping {item.Name} would exceed your carry weight limit.");
            return;
        }

        try
        {
            // Show equipment comparison before equipping
            _display.ShowEquipmentComparison(player, currentlyEquipped, item);
            
            player.EquipItem(item);
            SetBonusManager.ApplySetBonuses(player);

            // Ring of Haste: apply cooldown reduction at equip time
            if (item.PassiveEffectId == "cooldown_reduction")
            {
                // AbilityManager is not injected here; reduction fires at next combat start via CombatEngine
                _display.ShowMessage("⚡ Ring of Haste — cooldowns will be reduced at the start of your next combat.");
            }

            // Display active set bonus if any
            var setDesc = SetBonusManager.GetActiveBonusDescription(player);
            if (!string.IsNullOrEmpty(setDesc))
                _display.ShowColoredMessage($"✦ Set bonus active: {setDesc}", ColorCodes.Yellow);

            _display.ShowMessage($"✓ Equipped {item.Name}");
            _display.ShowMessage(ItemInteractionNarration.Equip(item));
            if (!string.IsNullOrEmpty(item.Description))
                _display.ShowMessage($"  {item.Description}");
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
            _display.ShowError("Unequip what? Specify WEAPON, ACCESSORY, or an armor slot: HEAD, SHOULDERS, CHEST, HANDS, LEGS, FEET, BACK, OFFHAND.");
            return;
        }

        try
        {
            var item = player.UnequipItem(slotName);
            SetBonusManager.ApplySetBonuses(player);
            _display.ShowMessage($"You unequip {item!.Name} and return it to your inventory.");
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

    /// <summary>Displays the player's currently equipped items in a structured 8-slot layout.</summary>
    public void ShowEquipment(Player player)
    {
        const string border  = "══════════════════════════════════════════";
        const string sep     = "╠══════════════════════════════════════════╣";
        const int    inner   = 42; // visible characters inside the ║ … ║ walls

        _display.ShowMessage($"╔{border}╗");
        _display.ShowMessage($"║{"              EQUIPMENT                   ".PadRight(inner)}║");
        _display.ShowMessage(sep);

        // --- Weapon ---
        if (player.EquippedWeapon != null)
        {
            var w       = player.EquippedWeapon;
            var atkVal  = $"{ColorCodes.BrightRed}+{w.AttackBonus}{ColorCodes.Reset}";
            var extras  = new System.Collections.Generic.List<string>();
            if (w.DodgeBonus   > 0) extras.Add($"+{w.DodgeBonus:P0} dodge");
            if (w.PoisonImmunity)   extras.Add("poison immune");
            if (w.MaxManaBonus > 0) extras.Add($"+{w.MaxManaBonus} mana");
            var extStr  = extras.Count > 0 ? $", {string.Join(", ", extras)}" : "";
            var content = $"⚔  Weapon:    {ColorizeItemName(w)} (ATK {atkVal}{extStr})";
            _display.ShowMessage($"║ {PadRightVisible(content, inner - 1)}║");
        }
        else
        {
            _display.ShowMessage($"║ ⚔  Weapon:    {ColorCodes.Gray}[Empty]{ColorCodes.Reset}{"".PadRight(inner - 21)}║");
        }

        // --- Accessory ---
        if (player.EquippedAccessory != null)
        {
            var acc    = player.EquippedAccessory;
            var stats  = new System.Collections.Generic.List<string>();
            if (acc.AttackBonus  != 0) stats.Add($"ATK {ColorCodes.BrightRed}+{acc.AttackBonus}{ColorCodes.Reset}");
            if (acc.DefenseBonus != 0) stats.Add($"DEF {ColorCodes.Cyan}+{acc.DefenseBonus}{ColorCodes.Reset}");
            if (acc.StatModifier != 0) stats.Add($"HP +{acc.StatModifier}");
            if (acc.DodgeBonus   > 0)  stats.Add($"+{acc.DodgeBonus:P0} dodge");
            if (acc.MaxManaBonus > 0)  stats.Add($"+{acc.MaxManaBonus} mana");
            var statStr = stats.Count > 0 ? $" ({string.Join(", ", stats)})" : "";
            var content = $"💍 Accessory: {ColorizeItemName(acc)}{statStr}";
            _display.ShowMessage($"║ {PadRightVisible(content, inner - 1)}║");
        }
        else
        {
            _display.ShowMessage($"║ 💍 Accessory: {ColorCodes.Gray}[Empty]{ColorCodes.Reset}{"".PadRight(inner - 22)}║");
        }

        _display.ShowMessage(sep);

        // --- 8 armor slots ---
        ShowArmorSlot(player.EquippedHead,      "🪖 Head:     ", inner);
        ShowArmorSlot(player.EquippedShoulders,  "🥋 Shoulders:", inner);
        ShowArmorSlot(player.EquippedChest,      "🛡 Chest:    ", inner);
        ShowArmorSlot(player.EquippedHands,      "🧤 Hands:    ", inner);
        ShowArmorSlot(player.EquippedLegs,       "👖 Legs:     ", inner);
        ShowArmorSlot(player.EquippedFeet,       "👟 Feet:     ", inner);
        ShowArmorSlot(player.EquippedBack,       "🧥 Back:     ", inner);
        ShowArmorSlot(player.EquippedOffHand,    "⛨  Off-Hand: ", inner);

        _display.ShowMessage($"╚{border}╝");
    }

    private void ShowArmorSlot(Item? item, string label, int inner)
    {
        if (item != null)
        {
            var defVal  = $"{ColorCodes.Cyan}+{item.DefenseBonus}{ColorCodes.Reset}";
            var extras  = new System.Collections.Generic.List<string>();
            if (item.DodgeBonus   > 0) extras.Add($"+{item.DodgeBonus:P0} dodge");
            if (item.PoisonImmunity)   extras.Add("poison immune");
            if (item.MaxManaBonus > 0) extras.Add($"+{item.MaxManaBonus} mana");
            var extStr  = extras.Count > 0 ? $", {string.Join(", ", extras)}" : "";
            var content = $"{label} {ColorizeItemName(item)} (DEF {defVal}{extStr})";
            _display.ShowMessage($"║ {PadRightVisible(content, inner - 1)}║");
        }
        else
        {
            var emptyLine = $"{label} {ColorCodes.Gray}[Empty]{ColorCodes.Reset}";
            _display.ShowMessage($"║ {PadRightVisible(emptyLine, inner - 1)}║");
        }
    }

    /// <summary>Pads <paramref name="text"/> to <paramref name="width"/> visible characters, ignoring ANSI escape codes.</summary>
    private static string PadRightVisible(string text, int width)
    {
        var visible = ColorCodes.StripAnsiCodes(text).Length;
        var padding = Math.Max(0, width - visible);
        return text + new string(' ', padding);
    }

    private static string ColorizeItemName(Item item)
    {
        return Systems.ColorCodes.ColorizeItemName(item.Name, item.Tier);
    }
}
