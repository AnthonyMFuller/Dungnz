# Session: Option E Deep-Dive Feasibility Assessment

**Date:** 2026-03-05  
**Requested by:** Anthony

## Overview
Coulson, Hill, and Barton each performed a deep-dive feasibility assessment of **Option E**: Spectre.Console Live+Layout as a Terminal.Gui replacement.

## Verdicts

### Coulson: **NO-GO**
- **Finding:** Live+input incompatibility is architectural, not fixable.
- **Recommendation:** 
  - Option F (Mini-HUD status bar, 4-8hrs)
  - Option G (TUI polish, 8-12hrs)

### Hill: **Conditional YES**
- **Key Finding:** `ctx.Refresh()` is thread-safe — eliminates GameThreadBridge.
- **Recommendation:** 1-day PoC
- **Estimates:**
  - LOC required: 1,200–1,500 (vs 950 prior)
  - IDisplayService analysis: 54 methods total
    - 30 display-only
    - 24 input-coupled
- **Menu approach:** Exit Live → SelectionPrompt → resume

### Barton: **YES**
- **Game-feel assessment:** "Noticeably better than current TUI"
- **Killer feature:** Loot comparison (side-by-side panels at drop)
- **Urgency indicators:** HP/MP bars trivially easy in Spectre
- **Modal acceptability:** Menus pausing Live is acceptable for turn-based game

## Unresolved Question
**Is it acceptable if menus (shop, combat, inventory) go full-screen for 1–5 seconds, then panels resume?**

- **Coulson:** No
- **Barton:** Yes  
- **Hill:** PoC needed

---

*Logged by Scribe.*
