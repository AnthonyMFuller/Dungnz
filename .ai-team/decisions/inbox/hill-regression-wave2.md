# Decision: Avalonia Smoke Test Checklist (Regression Wave 2)

**Author:** Hill
**Date:** 2026-03-21
**Status:** Proposed
**Issue:** #1419

## Context

Regression Wave 2 tasked Hill with creating a manual smoke test checklist for the Avalonia GUI (`Dungnz.Display.Avalonia/`). The P2 milestone delivered a functional 6-panel window but lacked any documented verification procedure.

## Decision

Created `docs/avalonia-smoke-test-checklist.md` with 12 scenarios covering:
- Application lifecycle (launch, quit, window close)
- All 6 panels (Map, Stats, Content, Gear, Log, Input)
- User interaction (typing, command submission, movement)
- Resilience (window resize)

Additionally audited `App.axaml.cs` and documented 4 hardcoded values (difficulty, seed, player name, player class) that are P2 scaffolding and must become configurable in P3–P8.

## Verification

- Build: ✅ 0 errors
- Headless launch: App starts and logs initialization, but interactive testing not possible in CI/headless
- Checklist is designed for local developer use on a desktop workstation

## Risks

- **Low:** Checklist may drift from implementation as panels evolve — should be updated alongside Avalonia display changes.
- **None:** This is documentation-only; no code changes to game logic.

## Action Items

- [ ] Coulson: Review and approve checklist coverage
- [ ] Team: Run checklist on desktop environment and fill in Pass/Fail column
- [ ] P3–P8: Remove hardcoded values in `App.axaml.cs` as startup flow is implemented
