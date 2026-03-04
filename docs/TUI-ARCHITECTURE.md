# TUI Architecture: Terminal.Gui Integration

This document describes the Terminal.Gui TUI (Text User Interface) implementation, including the dual-thread model, display coordination, and file structure.

## Overview

The TUI mode provides a split-screen interface powered by [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) v2.34+. It runs on the main thread while the game loop executes on a background thread, coordinated through the **GameThreadBridge** pattern.

## Dual-Thread Model

```
Main Thread:
  ┌─────────────────────────────────┐
  │ Terminal.Gui Event Loop         │
  │ (TerminalGuiDisplayService)     │
  │ ├─ Map Panel                    │
  │ ├─ Stats Panel                  │
  │ ├─ Content Area                 │
  │ └─ Message Log                  │
  └──────────┬──────────────────────┘
             │
             │ Queue<string> messages
             │ Queue<GameState> state updates
             │
Background:  │
  ┌──────────▼──────────────────────┐
  │ GameLoop (background task)      │
  │ ├─ Command processing           │
  │ ├─ Combat/logic execution       │
  │ └─ Player state updates         │
  └─────────────────────────────────┘
```

### Communication

- **Main → Background:** Commands queued via `GameThreadBridge.EnqueueCommand()`
- **Background → Main:** Display updates queued and flushed on each refresh
- **Thread-safe:** Queues protect shared state; no direct memory access between threads

## GameThreadBridge Pattern

`Display/Tui/GameThreadBridge.cs` manages bidirectional communication:

```csharp
public class GameThreadBridge
{
    // Game thread → Display thread
    public void EnqueueCommand(string command);
    public bool TryGetNextCommand(out string command);
    
    // Display thread → Game thread (state/messages)
    public void QueueDisplayMessage(string message);
    public void QueueStateUpdate(GameState state);
    public List<string> FlushMessages();
    public GameState? GetLatestState();
}
```

