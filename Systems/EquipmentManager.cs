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
            var equippable = player.Inventory.Where(i => i.IsEquippable).ToList();
            if (equippable.Count == 0)
            {
                _display.ShowError("You have no equippable items in your inventory.");
                return;
            }
            var selected = _display.ShowEquipMenuAndSelect(equippable.AsReadOnly());
            if (selected == null) return;
            DoEquip(player, selected);
            return;
        }

        var itemNameLower = itemName.ToLowerInvariant();
        var item = player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(itemNameLower));

        if (item == null)
        {
            // Pass 2: fuzzy Levenshtein distance match
            int tolerance = Math.Max(3, itemNameLower.Length / 2);
            var candidates = player.Inventory
                .Select(i => (Item: i, Distance: LevenshteinDistance(itemNameLower, i.Name.ToLowerInvariant())))
                .Where(x => x.Distance <= tolerance)
                .ToList();

            if (candidates.Count == 0)
            {
                _display.ShowError($"You don't have '{itemName}' in your inventory.");
                return;
            }

            int bestDistance = candidates.Min(x => x.Distance);
            var bestCandidates = candidates.Where(x => x.Distance == bestDistance).ToList();

            if (bestCandidates.Count > 1)
            {
                var names = string.Join(", ", bestCandidates.Select(x => x.Item.Name));
                _display.ShowError($"Did you mean one of: {names}? Please be more specific.");
                return;
            }

            item = bestCandidates[0].Item;
            _display.ShowMessage($"(Did you mean \"{item.Name}\"?)");
        }

        if (!item.IsEquippable)
        {
            _display.ShowError($"{item.Name} cannot be equipped.");
            return;
        }

        DoEquip(player, item);
    }

    private void DoEquip(Player player, Item item)
    {
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
            ItemType.Armor     => player.GetArmorSlotItem(item.Slot == ArmorSlot.None ? ArmorSlot.Chest : item.Slot),
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
        _display.ShowEquipment(player);
    }

    /// <summary>Computes the Levenshtein edit distance between two strings.</summary>
    internal static int LevenshteinDistance(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var dp = new int[m + 1, n + 1];
        for (int i = 0; i <= m; i++) dp[i, 0] = i;
        for (int j = 0; j <= n; j++) dp[0, j] = j;
        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
        return dp[m, n];
    }
}
