# Fitz — History (Recent Activity)

**Full archive:** `history-archive-2026-03-11.md`

---

## Compressed Index — 2026-02-XX through 2026-03-09

- 2026-02-24: GitHub Actions optimization round 1 — ~40% reduction (heartbeat, CI consolidation, readme→pre-push hook)
- 2026-02-24: GitHub Actions optimization round 2 — removed build/test from release, deleted squad-preview.yml, heartbeat→dispatch-only
- 2026-02-XX: UI consistency PR cleanup — PR #602 rebase + admin merge; repo hygiene pass
- 2026-03-03: `squad-release.yml` tag versioning with Git SHA (#874, PR #885)
- 2026-03-XX: DevOps improvements — CI speed, Dependabot, EditorConfig, release artifacts, Stryker manifest, CodeQL (#759, #761, #763, #765, #767, #769)
- 2026-03-XX: CI improvements — osx-x64 publish, Stryker threshold 50→65, coverage floor confirmed 80% (#876, #877, #878)
- 2026-03-XX: Coverage artifact upload + PR summary comment (PR #898, issue #883)

---

## 2026-03-11 — Combat Smoke Test Added to CI (Issue #1338)

**Branch:** `squad/1338-smoke-test-combat-scenario`  
**Files:**
- `.github/workflows/smoke-test.yml` — scripted combat scenario via piped stdin
- `Program.cs` — non-TTY mode: uses `ConsoleDisplayService` when `Console.IsInputRedirected`

Retro P1 action item. `dotnet test` was green while game crashed on actual play
(`System.InvalidOperationException` in Spectre rendering). Smoke test drives: startup →
new game → class/difficulty → game loop, failing on any stack trace or unhandled exception.

**Key insight:** `Program.cs` previously hardcoded `SpectreLayoutDisplayService`, which throws
`NotSupportedException` without a live terminal. Now checks `inputReader.IsInteractive`; when
false (piped stdin), routes to `ConsoleDisplayService` instead.

Verified locally: game runs clean with piped input, reaches combat, exits "Thanks for playing!"
Build: ✅ 0 errors.  
(see decisions.md: "Combat Smoke Test Added to CI")
