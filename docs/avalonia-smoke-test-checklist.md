# Avalonia GUI — Manual Smoke Test Checklist

> **Purpose:** Verify the Avalonia GUI (`Dungnz.Display.Avalonia`) works correctly on a developer workstation.
> This checklist is for **local manual testing** — the Avalonia app requires a desktop environment with a display server (X11 or Wayland) and cannot be verified in headless CI.

## Prerequisites

- .NET 10 SDK installed
- Desktop environment with display server (X11/Wayland)
- Repository cloned and dependencies restored (`dotnet restore`)

## Launch Command

```bash
dotnet run --project Dungnz.Display.Avalonia
```

## Test Scenarios

| # | Scenario | Steps | Expected Result | Pass? |
|---|----------|-------|-----------------|-------|
| 1 | Application launch | Run `dotnet run --project Dungnz.Display.Avalonia` | Window opens with title "Dungnz — Dungeon Crawler", 1200×800 default size, dark background (#1a1a1a), 6 panels visible | ☐ |
| 2 | Map panel renders | Observe top-left panel after launch | ASCII dungeon map with room grid and connectors; default text "Map will appear here" replaced after game loop starts | ☐ |
| 3 | Stats panel shows player info | Observe top-right panel after launch | Shows "Adventurer" (Warrior), HP bar 100/100 (green), Level 1, ATK/DEF stats, Gold 0 | ☐ |
| 4 | Content panel shows room description | Observe middle-left panel after launch | Non-empty text describing the starting room; scrollable if content exceeds panel height | ☐ |
| 5 | Gear panel shows equipment slots | Observe middle-right panel after launch | Equipment slot list with emoji icons (⚔ Weapon, 🦺 Chest, etc.); initially all "empty" or "—" | ☐ |
| 6 | Log panel accumulates messages | Observe bottom-left panel after launch | At least 2–3 timestamped log entries (e.g., room entry, startup info); format `HH:mm ℹ message` | ☐ |
| 7 | Input panel accepts typing | Click the input TextBox (bottom-right), type any text | Typed text appears in the input box; placeholder "Enter command..." disappears; prompt shows `> ` | ☐ |
| 8 | Command submission | Type any valid command (e.g., "look") and press Enter | Command is submitted, input box clears, game responds in Content/Log panels | ☐ |
| 9 | Movement command | Type `n` + Enter (or `s`, `e`, `w` if north unavailable) | Player moves to adjacent room; Map panel updates; Content panel shows new room description | ☐ |
| 10 | Quit command | Type `quit` + Enter | Window closes cleanly; process exits with code 0 | ☐ |
| 11 | Window close via X button | Click the window's X (close) button | Process exits cleanly with no hang or orphaned process | ☐ |
| 12 | Window resize | Drag window edges to resize (smaller and larger) | Panels reflow proportionally; no crash, no rendering artifacts, no truncated text | ☐ |

## Known Limitations

- **Headless / CI environments:** The app exits with code 124 (timeout) or fails to open a window when no display server is available. This is expected — Avalonia requires a graphical environment.
- **Input panel starts disabled:** The input TextBox is disabled until the game loop reaches its first `ReadLine()` call. There is a brief delay (~1–2 seconds) after window open before input becomes active.
- **`ReadKey()` not implemented:** `AvaloniaInputReader.ReadKey()` returns `null`; the game falls back to numbered text prompts instead of arrow-key menus (planned for P6).
- **`IsInteractive` returns `false`:** Interactive key-based navigation is not yet supported in the Avalonia frontend.

## Hardcoded Values in `App.axaml.cs`

The following values are hardcoded in `App.axaml.cs` and should be made configurable before shipping (tracked as `TODO(P3-P8)`):

| Line | Value | Hardcoded To | Should Be |
|------|-------|-------------|-----------|
| 49 | Difficulty | `Difficulty.Normal` | Player-selectable (P3 startup flow) |
| 59 | Dungeon seed | `12345` | Random or player-entered seed |
| 62 | Player name | `"Adventurer"` | Player-entered name (P3 startup flow) |
| 63 | Player class | `PlayerClass.Warrior` | Player-selectable (P3 class selection) |
| 68 | Input reader | `AvaloniaInputReader` | Correct for Avalonia — no change needed |

> **Note:** Lines 49–63 are intentional scaffolding for the P2 smoke test milestone. The full startup flow (name entry, class selection, difficulty picker) is planned for P3–P8 and is annotated with `TODO(P3-P8)` in the source.

## Verification Attempt (Headless)

**Date:** 2026-03-21
**Environment:** Linux (Wayland session, `DISPLAY=:0`, `WAYLAND_DISPLAY=wayland-0`)

- `dotnet build Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj` — ✅ 0 errors
- `dotnet run --project Dungnz.Display.Avalonia` — App starts, log shows `"Dungnz Avalonia GUI starting..."`, process remains running (GUI window opens but headless session prevents interaction)
- Process killed by `timeout 10` → exit code 124 (SIGTERM, not a crash)
- **Conclusion:** App launches and initializes correctly. Full interactive verification requires a desktop session with keyboard/mouse access.
