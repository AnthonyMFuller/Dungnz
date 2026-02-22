# Phase 4 Map Design

## 1. Overview
This design covers the implementation of Phase 4 map improvements, including strict fog of war, corridor connectors, color-coded symbols, and a legend.

## 2. Requirements & Scope
- **#239 Strict Fog of War:** Only visited rooms should be visible. Unvisited rooms should not be rendered.
- **#243 Corridor Connectors:** Visual lines connecting rooms to show pathways.
- **#247 Color-coded Symbols:** Different colors for different room types/states.
- **#248 Map Legend:** A key explaining the symbols and colors.

## 3. Detailed Design

### 3.1. Rendering Logic (Fog of War) - Hill
The `ShowMap(Room currentRoom)` method in `DisplayService` needs a significant overhaul.

**Current Logic:**
- BFS traverses *all* reachable rooms from `currentRoom`.
- Renders everything.

**New Logic:**
1. **Global Discovery:** The map shouldn't just be "reachable from current". It needs to know about *all* visited rooms.
   - *Problem:* `ShowMap` only takes `currentRoom`.
   - *Solution:* We need to traverse the entire dungeon graph (BFS from `currentRoom` is fine if the dungeon is fully connected, which it seems to be).
   - *Filter:* When rendering, we only include a room in the `grid` dictionary if `room.Visited == true` OR if it is an immediate neighbor of a visited room (optional, but "rooms not yet visited should not appear" suggests we might hide neighbors too. However, seeing an exit exists is useful. Let's decide: **Show visited rooms ONLY**. Neighbors that haven't been entered are invisible on the map, but the "Exits" list in the text description tells you they are there. This matches the strict "not yet visited should not appear" requirement.)
   - *Refinement:* Actually, standard roguelike "Fog of War" usually shows the *walls* of the room you are in and the *openings*, but not the content of the next room. On a grid map, this usually means showing the cell for the current room and previously visited rooms.

2. **Grid Construction:**
   - Run BFS/DFS to find *all* rooms in the dungeon (to establish the coordinate system relative to start or current).
   - *Optimization:* If we re-center `(0,0)` on `currentRoom` every time, the map shifts. This is acceptable for a relative map.
   - Filter the list of rooms to render: `visibleRooms = allRooms.Where(r => r.Visited)`
   - *Edge Case:* What if I visited a room, went away, and now it's disconnected from my current known path? (Unlikely in a connected dungeon, but possible if one-way doors existed. They don't seem to.)

### 3.2. Corridor Connectors - Hill
To draw connectors, we need a 2x render grid or distinct "connector" cells.
- **Grid Expansion:** Instead of a 1x1 grid where (0,0) is a room and (1,0) is next room, we can treat the map as having "spaces" between rooms.
- **Approach:**
   - Render Room at `(x, y)`.
   - Check Exits.
   - If Exit North exists: Draw `|` at `(x, y-1)`? No, that messes up the coordinate system if we just increment integers.
   - **Better Approach:** "Expanded Grid".
     - Room at `(x*2, y*2)`.
     - Connector at `(x*2 + dx, y*2 + dy)`.
     - `North` neighbor of `(x,y)` is at `(x,y-1)`. Connector is at `(x*2, y*2 - 1)`.
     - `East` neighbor of `(x,y)` is at `(x+1,y)`. Connector is at `(x*2 + 1, y*2)`.
   - **Symbols:**
     - North/South: `|`
     - East/West: `-`

### 3.3. Color Coding - Barton
We need new colors in `ColorCodes.cs` or reuse existing ones.
- **Room Types:**
  - `Standard`: Gray/White
  - `Dark`: DarkGray (`\u001b[90m` exists as `Gray`)
  - `Mossy`: Green (`\u001b[32m`)
  - `Flooded`: Blue (`\u001b[34m`)
  - `Scorched`: Red (`\u001b[31m`) or Yellow/Orange
  - `Ancient`: Cyan (`\u001b[36m`) or Magenta (Need to add `Magenta`)
- **States:**
  - `Current`: Bright White `[*]`, maybe blinking or bold?
  - `Visited`: Based on RoomType color.
  - `Exit`: Bright Yellow or Special.
  - `Enemy`: Red `[!]`.

**Action for Barton:**
- Add `Magenta` (`\u001b[35m`) to `ColorCodes`.
- Add `Orange` (if possible, ANSI 256 color `\u001b[38;5;208m` or just use `Yellow`)? Stick to standard 16 colors for compatibility. Use `Yellow` or `BrightRed` for Scorched.
- Create helper `GetRoomColor(Room room)` in `DisplayService` (or `ColorCodes` extension) that determines color based on `RoomType`.

### 3.4. Legend - Barton
- Display below the map.
- Format:
  `Legend:`
  `[*] You` (White)
  `[ ] Standard` (Gray)
  `[ ] Mossy` (Green)
  ...etc...
  `|/- Connections`

## 4. Interface Contracts

### 4.1. `DisplayService.ShowMap(Room currentRoom)`
- **Responsibility:** Hill
- **Logic:**
  1. BFS to build full coordinate map (keep this).
  2. Determine bounds (min/max X/Y).
  3. Loop `y` from `minY` to `maxY`.
     - Loop `x` from `minX` to `maxX`.
     - **Change:** Step size? Or render grid with gaps?
     - **Decision:** Render with 2-char spacing logic or 3x3 grid per cell?
     - *Simpler:* Keep `(x,y)` coordinates. When rendering:
       - Print Room Row: `[Room]--[Room]`
       - Print Connector Row: `  |      |  `
       - This implies iterating `y` and injecting a "connector row" between `y` and `y+1`.
       - And iterating `x` and injecting a "connector column" between `x` and `x+1`.
  4. **Strict Fog of War:**
     - `if (!room.Visited) continue;` (Don't draw room).
     - *Crucial:* If room is not visited, do we draw the connector leading to it?
     - Decision: **No**. If I haven't been there, I don't see the path.
     - *Exception:* Maybe I see the "stub" of the path leaving my current room?
     - *Simpler:* Only draw connectors between two `Visited` rooms.

### 4.2. `ColorCodes` extensions
- **Responsibility:** Barton
- **Additions:**
  - `public const string Magenta = "\u001b[35m";`
  - `public const string BrightGreen = "\u001b[92m";` (for Mossy?)

## 5. Implementation Plan

### Step 1: Color Codes (Barton)
- Modify `Systems/ColorCodes.cs`.
- Add `Magenta`.
- Add `GetColorForRoomType(RoomType type)` helper (or put in `DisplayService` private method).

### Step 2: Map Rendering (Hill)
- Modify `Display/DisplayService.cs`.
- Update `ShowMap`.
- Implement "Expanded Grid" rendering (interleaved rows/cols for connectors).
- Apply Fog of War filter (`room.Visited`).
- Apply Colors.
- Add Legend.

## 6. Risks & Edge Cases
- **Map Size:** Large dungeons might scroll off screen. (Accepted risk for now).
- **Orphaned Rooms:** If `Visited` filter is strict, map might look disjointed if you teleported (not possible yet).
- **ANSI Length:** `Colorize` adds characters. Ensure alignment isn't broken.
  - *Mitigation:* Reset color *after* the symbol `[?]`, but handle spacing carefully.
- **Complex Connectors:** What if diagonal? (Not supported by `Direction` enum).

## 7. Action Items
- **Coulson (Facilitator):** Commit this design doc.
- **Hill:** Refactor `ShowMap` for connectors and Fog of War.
- **Barton:** Update `ColorCodes` and support Legend.
