# Session: 2026-03-11 — Inbox Merge: Barton #1333/#1336 Decisions

**Requested by:** anthony  
**Team:** Barton, Scribe  

---

## What They Did

### Barton — Spectre Markup Bracket Sweep (#1336)

Executed a full grep audit of all `[WORD]` bracket patterns in `Dungnz.Display/`, `Dungnz.Engine/`, and `Dungnz.Systems/`. Result: **CLEAN — no code changes required.** All dangerous patterns were already properly escaped. Key findings:

- `SpectreLayoutDisplayService.cs` L449, L507: `[[CHARGED]]` — already double-bracket escaped ✅  
- `SpectreDisplayService.cs` map legend entries (`[[B]]`, `[[E]]`, etc.) — already escaped ✅  
- `CombatEngine.cs` combat menu options (`[A]ttack`, `[B]ability`, `[F]lee`) — routed through `ShowMessage`/`ShowError` which call `Markup.Escape` ✅  
- `AttackResolver.cs` ability names (`[Focus]`, `[Fury]`, `[Shadowstep]`) — via `ShowColoredCombatMessage` + `Markup.Escape` ✅  
- `StatusEffectManager.cs`, `AbilityManager.cs` — via `ShowCombatMessage` + `ConvertAnsiInlineToSpectre` ✅  

The crash-proof architecture established in prior PRs covers all paths: every game-state string passes through `Markup.Escape` before Spectre renders it.

Decision recorded: **Decision 8** in `decisions.md`.

### Barton — Panel Height Regression Tests (#1333, PR #1344)

Wrote panel height regression tests for `BuildPlayerStatsPanelMarkup`. Two scoping decisions made:

1. **Cooldown row excluded from regression tests.** Active cooldowns push the panel to 10 lines, exceeding `StatsPanelHeight = 8`. Tests pass `Array.Empty<(string, int)>()` to isolate the regression being guarded (enemy stats appearing in the wrong panel). Cooldown overflow is a known constraint deferred to a future PR.

2. **GearPanel seam deferred to Hill.** `RenderGearPanel` is `private` — cannot be tested without extraction. A `// TODO:` comment marks the deferred test. Hill to extract `BuildGearPanelMarkup(Player player)` as `internal static` in `SpectreLayoutDisplayService.cs`; Romanoff/Barton to close the TODO once that extraction lands.

Decisions recorded: **Decision 9** and **Decision 10** in `decisions.md`.

### Scribe — Inbox Merge

- Merged `barton-markup-escape-complete.md` → `decisions.md` (Decision 8)  
- Merged `barton-panel-height-tests.md` → `decisions.md` (Decisions 9 and 10)  
- Deleted both inbox files  

---

## Key Technical Decisions

- **All Spectre markup paths are crash-proof** — the sweep found zero unescaped bracket patterns. The `Markup.Escape` + `ConvertAnsiInlineToSpectre` architecture established in earlier PRs is sufficient.
- **Cooldown overflow is a separate concern** — regression tests intentionally omit cooldown rows; a future decision will address `StatsPanelHeight` vs. compressed display.
- **GearPanel requires Hill's involvement** — extracting `BuildGearPanelMarkup` as `internal static` is a display-plumbing change that belongs in a targeted Hill PR, not a test-only PR.

---

## Related PRs

- PR #1344: Panel height regression tests + GearPanel TODO  
- Closes #1336: Markup bracket sweep (clean — no code changes)  
- Closes #1333: Panel height regression coverage  
