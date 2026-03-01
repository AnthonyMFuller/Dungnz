# Architectural Decision: Mini-Map Overhaul

**Date:** 2026-03-02
**Author:** Coulson (Lead)
**Requested by:** Boss
**Issues:** #823, #824, #825, #826, #827

---

## Context

The current mini-map is functional but minimal. All cleared rooms show identical `[+]` symbols, unvisited rooms are completely hidden, and the legend is hardcoded. This plan transforms the mini-map into a real exploration and decision-making tool.

## Priority Classification

| Priority | Issue | Title | Impact |
|----------|-------|-------|--------|
| **P0** | #825 | Floor Number in Header + Interface Change | Tiny effort, high info, **unblocks other work** |
| **P0** | #823 | Fog of War — Adjacent Unknowns | Most impactful visual change |
| **P0** | #824 | Rich Room Type Symbols | Most impactful information change |
| **P1** | #826 | Dynamic Legend | Required once #824 lands |
| **P2** | #827 | Visual Polish — Corridors & Compass | Pure cosmetic |

## Implementation Order

### Phase 1: Interface Change (#825)
**Assignee:** Hill
**Why first:** Changes the `IDisplayService.ShowMap` signature. Must be done before any other work to avoid merge conflicts across the 6 affected files.

Changes:
```csharp
// IDisplayService.cs
void ShowMap(Room currentRoom, int currentFloor);

// GameLoop.cs (line ~209)
_display.ShowMap(_currentRoom, _currentFloor);

// SpectreDisplayService.cs — panel header
Header = new PanelHeader($"[bold white]Mini-Map — Floor {currentFloor}[/]")

// DisplayService.cs, TestDisplayService.cs, FakeDisplayService.cs — signature only
```

### Phase 2: Fog of War (#823)
**Assignee:** Hill
**Why second:** Adds the biggest visual transformation — the map goes from sparse dots to a real exploration grid. Independent of symbol work.

Key changes to `ShowMap()`:
1. After computing `visiblePositions`, compute `fogPositions`:
   ```csharp
   var visitedSet = new HashSet<Room>(visiblePositions.Select(kv => kv.Key));
   var fogPositions = positions
       .Where(kv => !visitedSet.Contains(kv.Key) && kv.Key != currentRoom)
       .Where(kv => kv.Key.Exits.Values.Any(n => visitedSet.Contains(n)))
       .ToList();
   ```
2. Include fog positions in the bounding box (`minX/maxX/minY/maxY`)
3. Use a `fogGrid` dictionary (or mark in main grid) to distinguish fog from visited
4. Render fog rooms as `[grey][[?]][/]`
5. Draw corridor connectors between visited↔fog rooms (same as visited↔visited)

### Phase 3: Rich Room Symbols (#824)
**Assignee:** Hill
**Why third:** Builds on the rendering infrastructure from Phase 2. Replaces `GetMapRoomSymbol` with the full priority-based symbol table.

#### Symbol Priority Table (Definitive)

```
Priority  Condition                                    Symbol    Color           Spectre Markup
────────  ───────────────────────────────────────────  ──────    ─────────────   ──────────────────────────
 1        r == currentRoom                             [@]       bold yellow     [bold yellow][[@]][/]
 2        fog room (unvisited, adjacent to visited)    [?]       grey            [grey][[?]][/]
 3        r.IsExit && r.Enemy?.HP > 0                  [B]       bold red        [bold red][[B]][/]
 4        r.IsExit                                     [E]       white           [white][[E]][/]
 5        r.Enemy?.HP > 0                              [!]       red             [red][[!]][/]
 6        r.HasShrine && !r.ShrineUsed                 [S]       cyan            [cyan][[S]][/]
 7        r.Merchant != null                           [$]       green           [green][[$]][/]
 8        r.Items.Count > 0                            [i]       yellow          [yellow][[i]][/]
 9        r.Type == TrapRoom && !r.SpecialRoomUsed     [T]       darkorange3     [darkorange3][[T]][/]
10        r.Type == PetrifiedLibrary && !SpecialUsed   [L]       dodgerblue1     [dodgerblue1][[L]][/]
11        r.Type == ContestedArmory && !SpecialUsed    [A]       mediumpurple2   [mediumpurple2][[A]][/]
12        r.EnvironmentalHazard == LavaSeam            [~]       orangered1      [orangered1][[~]][/]
13        r.EnvironmentalHazard == CorruptedGround     [%]       darkgreen       [darkgreen][[%]][/]
14        r.EnvironmentalHazard == BlessedClearing     [♥]       springgreen2    [springgreen2][[♥]][/]
15        fallback (cleared)                           [+]       white           [white][[+]][/]
```

**Design decisions:**
- Merchant condition: `r.Merchant != null` (show even if inventory empty — player might want to sell)
- Trap rooms: Show `[T]` even after triggered (narratively the trap is still there)
- Special rooms after use: Fall through to hazard or `[+]`
- `[♥]` for BlessedClearing: single Unicode char, fits in `[X]` pattern, visually distinct and positive

#### `GetMapRoomSymbol` Refactored Signature
```csharp
private static string GetMapRoomSymbol(Room r, Room currentRoom, bool isFog = false)
```
When `isFog == true`, return `[grey][[?]][/]` immediately.

### Phase 4: Dynamic Legend (#826)
**Assignee:** Hill
**Why fourth:** Only makes sense after Phase 3 adds the new symbols.

- Track which symbol keys appeared during rendering via `HashSet<string>`
- Build legend from only visible symbols
- Wrap at 6 entries per line
- Use static `LegendEntries` dictionary for maintainability

### Phase 5: Visual Polish (#827)
**Assignee:** Hill (if time permits) or defer
**Why last:** Pure cosmetic. The map is fully functional without it. Box-drawing characters (`─`, `│`) and a compass rose.

## Interface Changes Summary

| File | Change |
|------|--------|
| `Display/IDisplayService.cs:132` | `void ShowMap(Room currentRoom, int currentFloor);` |
| `Display/SpectreDisplayService.cs:455` | Add `int currentFloor` param, use in PanelHeader |
| `Display/DisplayService.cs:817` | Add `int currentFloor` param |
| `Engine/GameLoop.cs:209` | Pass `_currentFloor` |
| `Dungnz.Tests/Helpers/TestDisplayService.cs:23` | Add param |
| `Dungnz.Tests/Helpers/FakeDisplayService.cs:30` | Add param |

## Risks

1. **Terminal width** — More rooms visible (fog) means wider maps. Current padding of 3-4 chars per cell should be fine for typical 80-col terminals up to ~15 rooms wide. Monitor.
2. **Spectre color names** — `darkorange3`, `dodgerblue1`, `mediumpurple2` etc. are Spectre.Console named colors. If a color doesn't exist, it will throw. Hill should verify each color name against Spectre docs.
3. **Unicode `♥` width** — U+2665 BLACK HEART SUIT is typically narrow (1 column) in most terminals. If it renders wide, replace with ASCII `[*]` and disambiguate from the legend.

## Branch Strategy

Single feature branch: `squad/minimap-overhaul`
- Phase 1 commit: interface change
- Phase 2 commit: fog of war
- Phase 3 commit: rich symbols
- Phase 4 commit: dynamic legend
- Phase 5 commit: visual polish (if included)

One PR at the end, squash-merge to master.
