### 2026-03-01: TAKE command interactive menu design
**By:** Coulson
**What:** Plan for TAKE command enhancement — arrow-key menu + Take All + fuzzy match
**Why:** Anthony requested USE/EQUIP parity for TAKE command

---

#### Issue Breakdown

| Issue | Title | Labels | Depends On |
|-------|-------|--------|------------|
| #697 | Add ShowTakeMenuAndSelect to IDisplayService and DisplayService | enhancement | — |
| #698 | Update HandleTake to show interactive menu when no argument given (with Take All support) | enhancement | #697 |
| #699 | Tests for TAKE command interactive menu and take-all behavior | testing | #697, #698 |

**Execution order:** #697 → #698 → #699 (strict dependency chain)

#### Key Design Decisions

1. **Take All sentinel value approach:** `ShowTakeMenuAndSelect` returns a special `Item` with `Name = "__TAKE_ALL__"` when the player selects Take All. This avoids introducing a new return type or wrapper enum — the caller checks `selected.Name == "__TAKE_ALL__"` to trigger the take-all loop. The sentinel is never added to inventory; it's consumed purely as a signal.

2. **Levenshtein tolerance:** `Math.Max(3, inputLength / 2)` — same formula as `EquipmentManager.HandleEquip`. Reuses `EquipmentManager.LevenshteinDistance` directly (it's `internal static`). Consistent UX across TAKE, EQUIP, and future commands.

3. **Take All inventory-full handling:** Loop through a snapshot copy of room items. On inventory full, stop immediately — items already taken stay in inventory, remaining items stay in room. Show count of items taken vs total. Turn is consumed if at least one item was taken.

4. **Empty room guard:** If `_currentRoom.Items` is empty and no argument given, show error `"There's nothing here to take."` instead of an empty menu. Matches the pattern where EQUIP shows "no equippable items" and USE shows "no usable items".

5. **Menu structure:** Room items (with icons + stats) → "📦 Take All" → "↩  Cancel". Uses `SelectFromMenu<Item?>` helper with header `"=== TAKE — Pick up which item? ==="`.

#### Files to Modify

| File | Change |
|------|--------|
| `Display/IDisplayService.cs` | Add `Item? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)` |
| `Display/DisplayService.cs` | Implement menu (follows ShowEquipMenuAndSelect pattern) |
| `Engine/GameLoop.cs` | Rewrite `HandleTake()` — menu path, take-all loop, fuzzy match |
| `Dungnz.Tests/Helpers/FakeDisplayService.cs` | Add testable stub |
| `Dungnz.Tests/Helpers/TestDisplayService.cs` | Add null-returning stub |
| `Dungnz.Tests/TakeCommandTests.cs` | New file — 12 test cases |

#### Assignment Recommendation
- #697 → Hill (Display layer, follows his ShowEquipMenuAndSelect work)
- #698 → Barton (GameLoop systems integration, fuzzy match wiring)
- #699 → Romanoff (Test coverage)
