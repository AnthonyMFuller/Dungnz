# Session: 2026-03-11 — Retro Action Item PRs Merged

**Requested by:** Anthony  
**Team:** Romanoff, Coulson  

---

## What They Did

### Romanoff — PR #1341 Merge (FinalFloor → GameConstants)

Reviewed and merged PR #1341, which closed Issue #1330.  
Hill had created `Dungnz.Models/GameConstants.cs` and migrated `FinalFloor` (and related game-wide constants) out of engine-local definitions. Romanoff verified the change and merged without issue.

Commit: `refactor(engine): centralize FinalFloor into GameConstants.cs (#1330)`

### Romanoff — Rebase + PR #1343 Merge (Smoke Test + TTY Fix)

Branch `squad/1338` contained Hill's `FinalFloor` contamination (references to the old definition location) that would have conflicted after #1341 landed. Romanoff rebased `squad/1338` onto the updated master to remove the contamination before merging.

After rebase, merged PR #1343, which closed Issues #1331, #1332, and #1338.  
This PR delivered:
- Extended CI smoke test (`smoke-test.yml`) with scripted combat scenario — pipes stdin through startup → class/difficulty selection → game loop; fails on stack traces or unhandled exceptions.
- TTY guard in `Program.cs`: when `Console.IsInputRedirected` is true (no live terminal), routes to `ConsoleDisplayService` instead of `SpectreLayoutDisplayService`, preventing `NotSupportedException` from Spectre on `SelectionPrompt`.

Commit: `ci: extend smoke test with scripted combat scenario (#1338)`

### Coulson — PR #1342 Merge (Adversarial Markup Tests)

Reviewed and merged PR #1342, which introduced adversarial markup and panel line-count assertion tests.

During review Coulson caught one defect: a magic number `8` used as the expected `StatsPanelHeight` bound. Corrected to `LayoutConstants.StatsPanelHeight` before merging to ensure the test stays coupled to the constant and doesn't drift. Also removed a duplicate comment.

Commit: `test(display): adversarial markup + panel line count assertions (#1342)`

---

## Key Technical Decisions

- **GameConstants.cs as source of truth** — `Dungnz.Models/GameConstants.cs` is now the single home for game-wide constants (`FinalFloor` etc.). Placed in `Dungnz.Models` (not `Dungnz.Engine`) to avoid circular dependency with `Dungnz.Systems`.
- **Smoke test covers real gameplay path** — `dotnet test` was green while the game crashed during actual rendering. Smoke test catches that crash class by exercising the actual execution path.
- **TTY detection gates Spectre** — `ConsoleDisplayService` is the fallback when stdin is redirected, preventing Spectre's `SelectionPrompt` from throwing `NotSupportedException` in CI/non-TTY contexts.
- **No magic numbers in tests** — Assertion bounds in display tests must reference `LayoutConstants` constants, not raw integers.

---

## Issues Closed

- #1330 — FinalFloor → GameConstants.cs
- #1331 — TTY fix / ConsoleDisplayService fallback
- #1332 — Smoke test scripted combat
- #1338 — CI smoke test extension

---

## Test Results

1909 passing, 4 skipped, 0 failed.

---

## Still In Flight

- **PR #1340** — Fury's content authoring spec (`docs/content-authoring-spec.md`); Romanoff reviewing.
- **Issue #1333** — Barton implementing (PR not yet opened).

---

## Related PRs

- PR #1341: `refactor(engine): centralize FinalFloor into GameConstants.cs` — merged
- PR #1342: `test(display): adversarial markup + panel line count assertions` — merged
- PR #1343: `ci: extend smoke test with scripted combat scenario` — merged
