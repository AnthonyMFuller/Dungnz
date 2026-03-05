# UI Options Review Session — 2026-03-05

**Requested by:** Anthony

**Agents who worked:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev)

## What They Did
Strategic UI options review — researched whether to improve Terminal.Gui TUI or replace it. Coulson assessed 5 options architecturally. Hill surveyed .NET library landscape. Barton defined UX requirements from a game-feel perspective.

## Key Findings
- Terminal.Gui v1.x has a fundamental per-line color limitation
- No good .NET TUI replacements exist today
- Coulson/Hill recommend **Option D** (make Spectre.Console the default, demote TUI to experimental)
- Barton prefers incremental TUI fixes (1-2 days) given the persistent panels have genuine UX value

## Status
No implementation started — this was research only, pending Anthony's decision.
