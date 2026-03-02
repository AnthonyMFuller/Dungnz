# Implementation Decision: Inventory Inspect & Compare Features

**Author:** Hill (C# Dev)  
**Date:** 2026-03-02  
**Status:** Implemented  
**Related Issues:** #844 (COMPARE command), #845 (Enhanced EXAMINE), #846 (Interactive INVENTORY)  
**PR:** #847 — `squad/844-845-846-inspect-compare`  
**Design Spec:** `.ai-team/decisions/inbox/coulson-inspect-compare-design.md`

---

## Implementation Summary

Successfully implemented three inventory UX improvements per Coulson's design spec:

1. **COMPARE command** — side-by-side stat comparison with interactive menu when no argument
2. **Enhanced EXAMINE** — auto-shows comparison for equippable inventory items
3. **Interactive INVENTORY** — arrow-key selection with detail/comparison display

All changes are non-breaking, reuse existing display methods, and follow established command patterns.

---

## Technical Decisions

### 1. Slot Resolution Strategy

**Decision:** Created `GetCurrentlyEquippedForItem(Item)` helper that mirrors exact logic from `EquipmentManager.DoEquip`.

**Rationale:**
- Single source of truth for slot resolution rules
- Handles `ArmorSlot.None` → `Chest` default consistently
- Avoids duplication of EquipmentManager logic

**Code:**
```csharp
private Item? GetCurrentlyEquippedForItem(Item item)
{
    return item.Type switch
    {
        ItemType.Weapon    => _player.EquippedWeapon,
        ItemType.Armor     => _player.GetArmorSlotItem(item.Slot == ArmorSlot.None ? ArmorSlot.Chest : item.Slot),
        ItemType.Accessory => _player.EquippedAccessory,
        _                  => null
    };
}
```

**Alternative considered:** Direct inline checks in each call site → Rejected due to DRY violation and maintenance burden.

---

### 2. Interactive Inventory Behavior

**Decision:** Modified `CommandType.Inventory` dispatcher to call `ShowInventoryAndSelect`, then conditionally show detail/comparison if item selected.

**Key implementation:**
```csharp
case CommandType.Inventory:
    var selectedItem = _display.ShowInventoryAndSelect(_player);
    _turnConsumed = false;  // viewing inventory is never a turn
    if (selectedItem != null)
    {
        _display.ShowItemDetail(selectedItem);
        if (selectedItem.IsEquippable)
        {
            var equipped = GetCurrentlyEquippedForItem(selectedItem);
            _display.ShowEquipmentComparison(_player, equipped, selectedItem);
        }
    }
    break;
```

**Rationale:**
- Preserves existing "inventory doesn't consume a turn" rule
- Cancellation (null return) is graceful — no error message
- Progressive disclosure: list → selection → detail → comparison

**Alternative considered:** Keep `ShowInventory` as-is, add separate `INSPECT` command → Rejected as unnecessarily verbose for common workflow.

---

### 3. COMPARE Command: Interactive Selection

**Decision:** When no argument provided, filter inventory to equippable-only and call `ShowEquipMenuAndSelect`.

**Rationale:**
- Consistent with EQUIP command pattern (no-arg = interactive menu)
- User already sees full inventory via INVENTORY command
- COMPARE is specifically about equipment stats, not browsing consumables

**Error handling:**
- No equippable items → "You have no equippable items to compare." (no turn consumed)
- Item not found → "You don't have '{itemName}' in your inventory." (no turn consumed)
- Item not equippable → "{item.Name} cannot be equipped, so there's nothing to compare." (no turn consumed)

**Alternative considered:** Show full inventory in selection → Rejected as noisy (consumables/materials not relevant to comparison).

---

### 4. Enhanced EXAMINE: Scope Limitation

**Decision:** Only show auto-comparison for *inventory* items that are equippable. Room items and enemies remain unchanged.

**Code change:**
```csharp
// Check items in inventory
var invItem = _player.Inventory.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains(targetLower));
if (invItem != null)
{
    _display.ShowItemDetail(invItem);
    
    // If equippable, show comparison vs. currently equipped
    if (invItem.IsEquippable)
    {
        Item? currentlyEquipped = GetCurrentlyEquippedForItem(invItem);
        _display.ShowEquipmentComparison(_player, currentlyEquipped, invItem);
    }
    
    return;
}
```

**Rationale:**
- Non-breaking: room items (not yet picked up) don't have "currently equipped" context
- Avoids confusing comparison for items player doesn't own yet
- Matches user mental model: "I examine what I *have*, see if it's better than what I'm *wearing*"

**Alternative considered:** Show comparison for all equippable items → Rejected as violating UX principle (comparison only makes sense for owned items).

---

### 5. Display Service Implementations

**Decision:** 
- SpectreDisplayService uses `SelectionPrompt<string>` with item names + cancel option
- DisplayService (fallback) uses numbered text input with 'x' to cancel

**SpectreDisplayService pattern:**
```csharp
var items = player.Inventory.ToList();
var prompt = new SelectionPrompt<string>()
    .Title("[yellow]Select an item:[/]")
    .PageSize(12)
    .MoreChoicesText("[grey](Move up and down to see more items)[/]")
    .AddChoices(items.Select(i => i.Name))
    .AddChoices("[grey]« Cancel »[/]");

var selection = AnsiConsole.Prompt(prompt);
if (selection == "[grey]« Cancel »[/]") return null;
return items.FirstOrDefault(i => i.Name == selection);
```

**DisplayService pattern:**
```csharp
Console.Write("Enter item number (or 'x' to cancel): ");
var input = Console.ReadLine()?.Trim() ?? "";
if (input.Equals("x", StringComparison.OrdinalIgnoreCase)) return null;
if (int.TryParse(input, out int index) && index >= 1 && index <= player.Inventory.Count)
    return player.Inventory[index - 1];
return null; // Invalid input treated as cancel
```

**Rationale:**
- Matches existing "AndSelect" method patterns (`ShowEquipMenuAndSelect`, `ShowShopAndSelect`)
- Graceful degradation for environments without Spectre.Console
- 1-based indexing matches ShowInventory display

**Alternative considered:** Return error on invalid input → Rejected as unnecessarily harsh (cancellation is user's intent).

---

### 6. Turn Consumption Rules

**Decision:** COMPARE and INVENTORY never consume turns. EXAMINE already follows this rule.

**Implementation:**
- COMPARE: `_turnConsumed = false` on all code paths (no arg, error, success)
- INVENTORY: `_turnConsumed = false` set before `if (selectedItem != null)` check
- EXAMINE: `_turnConsumed = false` already set on error paths; comparison doesn't change this

**Rationale:**
- Info-only commands should not advance game state
- Consistent with existing command classification (LOOK, STATS, HELP, etc.)
- Allows player to inspect equipment freely without penalty

---

## Testing Strategy

**Unit tests implemented by Romanoff** on separate branch `squad/846-inspect-compare-tests`:
- CommandParser: `compare` and `comp` parsing with/without arguments
- HandleCompare: item-name argument, no-arg menu, error cases (no items, not found, not equippable)
- HandleExamine: auto-comparison for inventory items
- ShowInventoryAndSelect: full inventory flow, cancel handling

**Integration testing deferred** to Romanoff's test suite per team protocol.

---

## Pre-Push Hook: README Update

**Requirement:** Changes to `Engine/` require README.md update.

**Changes made:**
1. `examine <target>` row: Added "for equippable inventory items, auto-shows comparison vs. currently equipped"
2. `inventory` row: Changed from "List carried items" to "Interactive item browser with arrow-key selection; displays details and comparison for selected equippable items"
3. **New row:** `compare <item>` | `comp` | "Display side-by-side stat comparison for an inventory item vs. currently equipped gear; omit item name for interactive menu"

---

## Build Verification

**Command:** `dotnet build --no-restore`  
**Result:** Build succeeded with 0 errors  
**Warnings:** 5 pre-existing XML doc warnings (CommandParser, DisplayService, GoblinShamanAI) — unrelated to this change

---

## Files Modified

1. **Engine/CommandParser.cs** (3 edits)
   - Added `Compare` enum value after `Leaderboard`
   - Added `"compare" or "comp"` switch case
   - Added "compare", "comp" to `knownVerbs` fuzzy-match array

2. **Engine/GameLoop.cs** (4 edits)
   - Added `case CommandType.Compare:` dispatcher
   - Modified `case CommandType.Inventory:` to use `ShowInventoryAndSelect`
   - Enhanced `HandleExamine` inventory block with comparison
   - Added `GetCurrentlyEquippedForItem` and `HandleCompare` methods

3. **Display/IDisplayService.cs** (1 edit)
   - Added `ShowInventoryAndSelect(Player)` method signature

4. **Display/SpectreDisplayService.cs** (1 edit)
   - Implemented `ShowInventoryAndSelect` with `SelectionPrompt`

5. **Display/DisplayService.cs** (1 edit)
   - Implemented `ShowInventoryAndSelect` with numbered fallback

6. **Dungnz.Tests/Helpers/FakeDisplayService.cs** (1 edit)
   - Added `ShowInventoryAndSelect` stub with input reader support

7. **Dungnz.Tests/Helpers/TestDisplayService.cs** (1 edit)
   - Added `ShowInventoryAndSelect` stub returning null

8. **README.md** (1 edit)
   - Updated commands table with new functionality

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Slot resolution diverges from EquipmentManager | Used exact same switch logic; future refactor should extract to shared helper |
| ShowInventoryAndSelect breaks backward compatibility | New method added to interface; existing ShowInventory unchanged |
| Interactive menus don't work in CI/test environments | Test helpers (FakeDisplayService, TestDisplayService) provide stubs that return null or read from injected input |
| Turn consumption inconsistency | Explicitly set `_turnConsumed = false` on all info-only paths; verified against existing commands |

---

## Future Refactoring Opportunities

1. **Extract slot resolution logic** to shared static helper in `EquipmentManager` — both `DoEquip` and `GetCurrentlyEquippedForItem` could call it
2. **Generalize "AndSelect" pattern** — `ShowInventoryAndSelect`, `ShowEquipMenuAndSelect`, `ShowShopAndSelect` all follow same template; could extract common selection logic
3. **Add UNEXAMINE command** for symmetry with EQUIP/UNEQUIP (low priority — user can just cancel selection)

---

## Design Spec Compliance

All implementation decisions aligned with Coulson's design spec:
- ✅ Non-breaking: existing command behavior unchanged
- ✅ Reuse existing display methods: `ShowEquipmentComparison`, `ShowEquipMenuAndSelect`
- ✅ Interactive selection only when no argument (backward compatible)
- ✅ Slot resolution matches `EquipmentManager.DoEquip` logic
- ✅ Turn consumption rules respected (info-only commands never consume turn)

---

**Hill — C# Dev**  
TextGame Project
