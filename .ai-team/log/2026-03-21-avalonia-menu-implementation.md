# Session: 2026-03-21 — Avalonia Menu Implementation

**Requested by:** Anthony (Boss)  
**Team:** Hill  

---

## What They Did

### Hill — Menu Infrastructure & Input Method Implementation

Discovered that Avalonia UI had all 37 output methods working correctly but 23 of 26 input methods were hardcoded stubs returning fake values. Created 6 GitHub issues (#1428-#1433) addressing:
- Menu infrastructure core
- Combat menus
- Inventory menus  
- Economy menus
- Progression & special room menus
- Startup flow integration

Implemented all 23 input methods using a text-based numbered menu pattern:
- Menus render as numbered options in ContentPanel
- User input collected via InputPanel using TaskCompletionSource bridge
- Reused proven ReadCommandInput pattern for consistency
- SelectFromMenu<T> serves as generic helper method
- No changes required to AXAML files

Also replaced hardcoded App.axaml.cs startup logic with real StartupOrchestrator flow.

**Result:** PR #1434 merged to master. All 6 issues auto-closed. 2,351 tests passing. +651 lines changed across 2 files.

---

## Key Technical Decisions

### Text-Based Numbered Menu Pattern for Avalonia Input

**Date:** 2026-03-21  
**Architect/Author:** Hill  
**Issues:** #1428, #1429, #1430, #1431, #1432, #1433  
**PRs:** #1434  

---

## Context
All Avalonia display methods worked correctly, but 23 of 26 input methods were unimplemented stubs. Required a reliable, consistent pattern for menu-driven input that could integrate seamlessly with the existing ContentPanel/InputPanel architecture.

Alternative approaches considered:
- Arrow-key navigation with custom controls (complex, risky, required AXAML modifications)
- Traditional button-based UI (violated text-game design, required heavy AXAML work)
- Modal dialog system (incompatible with existing TCS bridge pattern)

## Decision
Adopted text-based numbered menu system:
1. Menu options rendered as numbered list in ContentPanel (e.g., "1. Attack\n2. Defend\n3. Cast Spell")
2. User enters number via InputPanel
3. Selection validated and returned via SelectFromMenu<T> helper
4. TaskCompletionSource bridge used to connect InputPanel completion to method return
5. Reused ReadCommandInput pattern throughout for consistency

## Rationale
- **Simplicity:** No custom controls or AXAML changes required
- **Reliability:** Pattern proven in existing ReadCommandInput implementation
- **Consistency:** All menu methods use identical architecture and flow
- **Compatibility:** Works seamlessly with existing ContentPanel/InputPanel infrastructure
- **Maintainability:** Generic SelectFromMenu<T> reduces code duplication

## Alternatives Considered
1. **Arrow-key navigation:** Would require custom controls and AXAML modifications; higher complexity and risk
2. **Button-based UI:** Violated text-game design philosophy; required extensive UI layer changes
3. **Modal dialogs:** Incompatible with TaskCompletionSource bridge pattern used for input

## Related Files
- Dungnz.Display.Avalonia/Input/* (all 23 menu input methods)
- Dungnz.Display.Avalonia/SelectFromMenu<T> (generic helper)
- Dungnz.Display.Avalonia/App.axaml.cs (startup flow integration)
- Dungnz.Models/Startup/StartupOrchestrator.cs

---

## Related PRs

- PR #1434: Implement all 23 Avalonia menu input methods using text-based numbered pattern
