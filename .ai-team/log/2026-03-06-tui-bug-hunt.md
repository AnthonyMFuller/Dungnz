# Session: 2026-03-06 TUI Bug Hunt

**Requested by:** Anthony  
**Date:** 2026-03-06

---

## What Happened

- **Romanoff conducted a 24-bug audit** of the Spectre.Console display layer
  - Full code review of SpectreLayoutDisplayService.cs, Program.cs, and integration points
  - Categorized findings: 4 Critical, 5 High, 6 Medium, 9 Low severity
  - Root causes: timing issues (Live startup), state cache staleness, missing refresh calls, dead code

- **Coulson conducted an architectural seam review** of display/game integration
  - Traced data flows between GameLoop, CombatEngine, CommandHandlers, and Display
  - Found 3 architectural gaps: missing post-combat refresh, no centralized refresh method, inconsistent cache updates
  - Identified 1 orphaned method (ShowFloorBanner never called) and made 8 critical/structural fix recommendations

---

## GitHub Issues Created

16 issues created across P0/P1/P2 severity:

### Issues #1075–#1089 (16 total)

**Display Layer Fixes (Hill):**
- #1075: INTRO SEQUENCE RENDERS NOTHING VISIBLE BEFORE LIVE STARTS
- #1076: FIRST ROOM NEVER SHOWN — LIVE STARTS WITH EMPTY PANELS
- #1077: STALE CACHED PLAYER/ROOM AFTER LOAD OR NEW GAME
- #1083: SHOWCOMMANDPROMPT NEVER UPDATES DURING GAMEPLAY
- #1085: CONTENT PANEL CLEARS ON COMBAT START — CONTEXT LOST
- #1086: LOG PANEL TRUNCATES AT 50 LINES BUT TAKESLAST LOGIC IS INCONSISTENT
- #1087: NESTED PAUSEANDRUN CALLS COULD DEADLOCK

**Game Logic Integration Fixes (Barton):**
- #1078: NO VISUAL FEEDBACK AFTER EQUIPPING ITEMS
- #1079: POST-COMBAT: STATS AND MAP NEVER REFRESH
- #1080: LEVEL-UP: STATS PANEL NOT REFRESHED
- #1081: HAZARD DAMAGE: HP DECREASES BUT STATS PANEL DOESN'T UPDATE
- #1082: DESCENDING TO NEW FLOOR: MAP NEVER RESETS (ShowFloorBanner never called)
- #1084: SHOWFLOORBAN NER ORPHANED METHOD NEVER CALLED

**Deferred (Future Sprint):**
- #1089: RefreshDisplay structural improvement (propose unified refresh method)

---

## Assignments

**Hill (Display-Layer Fixes):**
- #1075, #1076, #1077, #1083, #1085, #1086, #1087

**Barton (Game-Logic Integration Fixes):**
- #1078, #1079, #1080, #1081, #1082, #1084

**Future Sprint:**
- #1089 (Architectural refactor — centralized RefreshDisplay method and cache management)

---

## Key Findings Summary

### Critical Bugs (Game-Breaking)
1. Intro sequence renders nothing before Live starts
2. First room display has race condition (empty panels on startup)
3. Cached player/room state persists between game sessions
4. Command prompt HP/MP bar never updates during gameplay

### High Severity (Major UX Issues)
1. Equipment comparison shown but stats panel not updated
2. Combat start clears room context from content panel
3. Log panel truncation logic is inconsistent (50 lines shown, 100 buffer)
4. Nested pause/resume could deadlock (pause depth not tracked)
5. ShowIntroNarrative returns bool but return value unused (dead code)

### Medium Severity (Noticeable but Workaround Exists)
- HP/MP bar safety checks (divide by zero prevention)
- Map legend shows symbols not in grid
- Duplicate helper methods (TierColor, PrimaryStatLabel)
- Missing emoji support on Linux terminals
- And 2 more...

### Low Severity (Polish/Minor)
- ShowHelp hardcodes command list (should be data-driven)
- Content panel header changes lag during modal prompts
- Log timestamp format missing timezone
- Status effect icons incomplete for new effects
- And 5 more...

---

## Architectural Gaps Identified

1. **Missing post-combat display refresh** — Stats and map panels never update after combat ends; player sees stale HP/MP and enemy markers
2. **No centralized "refresh all panels" method** — Each game event manually orchestrates panel updates; developers forget to call all three (map, stats, content)
3. **Cached state is a leaky abstraction** — ShowRoom auto-refreshes stats/map via caches, but this only works for room transitions, not in-room state changes (combat, hazard, level-up)

---

## Recommendations

### Immediate Fixes (Block Player Experience)
1. Add fallback rendering for intro sequence when Live isn't active yet
2. Add sync barrier (ManualResetEventSlim) to ensure Live is ready before game loop starts
3. Add Reset() method to clear cached state between game sessions
4. Call ShowCommandPrompt after every HP/MP change

### Structural Improvements (Prevent Future Bugs)
1. Add RefreshDisplay(Player, Room, int floor) method to unconditionally update all three panels at turn boundaries
2. Call ShowFloorBanner in DescendCommandHandler (currently orphaned)
3. Document content panel policy (room description vs combat log after combat ends)
4. Consolidate duplicate helper methods (TierColor, PrimaryStatLabel)

### Tech Debt Cleanup
1. Remove RunPrompt (dead code, PauseAndRun used exclusively)
2. Remove compiled RegexOptions (used only 3 times)
3. Data-drive help text and icons (remove hardcoded switch statements)

---

## Threading Model Assessment

✅ **Sound for turn-based games** — Live on UI thread, game logic on separate thread, ctx.Refresh() is thread-safe.  
⚠️ **Nested pause/resume risk** — Pause depth not tracked; nested modal prompts could cause inconsistent event state.

---

## Next Steps

1. Hill: Prioritize #1075, #1076 (startup critical issues)
2. Barton: Review integration points for missing display refresh calls
3. Future sprint: Implement #1089 (RefreshDisplay architecture) after critical bugs are fixed
4. Code review: Enforce Show* method calls at all state change points

---

**Romanoff's detailed findings:** See `.ai-team/decisions/inbox/romanoff-tui-audit-bugs.md` (merged to decisions.md)  
**Coulson's architectural analysis:** See `.ai-team/decisions/inbox/coulson-tui-architecture-findings.md` (merged to decisions.md)
