### 2026-03-11: Combat smoke test added to CI

**By:** Fitz
**What:** Added scripted combat scenario to CI smoke test (`smoke-test.yml`). Drives the game through startup → new game → class/difficulty selection → game loop via piped stdin. Fails if output contains stack traces or unhandled exceptions.
**Why:** Retro P1 — `dotnet test` was green while the game crashed during actual gameplay (`System.InvalidOperationException` in rendering). Unit tests don't exercise the actual game execution path through combat. This smoke test catches that crash class.

**Also changed:** `Program.cs` — when stdin is not a TTY (`Console.IsInputRedirected`), uses `ConsoleDisplayService` instead of `SpectreLayoutDisplayService`. Spectre throws `NotSupportedException` on `SelectionPrompt` without a live terminal.
