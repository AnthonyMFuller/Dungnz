### 2026-03-11: PR #1344 — PanelHeightRegressionTests approved and merged

**By:** Romanoff  
**What:** Reviewed and merged `squad/1333-panel-height-regression-tests` — the final retro action item (#1333).

**Verified:**

- 4 tests in `Dungnz.Tests/Display/PanelHeightRegressionTests.cs`, all passing (targeted run: 4/4; full suite: 1913/1913)
- Tests call `SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup` directly via `InternalsVisibleTo` seam — no live terminal required
- All height assertions reference `LayoutConstants.StatsPanelHeight` — zero magic numbers
- Empty cooldowns (`Array.Empty<(string, int)>()`) used throughout — matches the team decision that cooldown overflow is a separate layout concern
- `[Collection("console-output")]` present — parallel interference prevented
- Deferred GearPanel test is a `// TODO:` comment only — no failing or skipped test
- The TODO comment specifies exactly what Hill must do to unblock it: extract `BuildGearPanelMarkup(Player player)` as `internal static` in `SpectreLayoutDisplayService.cs`
- Issue #1333 auto-closed on merge

**Why:** Final retro action item (#1333) — all 9 retro items are now complete.

**Outstanding follow-up (not blocking merge):**  
Hill to extract `BuildGearPanelMarkup` seam → Romanoff/Barton to write `GearPanelLineCount_IsWithinGearPanelHeight` test in a follow-up PR.
