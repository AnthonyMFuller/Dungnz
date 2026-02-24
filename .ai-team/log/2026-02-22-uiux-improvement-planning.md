# Session: 2026-02-22 UI/UX Improvement Planning

**Requested by:** Anthony

**Facilitated by:** Coulson

**Participants:** Hill, Barton, Romanoff

## Outcome
Plan written to `.ai-team/plans/uiux-improvement-plan.md`

## Structure
- **Phase 1:** Combat Feel — visual feedback and status clarity
- **Phase 2:** Navigation & Exploration Polish — exploration UX and wayfinding
- **Phase 3:** Information Architecture — system visibility and player mental models

## Shared Infrastructure Identified
- `RenderBar()` utility for stat/ability bar rendering
- `ColorCodes.StripAnsiCodes()` for ANSI-safe padding (fixes existing `ShowLootDrop` bug)
- 6 new IDisplayService methods:
  - Extended `ShowCombatStatus` with active effect lists
  - Extended `ShowCommandPrompt` with Player parameter for persistent status mini-bar
  - `ShowVictory` and `ShowGameOver` moved from GameLoop to IDisplayService
  - 2 additional display methods for phase-specific content

## Status
Plan presented to Anthony for approval — implementation NOT started
