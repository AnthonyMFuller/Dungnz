# Decision: Extract BuildGearPanelMarkup as Internal Static (Issues #1349, #1350)

**Date:** 2026-03-11  
**Decided by:** Barton (Display Specialist)  
**Context:** Phase 3 retro identified GearPanel testability gap and StatsPanel cooldown overflow as highest-leverage unblocking actions  
**Status:** ✅ Implemented (PR #1364)

---

## Problem

1. **Zero test surface for GearPanel rendering** — `RenderGearPanel` is private instance method with 10 gear slots + set bonus logic. Most complex panel in the UI. Any gear display bugs require full integration testing. Hill and Romanoff identified this as blocking test coverage expansion.

2. **StatsPanel cooldown overflow** — `BuildPlayerStatsPanelMarkup` generates 9 lines when cooldowns are present, but `StatsPanelHeight = 8`. This causes layout overflow. Regression tests were forced to exclude cooldown paths to avoid false CI failures.

---

## Decision

### Extract `BuildGearPanelMarkup(Player player)` as `internal static`

**Rationale:**
- Mirrors the existing `BuildPlayerStatsPanelMarkup` pattern (already internal static and tested)
- Provides unit test surface for Romanoff to write gear panel regression tests
- No behavior change — pure extraction
- `RenderGearPanel` delegates to it (60 lines → 6 lines)

**Implementation:**
- Method signature: `internal static string BuildGearPanelMarkup(Player player)`
- Contains: `AddSlot` local function, all 10 slot calls, set bonus conditional, returns `sb.ToString().TrimEnd()`
- `TierColor(item.Tier)` remains accessible (private static in same partial class)
- `SetBonusManager.GetActiveBonusDescription(player)` called for set bonus display

### Fix StatsPanel cooldown overflow by increasing panel height

**Root cause:** Cooldown line (line 432) was added for combat HUD (Issue #1268) but panel height constant wasn't updated.

**Solution:** `StatsPanelHeight = 8` → `StatsPanelHeight = 9`

**Line count breakdown:**
1. Player name line
2. Blank line
3. HP bar
4. MP bar (if MaxMana > 0)
5. **Cooldown line (if cooldowns.Count > 0)** ← This is line 9
6. Blank line
7. ATK/DEF line
8. Gold line
9. XP line

With cooldowns: 9 lines. Panel height was 8. Now 9.

**Layout constant rationale:**
- BaselineTerminalHeight = 40 rows
- TopRow = 20% = 8 rows baseline
- +1 for cooldown line = 9 rows
- XML comment updated: "TopRow = 20% of 40 = 8 rows baseline; +1 for cooldown line"

---

## Consequences

### Positive
- GearPanel now has unit test surface (can test empty slots, set bonus rendering, accessory vs armor stat display)
- StatsPanel no longer overflows when cooldowns are present
- Regression tests can now include cooldown paths without false failures
- Layout constants enforced by `PanelHeightRegressionTests`

### Neutral
- `StatsPanelHeight = 9` is slightly above the 20% baseline (8 rows), but within Spectre Layout's flex tolerance
- Panel renders correctly at standard terminal sizes (40+ rows)

### Risks
- None identified. All tests pass. No layout regressions observed.

---

## Alternatives Considered

### For gear panel extraction:
1. **Keep as private** — Rejected. Zero testability. Defers the problem.
2. **Extract to separate class** — Rejected. Overkill. `internal static` is sufficient and mirrors existing pattern.

### For cooldown overflow:
1. **Cap rendering** — Rejected. Would hide cooldown info from players.
2. **Split cooldowns to separate panel** — Rejected. Increases layout complexity.
3. **Remove blank lines** — Rejected. Degrades visual hierarchy.
4. **Increase panel height by 1** — **CHOSEN**. Simplest fix, within baseline tolerance.

---

## Implementation Notes

- All changes in PR #1364: `squad/1349-gear-panel-extraction`
- Files modified:
  - `Dungnz.Display/Spectre/LayoutConstants.cs` (StatsPanelHeight: 8 → 9)
  - `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` (extracted BuildGearPanelMarkup, thinned RenderGearPanel)
  - `Dungnz.Tests/Display/PanelHeightRegressionTests.cs` (assertion updated to 9)
- Build: ✅ clean
- Tests: ✅ 1913/1913 passed

---

## Future Work

- Romanoff can now write unit tests for `BuildGearPanelMarkup` (see `PanelHeightRegressionTests.cs` TODO)
- If cooldown line becomes dynamic (multiple lines for many cooldowns), revisit panel height logic
