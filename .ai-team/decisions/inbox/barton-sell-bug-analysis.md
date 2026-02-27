# Sell Bug Root Cause Analysis

## Summary
Items picked up on Floor 1 cannot be sold at merchants. Investigation shows the code logic is sound — all items from JSON have valid `Type` values, and the sell filter `i.Type != ItemType.Gold` should work correctly.

## Investigation Results

### Item Flow Trace
1. **JSON Load** (`Program.cs:20`)
   - Items loaded via `ItemConfig.Load("Data/item-stats.json")`
   - All items validated for Type field (throws on invalid/missing)
   - Floor 1 items confirmed: Health Potion (Consumable), Iron Sword (Weapon), Leather Armor (Armor)

2. **Item Creation**
   - `ItemConfig.CreateItem()` explicitly sets `Type` from parsed enum
   - Validation at lines 186-189 throws on invalid types
   - All Floor 1 items have valid non-Gold types

3. **Item Cloning** (Room loot / Combat drops)
   - `Item.Clone()` uses `MemberwiseClone()` which DOES copy enum values
   - Tested: `DungeonGenerator.CreateRandomItem()` line 261
   - Tested: `LootTable.RollDrop()` line 193

4. **Sell Filter** (`GameLoop.cs:1278`)
   ```csharp
   foreach (var item in _player.Inventory.Where(i => i.Type != ItemType.Gold))
       list.Add((item, false));
   ```
   - Filters items where `Type == ItemType.Gold`
   - Default ItemType is `Weapon` (0), NOT `Gold` (4)
   - Filter logic is correct

5. **Display Layer** (`DisplayService.cs:482-502`)
   - `ShowSellMenu()` renders items as provided
   - No additional filtering

### ItemType Enum Values
```csharp
Weapon = 0      // Default value
Armor = 1
Accessory = 2
Consumable = 3
Gold = 4
```

## Root Cause

**The logical code path is correct. The bug must be in one of these areas:**

1. ❓ **Runtime State Corruption** — Item.Type being modified after pickup
2. ❓ **Edge Case Item** — Specific item in JSON with unexpected Type value
3. ❓ **Input Selection Bug** — Items display but selection fails silently

## Recommended Fix

Add diagnostic logging to `BuildSellableList()` to identify the actual issue:

**File:** `Engine/GameLoop.cs` lines 1273-1288

```csharp
private List<(Item Item, bool IsEquipped)> BuildSellableList()
{
    var list = new List<(Item, bool)>();
    
    // DEBUG: Log inventory state
    System.Console.WriteLine($"[DEBUG] Inventory count: {_player.Inventory.Count}");
    foreach (var item in _player.Inventory)
    {
        var isGold = item.Type == ItemType.Gold;
        System.Console.WriteLine($"[DEBUG] {item.Name} | Type: {item.Type} | IsGold: {isGold}");
    }
    
    // Unequipped inventory items (excluding loose gold coins)
    foreach (var item in _player.Inventory.Where(i => i.Type != ItemType.Gold))
        list.Add((item, false));
    
    System.Console.WriteLine($"[DEBUG] Sellable items: {list.Count}");
    
    // Equipped items — selling auto-unequips
    if (_player.EquippedWeapon    != null) list.Add((_player.EquippedWeapon,    true));
    if (_player.EquippedAccessory != null) list.Add((_player.EquippedAccessory, true));
    foreach (var a in _player.AllEquippedArmor)
        list.Add((a, true));
    
    return list;
}
```

## Next Steps

1. Add debug logging as shown above
2. Reproduce bug with logging enabled
3. Check console output for item Type values
4. If all items show correct Type but still filtered out, investigate LINQ execution timing

## Files Requiring Changes

- `Engine/GameLoop.cs` lines 1273-1288 — Add diagnostic logging
