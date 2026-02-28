# Hill Code Review — 2026-02-27

Reviewed: `Display/DisplayService.cs`, `Display/ConsoleMenuNavigator.cs`, `Engine/GameLoop.cs`, `Engine/CommandParser.cs`, `Engine/IntroSequence.cs`, `Program.cs`.

## Issues Filed

| # | Issue | Severity |
|---|-------|----------|
| #604 | `ShowLootDrop` namePad uses `icon.Length` not visual column width — box row misaligns for wide-emoji item icons | HIGH |
| #605 | `HandleUse` consumes a turn even when consumable has no recognized effect — player punished for unhandled item | HIGH |
| #606 | `HandleLoad` does not reset `RunStats` — pre-load stats (turns, kills, gold) bleed into the loaded run's history | HIGH |
| #607 | `SelectFromMenu` hides cursor but no `try/finally` — cursor stays hidden permanently if exception escapes the loop | MEDIUM |
| #608 | `ConsoleMenuNavigator.Select` never hides the cursor during arrow-key navigation (unlike `SelectFromMenu`) | MEDIUM |
| #609 | Both arrow-key menu renderers corrupt output when option count ≥ terminal height (cursor-up past top of window) | MEDIUM |
| #610 | `ShowPrestigeInfo` box misaligned — `⭐` (U+2B50) is 2 visual columns but counted as 1 by C# `.Length` | LOW |

## No Issues In
- `CommandParser.Parse` — clean, fully null-safe, falls back to `Unknown` on all unrecognised input.
- `Program.cs` — clean single-run setup; new `GameLoop` created per run so `_gameOver` staleness is not a live bug.
- `ShowMap` BFS — correct; only renders visited rooms; no null risks found.
- `HandleGo` / `HandleExamine` / `HandleTake` — all set `_turnConsumed = false` on every rejection path. ✓
- Color reset — every `Console.Write` with ANSI color in DisplayService terminates with `ColorCodes.Reset`. No color-leak paths found.
- `ConsoleMenuNavigator` cursor-up formula — `options.Count - 1` is correct given the no-trailing-newline-on-last-item render pattern. ✓
- 1-item menus — handled correctly (no cursor-up on first render; single `ReadKey` loop works). ✓
