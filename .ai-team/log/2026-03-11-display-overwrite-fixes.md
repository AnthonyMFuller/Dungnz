# Session: 2026-03-11 — Display Overwrite Bug Fixes

**Requested by:** Anthony  
**Team:** Coulson, Barton, Romanoff  

---

## What They Did

### Coulson — Display Overwrite Audit (#1313)

Audited all 26 command handlers in `Dungnz.Engine/Commands/`, two `GameLoop.cs` methods (`HandleShrine`, `HandleContestedArmory`), and `Dungnz.Systems/EquipmentManager.cs` for display overwrite bugs.

**Root cause confirmed:** `ShowError`/`ShowMessage` route through `AppendContent` (appends to `_contentLines`), while `ShowRoom` calls `SetContent` (clears `_contentLines` entirely). Any handler that called `ShowError` or `ShowMessage` followed unconditionally by `ShowRoom` silently wiped all messages before they could be read.

**Previously known issues:** #1311 (EquipCommandHandler — class restriction error wiped), #1312 (GoCommandHandler combat flow — enemy stats cleared), #1314 (CompareCommandHandler — ShowEquipmentComparison wiped by ShowRoom).

**New issues filed (#1315–#1321):**
- **#1315** — `UseCommandHandler`: 7 distinct error paths wiped by unconditional `ShowRoom` at end of `Handle()`
- **#1316** — `ExamineCommandHandler`: "Examine what?" error, enemy stats message, `ShowItemDetail`, and `ShowEquipmentComparison` all wiped
- **#1317** — `CraftCommandHandler`: "Unknown recipe" error and craft-failure error wiped
- **#1318** — `SkillsCommandHandler` / `LearnCommandHandler`: skill learn/fail/unknown messages wiped
- **#1319** — `GameLoop.HandleShrine()`: "no shrine here", "already used", and 4× gold-check errors wiped
- **#1320** — `GameLoop.HandleContestedArmory()`: "no armory here" and "already looted" wiped
- **#1321** — `GoCommandHandler` post-combat: two narrative messages (post-combat + "room cleared") wiped by `ShowRoom` after `CombatResult.Won`

**Totals:** 26 handlers + 2 GameLoop methods + EquipmentManager audited; 3 previously filed + 7 new = 10 total bugs.

**Recommended fix strategy:** Error paths should `return` without calling `ShowRoom`. Only success/completion paths transition back to room view — matching the pattern already correct in `GoCommandHandler` error paths, `AscendCommandHandler`, and others.

**Long-term option:** Route `ShowError` to a persistent status bar or message log never cleared by `SetContent`.

---

### Barton — Fix Implementation (PR #1322 + PR #1323)

**PR #1322** (merged) — Fixed previously known issues #1311, #1312, #1314:
- **#1311 / EquipmentManager**: `EquipmentManager.HandleEquip` refactored to return `(bool success, string? errorMessage)` tuple. Display calls removed from the Systems layer; `EquipCommandHandler` handles all display. Class restriction errors now surfaced correctly.
- **#1312 / GoCommandHandler combat flow**: Enemy stats now routed to the Stats sidebar panel via `RenderCombatStatsPanel` + cached enemy fields, eliminating the stats panel clear on room re-entry.
- **#1314 / CompareCommandHandler**: `ShowRoom` reordered to run first; `ShowEquipmentComparison` appended after so the comparison is not wiped.

**PR #1323** (under review) — Fixed new issues #1315–#1321:
- Applied the fix pattern — call `ShowRoom` first, then append error/result messages after — across `UseCommandHandler`, `ExamineCommandHandler`, `CraftCommandHandler`, `SkillsCommandHandler`, `LearnCommandHandler`, `GameLoop.HandleShrine()`, `GameLoop.HandleContestedArmory()`, and `GoCommandHandler` post-combat.

**Fix pattern established:**
```csharp
// BEFORE (broken): ShowError then ShowRoom wipes the error
context.Display.ShowError("You can't use that.");
context.Display.ShowRoom(context.CurrentRoom);
return;

// AFTER (correct): ShowRoom first, then append message
context.Display.ShowRoom(context.CurrentRoom);
context.Display.ShowError("You can't use that.");
return;
```

---

### Romanoff — PR #1322 Review and Merge

Reviewed and merged PR #1322. Issues #1311, #1312, and #1314 closed. All 1,898 tests passing after merge.

---

## Key Technical Decisions

**Display write order:** `ShowRoom` (SetContent) must precede any `ShowError`/`ShowMessage` (AppendContent) calls within a handler that needs to display both room context and a message. Reversing the order is the minimal, surgical fix.

**EquipmentManager return type:** `HandleEquip` now returns `(bool, string?)` tuple instead of calling display directly. Display responsibility belongs to the command handler layer, not the Systems layer. This prevents Systems code from having display side-effects.

**Enemy stats via sidebar:** Enemy combat stats route to the Stats sidebar panel (`RenderCombatStatsPanel` + cached enemy fields) rather than the Content panel, making them persistent and not subject to `SetContent` clearing.

---

## Related PRs

- PR #1322: Fix display overwrite bugs #1311, #1312, #1314 — **merged**
- PR #1323: Fix display overwrite bugs #1315–#1321 — **under review**
