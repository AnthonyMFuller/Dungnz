# Decision: Combat Item Usage Design

**Date:** 2025-07-24  
**Author:** Coulson  
**Status:** Approved  
**Issues:** #647, #648, #649

## Context
Anthony requested "Use Items during combat" — a new combat action letting players consume items (potions) during their turn.

## Decision

### Interface Addition
Add one new method to `IDisplayService`:
```csharp
Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables);
```
Returns the selected item or `null` for cancel. Follows the exact same pattern as `ShowAbilityMenuAndSelect`.

### Combat Menu Change
`ShowCombatMenuAndSelect` gains a 4th option: `"🧪 Use Item" → "I"`. When the player has no consumables, this option is rendered as a grayed-out info line (not selectable), matching the unavailable-ability pattern.

### Engine Wiring
New private method `HandleItemMenu(Player, Enemy)` in `CombatEngine`, following `HandleAbilityMenu` structure:
1. Filter `player.Inventory` for `ItemType.Consumable`
2. Call `_display.ShowCombatItemMenuAndSelect(consumables)`
3. On cancel → return `AbilityMenuResult.Cancel` → **does not consume turn** (re-show menu)
4. On selection → call `_inventory.UseItem(player, selectedItem.Name)` → enemy takes turn

### Key Design Choices

| Choice | Decision | Rationale |
|--------|----------|-----------|
| Cancel behavior | Does NOT consume turn | Opening a bag is free; differs from ability cancel (#611) which does consume turn |
| No consumables | Grayed info line, not selectable | Prevents dead-end menu, clean UX |
| HP/mana overflow | No special handling | `Heal()` and `RestoreMana()` already clamp at max; item is still consumed (consistent with out-of-combat) |
| InventoryManager changes | None | `UseItem` already handles consumables correctly |
| New enum values | None | Reuses `AbilityMenuResult.Used/Cancel` for item menu result |

### Files Affected
- `Display/IDisplayService.cs` — new method signature
- `Display/DisplayService.cs` — implement method + modify `ShowCombatMenuAndSelect`
- `Engine/CombatEngine.cs` — `HandleItemMenu` + turn handler wiring
- `Dungnz.Tests/Helpers/FakeDisplayService.cs` — stub
- `Dungnz.Tests/Helpers/TestDisplayService.cs` — stub
- `Dungnz.Tests/CombatItemTests.cs` — new test file

### Work Decomposition
1. **#647** — Display layer (Hill): interface + implementation + test stubs
2. **#648** — Engine wiring (Barton): HandleItemMenu + turn handler, depends on #647
3. **#649** — Tests (Romanoff): 10 test cases covering happy path + edge cases, depends on #647 + #648

## Alternatives Considered
- **Separate ItemMenuResult enum** — Rejected: `AbilityMenuResult` already captures Used/Cancel semantics, no need for a new type
- **Item cancel consumes turn** — Rejected: inconsistent with player expectation (peeking in bag is free)
- **Skip grayed option, just don't show it** — Rejected: player should see the option exists even when empty, for discoverability
