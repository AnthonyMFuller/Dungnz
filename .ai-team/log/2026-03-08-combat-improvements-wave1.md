# Session: Combat Improvements Wave 1
**Date:** 2026-03-08  
**Requested by:** Anthony

## Team Work

| Agent | Work Item | PR | Status |
|-------|-----------|-----|--------|
| Romanoff | Combat baseline tests (Issue #1273) | #1277 | ✅ Merged |
| Barton | Cooldown HUD visibility (Issue #1268) | #1276 | ✅ Merged |
| Fury | Enemy crit reactions (Issue #1269) | #1275 | ✅ Merged |

## Decisions Made

### Combat Improvement Plan (3-Phase Approach)
- **P0 Quick Wins:** Cooldown HUD + enemy crit reactions (2-3 sessions)
- **P1 Core:** Telegraph system + banter + phase-aware narration (5-6 sessions)
- **P2 Stretch:** Momentum resource system (3-4 sessions)

**Key finding:** Cooldowns work correctly but lack visibility. This PR fixes the display gap.

### Cooldown HUD Pattern (Barton)
Use default interface methods on `IDisplayService` for display-only features rather than abstract members. Only `SpectreLayoutDisplayService` overrides; test stubs inherit no-op. Reduces implementation burden from 5 files to 1.

### Narration Test Pattern (Romanoff)
- Assert narration via message counts + ordering, not string content
- Boss phases via `FiredPhases` HashSet, not display messages
- Ability tests call `UseAbility()` directly to isolate pipeline
- Pre-combat status effects via `StatusEffectManager` injection
- Use `player.MaxHP` sentinel for damage assertions, not hardcoded HP

### Enemy Crit Reactions (Fury)
All 31 enemy types now have personality-driven crit reactions. Static dictionary mirrors existing narration pattern. 93 total lines added covering all archetypes (Goblin, Skeleton, Dark Knight, Wraith, Dragon, etc.). Integrated via hook in CombatEngine.PerformEnemyTurn().

## Dependencies Established

**Blocker:** No combat PRs merge until Romanoff's baseline tests exist (PR #1277).

## Scope Summary

- Test baseline: 1-2 sessions
- P0 complete: 2-3 sessions
- Total P0+P1+tests: 8-11 sessions
- Content volume: ~184 lines (crit reactions, telegraphs, banter, phase narration)
