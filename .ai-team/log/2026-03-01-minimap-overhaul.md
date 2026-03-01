# Mini-Map Overhaul Session — 2026-03-01

**Requested by:** Anthony (Boss)

## Team

- **Coulson** (Lead) — Audit, planning, architecture
- **Hill** (C# Dev) — Implementation
- **Romanoff** (Tester) — Validation

## Work Summary

**Coulson:** Audited current mini-map, created 5 GitHub issues (#823-#827), wrote architectural decision to `.ai-team/decisions/inbox/coulson-minimap-plan.md`. Issues address interface change, fog-of-war, rich room symbols, dynamic legend, and visual polish.

**Hill (Phase 1, commit 84321ee):** Added fog-of-war (unvisited adjacent rooms show as grey `[?]`), 8 new room type symbols (`[M]` Merchant, `[T]` Trap, `[A]` Armory, `[L]` Library, `[F]` ForgottenShrine, `[*]` BlessedClearing, `[~]` Hazard, `[D]` Dark), updated `ShowMap()` signature to include `int floor = 1` parameter, floor number shown in panel header, updated `IDisplayService`, `DisplayService`, `GameLoop`, test helpers.

**Hill (Phase 2, commits 95c9e5f + 28b00bd):** Dynamic legend (only symbols present on current map shown), box-drawing connectors (`─` and `│` instead of `-` and `|`), 3-line compass rose (N / W ✦ E / S).

## Decisions

All 5 GitHub issues (#823-#827) created and closed in same session. Architectural decisions documented in merged decision files.
