# 2026-03-04: Post-TUI-migration strategic assessment

**Requested by:** Anthony (Boss)

---

## Session Overview

Coulson produced a comprehensive post-TUI-migration strategic assessment covering:

1. **What Terminal.Gui migration delivered** — Persistent split-screen layout, dual-thread architecture, full modal dialog coverage (19+), real map rendering with fog-of-war, live stats panel, rollback safety via feature flag, 1785 passing tests.

2. **6 TUI-specific gaps identified:**
   - ShowSkillTreeMenu stub (non-functional)
   - Color stripped (system mapped but not connected)
   - No dirty-flag rendering (GC/flicker risk at high update frequency)
   - TuiMenuDialog can't disable items (grayed-out items)
   - BuildColoredHpBar dead code (barChar computed but unused)
   - No terminal resize handling (minimum size not enforced)

3. **7 unresolved P1 game bugs (pre-TUI, still unfixed):**
   - Boss loot scaling broken (no isBossRoom params)
   - Enemy HP can go negative (no clamping)
   - Boss phase abilities skip DamageTaken tracking
   - SetBonusManager 2-piece bonuses never applied (discarded with `_`)
   - SoulHarvest dual implementation (inline + unused bus passive)
   - GameEventBus never wired (parallel event systems)
   - FinalFloor duplicated 4x (should be shared constant)
   - CombatEngine is 1,709-line god class (team consensus #1 debt)

4. **5 ranked next-step options:**
   - **Option 1 (RECOMMENDED):** Fix P1 gameplay bugs, stabilize TUI (~15-20h)
   - **Option 2:** CombatEngine decomposition (~25-30h, high-risk refactor)
   - **Option 3:** Polish TUI to feature parity (~10-15h, nice-to-have)
   - **Option 4:** Make TUI default, deprecate Spectre (~5-15h, depends on 1+3)
   - **Option 5:** New content wave (not recommended yet, broken foundation)

---

## Coulson's Recommendation

> The TUI migration was executed well. The architecture held — IDisplayService abstraction + dual-thread model + feature flag is exactly what we designed. Zero changes to game logic. Clean rollback path. All 19+ modal dialogs working.
> 
> But the game underneath has P1 bugs that make core systems non-functional (set bonuses, boss loot, damage tracking). These existed before the TUI work and they still exist now. The TUI makes the game look better while the engine has holes.
>
> **My recommendation: Option 1 first (gameplay correctness), then Option 3 (TUI polish), then consider Option 4 (make TUI default).** Fix the game, then make it pretty, then ship it as the standard experience.

---

## What This Means

1. **Immediate priority:** Fix the 5 critical gameplay bugs (boss loot, HP clamping, damage tracking, set bonuses, SoulHarvest) before adding more features or making TUI default.

2. **Secondary priority:** Polish TUI color system, skill tree menu, dirty-flag rendering.

3. **Strategic implication:** The TUI is feature-complete (all 19+ dialogs work). The bottleneck is core gameplay correctness, not UI.

4. **TUI rollback remains safe:** Feature flag, additive code, zero changes to game engine logic.