**Benefits:**
- ✅ No locks required (queue-based coordination)
- ✅ Non-blocking I/O on both sides
- ✅ Safe for exceptions (game crash doesn't freeze display)
- ✅ Easy to test (mock the bridge)

## Panel Layout

The TUI is divided into four regions:

### 1. Map Panel (Top-Left, 50% width)
- **Content:** Live ASCII dungeon map with player position marked (`@`)
- **Update Frequency:** On room change, after player movement
- **Colors:** Room theme (dark, mossy, flooded, etc.) reflected in panel colors

### 2. Stats Panel (Top-Right, 50% width)
- **Content:** Player vitals and progression
  - `HP: 75 / 100 [==========>......] 75%`
  - `Mana: 40 / 50 [========>........] 80%`
  - `Level: 5 | XP: 240 / 300`
  - `ATK: 15 | DEF: 8 | Gold: 1250`
  - Current floor and turn count
- **Update Frequency:** Every game tick

### 3. Content Area (Bottom-Left to Center, 80% width)
- **Content:** Room description, enemy info, loot, merchant stock, inventory
- **Interaction:** Keyboard input for menus and selection
- **Rendering:** Via `TerminalGuiDisplayService.ShowMessage()` and menu dialogs

### 4. Message Log (Bottom, Full Width)
- **Content:** Recent 5–10 game messages (combat log, loot notifications, skill unlocks)
- **Update Frequency:** Appended on each message
- **Scrolling:** Auto-scrolls to latest; user can scroll up for history

## File Structure

```
Display/Tui/
├── GameThreadBridge.cs
│   └─ Thread-safe queue-based coordination between game and display threads
│
├── TerminalGuiDisplayService.cs
│   └─ IDisplayService implementation using Terminal.Gui
│      ├─ Main TUI initialization (window, panels, event handlers)
│      ├─ Panel refresh logic
│      ├─ ShowMessage(), RenderTable(), RenderProgress() implementations
│      └─ Event routing (user input → bridge → game loop)
│
├── TerminalGuiInputReader.cs
│   └─ IInputReader implementation using Terminal.Gui
│      ├─ ReadLine() → returns user input from the TUI text field
│      ├─ Input validation and queuing
│      └─ Special key handling (Enter, Escape, arrow keys)
│
├── TuiLayout.cs
│   └─ Panel definitions and layout manager
│      ├─ CreateMapPanel() → Map visualization
│      ├─ CreateStatsPanel() → Player stats display
│      ├─ CreateContentPanel() → Main content area
│      ├─ CreateMessageLogPanel() → Message log
│      └─ RefreshLayout() → Recalculate sizes on terminal resize
│
└── TuiMenuDialog.cs
    └─ Interactive menu dialogs (inventory, skills, shop)
       ├─ ArrowKeyMenu() → Navigate list with ↑/↓, select with Enter
       ├─ ConfirmDialog() → Yes/No confirmation
       └─ ItemComparisonDialog() → Side-by-side gear comparison
```

## Initialization

When `dotnet run -- --tui` is invoked:

1. **Program.cs** detects `--tui` flag
2. **TerminalGuiDisplayService** is instantiated (instead of `SpectreDisplayService`)
3. **TuiLayout** creates and arranges panels
4. **TerminalGuiInputReader** hooks into Terminal.Gui event system
5. **GameThreadBridge** is initialized
6. **GameLoop** starts on a background Task
7. **Terminal.Gui event loop** starts on main thread, polling the bridge for commands

## Key Design Patterns

### 1. **Display Abstraction**
Both `SpectreDisplayService` and `TerminalGuiDisplayService` implement `IDisplayService`, allowing the game engine to remain display-agnostic.

```csharp
public interface IDisplayService
{
    void ShowMessage(string message);
    void RenderTable(/* ... */);
    void RenderProgress(/* ... */);
    void Clear();
}
```

### 2. **Thread-Safe Queuing**
`GameThreadBridge` uses `ConcurrentQueue<T>` (or synchronized collections) to avoid locks:

```csharp
private ConcurrentQueue<string> _incomingCommands = new();
private ConcurrentQueue<string> _outgoingMessages = new();

public void EnqueueCommand(string cmd) => _incomingCommands.Enqueue(cmd);
public bool TryGetNextCommand(out string cmd) => _incomingCommands.TryDequeue(out cmd);
```

### 3. **Non-Blocking Event Loop**
Terminal.Gui's event loop continuously:
- Polls the bridge for new commands from the game thread
- Renders panel updates
- Listens for user input (keyboard, mouse)
- Routes input back to the game via the bridge

### 4. **Graceful Fallback**
If TUI initialization fails or terminal support is insufficient:
1. Catch exception in Program.cs
2. Fall back to `SpectreDisplayService`
3. Log warning to `Logs/dungnz-.log`
4. Continue with default display

## Performance Considerations

- **Panel Refresh:** Only panels with changed content are re-rendered (dirty flag pattern)
- **Map Rendering:** Dungeon map is cached and only regenerated on floor change
- **Message Log:** Fixed-size buffer (e.g., 10 messages) prevents memory bloat
- **Background Thread:** Game loop executes independently; TUI never blocks game logic

## Debugging & Logging

All TUI operations log to `%AppData%/Dungnz/Logs/dungnz-.log`:

```
[2024-01-15 14:23:45.123] [INF] [TerminalGuiDisplayService] TUI initialized
[2024-01-15 14:23:45.456] [DBG] [TerminalGuiInputReader] User input: "go north"
[2024-01-15 14:23:46.789] [DBG] [GameThreadBridge] Flushed 3 messages to display queue
```

Enable debug-level logging to trace thread coordination and panel updates.

## Known Limitations

1. **Terminal Size:** Minimum 100×30 recommended; smaller terminals may have rendering issues
2. **Unicode Support:** Requires UTF-8 terminal (Linux/macOS default, Windows Terminal required on Windows)
3. **Mouse Support:** Terminal.Gui v2 supports mouse; game commands are keyboard-only
4. **Color Palette:** Limited to 256-color or true-color depending on terminal capabilities
5. **Accessibility:** Screen readers may have limited support due to TUI nature

## Future Enhancements

- Mouse click support for menu selection
- Expandable message log with scrollback
- Configurable panel layouts
- Performance optimizations for slow terminals
- Color theme customization
