# Display Overwrite Audit — Findings

**Author:** Coulson  
**Date:** 2026-03-04  
**Tracking issue:** #1313  

---

## Scope

Full audit of:
- All 26 `.cs` files in `Dungnz.Engine/Commands/`
- `Dungnz.Engine/GameLoop.cs` (HandleShrine, HandleContestedArmory, HandleTrapRoom, HandlePetrifiedLibrary, HandleCommand)
- `Dungnz.Systems/EquipmentManager.cs`

---

## Display Architecture (confirmed)

| Method | Mechanism | Effect |
|--------|-----------|--------|
| `ShowError(msg)` | `AppendContent` | Adds to `_contentLines` |
| `ShowMessage(msg)` | `AppendContent` | Adds to `_contentLines` |
| `ShowRoom(room)` | `SetContent` | **Clears** `_contentLines`, replaces entirely |
| `ShowItemDetail(item)` | `SetContent` | **Clears** `_contentLines` |
| `ShowEquipmentComparison(...)` | `SetContent` | **Clears** `_contentLines` |
| `ShowCraftRecipe(...)` | `SetContent` | **Clears** `_contentLines` |
| `ShowPlayerStats(player)` | Updates stats side panel only | Does NOT touch content panel ✓ |

---

## Previously Confirmed Issues

| Issue | Handler | Bug |
|-------|---------|-----|
| #1311 | `EquipCommandHandler` + `EquipmentManager` | Class restriction error wiped |
| #1312 | `GoCommandHandler` combat flow | Enemy stats panel cleared on room re-entry |
| #1314 | `CompareCommandHandler` | ShowEquipmentComparison wiped by ShowRoom |

---

## New Issues Filed

### #1315 — UseCommandHandler
All error paths wiped by unconditional `ShowRoom` at the end of `Handle()`:
- L16-17: "You have no usable items"
- L54-55: "You don't have '{argument}'"
- L66-67: "Did you mean one of: …?"
- L177+191: "Use 'EQUIP X' to equip"
- L182+191: "Crafting material, cannot be used directly"
- L187+191: "You can't use X"
- L168-169+191: "Nothing happened" + "can't use right now"

### #1316 — ExamineCommandHandler
Multiple display calls wiped by ShowRoom:
- L10-11: "Examine what?" error
- L21-22: Enemy stats message
- L30-31: `ShowItemDetail` (room item) wiped
- L38-48: `ShowItemDetail` + `ShowEquipmentComparison` (inventory item) wiped
- L53-54: "You don't see any '{argument}' here" error

### #1317 — CraftCommandHandler
- Interactive path: `ShowCraftRecipe` (SetContent) + `ShowMessage`/`ShowError` result wiped by ShowRoom
- Direct-argument path: "Unknown recipe" error wiped; craft failure error wiped

### #1318 — SkillsCommandHandler / LearnCommandHandler
- "You learned X!" / "Cannot learn X right now." messages wiped by ShowRoom
- "Unknown skill: X" error wiped by ShowRoom

### #1319 — GameLoop.HandleShrine()
- L368-369: "There is no shrine here" — ShowError → ShowRoom
- L374-375: "The shrine has already been used" — ShowMessage → ShowRoom
- L402, 412, 423, 433: "Not enough gold (need Xg)" — 4× inline ShowError → ShowRoom → return

### #1320 — GameLoop.HandleContestedArmory()
- L519-520: "There is no armory here" — ShowError → ShowRoom
- L525-526: "The armory has already been looted" — ShowMessage → ShowRoom

### #1321 — GoCommandHandler post-combat
- Lines 163-167: Two ShowMessage calls (post-combat narrative + "room cleared") wiped by ShowRoom after `CombatResult.Won`

---

## Clean Handlers

No display overwrite bugs found in:
- `GoCommandHandler` error paths (return without ShowRoom — correct)
- `BackCommandHandler`
- `AscendCommandHandler` (error paths return without ShowRoom)
- `DescendCommandHandler` (floor transition messages before ShowRoom are cosmetic — acceptable)
- `ShopCommandHandler` (interactive loop; "no merchant" error returns without ShowRoom)
- `SellCommandHandler` (same)
- `TakeCommandHandler` single-item path (inventory-full error returns before ShowRoom)
- `SaveCommandHandler`, `LoadCommandHandler`
- `ListSavesCommandHandler`, `LeaderboardCommandHandler`, `PrestigeCommandHandler`, `StatsCommandHandler`
- `LookCommandHandler`, `MapCommandHandler`, `HelpCommandHandler`
- Infrastructure files: `ICommandHandler`, `CommandHandlerBase`, `CommandContext`

---

## Totals

- **Handlers audited:** 26 command handlers + 2 GameLoop methods + EquipmentManager
- **Previously filed:** 3 issues (#1311, #1312, #1314)
- **New issues filed:** 7 issues (#1315–#1321)
- **Handlers with confirmed bugs:** 8 command handlers + 2 GameLoop methods

---

## Recommended Fix Strategy

**Per-handler surgical fix (Option 2 — recommended):**  
Error paths should `return` without calling `ShowRoom`. Only success/completion paths should transition back to room view. This matches the pattern already used correctly in `GoCommandHandler`, `AscendCommandHandler`, and others where error paths skip ShowRoom.

Example:
```csharp
// BEFORE (broken):
context.Display.ShowError("You can't use that.");
context.Display.ShowRoom(context.CurrentRoom);
return;

// AFTER (correct):
context.Display.ShowError("You can't use that.");
return;  // player sees the error; room stays in view from the last ShowRoom call
```

**Architectural fix (Option 3 — long-term):**  
Route `ShowError` to a persistent status bar or message log that is never cleared by `SetContent`. This would eliminate the entire class of bugs without requiring per-handler fixes.
