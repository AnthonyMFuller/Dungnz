# Session Log: Terminal.Gui Migration

**Date:** 2026-03-04  
**Requested by:** Anthony (Boss)

## Summary

Coulson architected the Terminal.Gui migration plan with 14 issues created (#1015–#1028). Implementation begun:

- **Fitz** implemented #1016 (NuGet + --tui flag), PR #1029 merged
- **Hill** implemented #1017–#1021 (TUI core: layout, bridge, display service, menus), PR #1030 merged
- **Phase 2+3 agents launched:**
  - Hill: #1026
  - Barton: #1022–#1025
  - Romanoff: #1027
  - Fitz: #1028

## Architecture

- **Dual-thread model:** Terminal.Gui on main thread, game logic on background thread
- **Feature flag:** `--tui` command-line argument (default: Spectre.Console)
- **Additive design:** All code in `Display/Tui/` — no changes to existing display layer
- **Rollback strategy:** Delete `Display/Tui/`, remove NuGet ref, revert `Program.cs` modifications

## Status

Implementation underway. Phase 1 complete (foundation + core infrastructure). Phases 2–3 in progress (panels, dialogs, integration).
