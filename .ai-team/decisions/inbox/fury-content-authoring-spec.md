# Decision Record: Content Authoring Spec Created

**Date:** 2026-03-11  
**Agent:** Fury (Narrative Content Specialist)  
**Issue:** #1337  
**PR:** #1340  
**File:** `docs/content-authoring-spec.md`

## Context

Content authors on the TextGame project were operating without documentation on:
- Which game UI panels each content surface renders in
- Line limits per panel/surface
- Which characters would crash Spectre Console during rendering

This emerged from the retrospective: *"If I write a 6-line enemy lore block and it gets swallowed, I'm writing into a void."* Content was being written blind, with no way to self-validate before handing off to developers.

## Decision

Created `docs/content-authoring-spec.md` — a comprehensive 416-line authoring guide covering:

1. **Panel Layout Reference**
   - Visual diagram of 6-panel UI layout
   - Table mapping 18 content surfaces (room descriptions, enemy intros, merchant greetings, etc.) to specific panels
   - Panel dimensions and wrapping behavior

2. **Panel Dimensions & Hard Limits**
   - Content panel: ~70 chars wide × ~20 lines tall (wraps naturally)
   - Gear panel: ~25–30 chars wide × ~20 lines (enemy lore: ~8 lines max visible)
   - Stats panel: ~25–30 chars wide × ~8 lines (compact display)
   - Log panel: ~70 chars wide × ~8 lines (scrolling history)
   - Map panel: ~70 chars wide × ~5 lines (procedural, not authored)
   - Input panel: ~25–30 chars wide × ~4 lines (command only)

3. **Unsafe Characters & Escaping Rules**
   - `[ALL_CAPS]` and `[PascalCase]` inside brackets crash Spectre → must escape with `[[DOUBLE_BRACKETS]]`
   - Invalid color names (`[crimson]`, `[darkgreen]`) crash → use only valid colors
   - Safe colors: [red], [green], [blue], [yellow], [cyan], [magenta], [grey], [white], [bold], [dim], [italic], [underline]

4. **Content Self-Validation Checklist**
   - 9-point checklist authors complete before submission
   - Covers line limits, panel widths, bracket safety, tone consistency, grammar

5. **Examples & Integration Points**
   - Correct vs incorrect enemy entry example (showing bracket escaping, tone)
   - 9 common pitfalls with fixes
   - Integration reference table (where content is used in code)

## Why This Decision

- **Eliminates blind authoring:** Authors can now verify their work against documented constraints
- **Reduces rework:** Self-validation catches bracket crashes and line overflows before code review
- **Centralizes knowledge:** Single authoritative reference for all content surface locations and limits
- **Supports scaling:** As more content surfaces are added, template is already in place

## Consequences

- Content authors have a single "go-to" reference document (no more asking developers "will this fit?")
- Developers can link to specific sections when reviewing content PRs
- Future content surfaces should be documented in this spec before implementation
- The spec is a living document—should be updated when panel layouts or Spectre markup rules change

## Follow-up Actions

- PR #1340 to be merged into master
- Content authors should review and bookmark the spec
- Future content PRs should reference the spec's self-validation checklist in commit messages or descriptions
- Coulson to consider linking from README or CONTRIBUTING.md
