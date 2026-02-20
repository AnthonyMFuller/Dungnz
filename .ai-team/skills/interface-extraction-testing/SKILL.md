# Interface Extraction for External Dependencies

**Confidence:** medium  
**Source:** earned  
**Domain:** testing, architecture, dependency-inversion

**Updated:** 2026-02-20 — Confirmed in Dungnz v2 architecture plan. Pattern successfully used for IDisplayService (Console I/O), IRandom (deterministic testing), and IInputService (UI decoupling).  

## Pattern

Extract interfaces for external dependencies (Console I/O, file system, network, time) to enable mocking in tests and alternative implementations in production.

```csharp
// Before: Concrete dependency on Console
public class DisplayService
{
    public void ShowMessage(string msg) => Console.WriteLine(msg);
}

// After: Interface extraction
public interface IDisplayService
{
    void ShowMessage(string msg);
    void ShowCombatStatus(Player player, Enemy enemy);
    void ShowInventory(Player player);
}

public class ConsoleDisplayService : IDisplayService
{
    public void ShowMessage(string msg) => Console.WriteLine(msg);
    // ... other methods
}

// Test implementation
public class TestDisplayService : IDisplayService
{
    public List<string> Messages { get; } = new();
    public void ShowMessage(string msg) => Messages.Add(msg);
    // ... other methods capturing output
}
```

## Usage

**Production code:**
```csharp
IDisplayService display = new ConsoleDisplayService();
var gameLoop = new GameLoop(display, combat);
```

**Test code (manual mock):**
```csharp
var testDisplay = new TestDisplayService();
var gameLoop = new GameLoop(testDisplay, combat);
gameLoop.Run(player, room);

// Verify output
Assert.Contains("You defeated", testDisplay.Messages);
```

**Test code (Moq):**
```csharp
var mockDisplay = new Mock<IDisplayService>();
var gameLoop = new GameLoop(mockDisplay.Object, combat);
gameLoop.Run(player, room);

// Verify method called
mockDisplay.Verify(d => d.ShowMessage(It.Is<string>(s => s.Contains("LEVEL UP"))), Times.Once);
```

## Rationale

- **Testability:** Enables headless testing without real Console I/O, file operations, or network calls
- **Dependency inversion:** High-level game logic depends on abstractions, not concrete implementations
- **Alternative implementations:** GUI, web UI, Discord bot, test harness all implement same interface
- **Verification in tests:** Can verify that specific messages were shown or methods were called

## Common Dependencies to Extract

- **Console I/O:** `IDisplayService`, `IInputReader`
- **File system:** `IFileSystem`, `ISaveGameRepository`
- **Network:** `INetworkClient`, `ILeaderboardService`
- **Time:** `IClock` (with `DateTime GetNow()`)
- **Random:** Not typically interfaced, use dependency injection with concrete `Random` class

## When to Extract Interfaces

✅ **Extract when:**
- Dependency is external (I/O, network, time, hardware)
- Tests need to mock behavior (e.g., simulate file not found, network timeout)
- Multiple implementations likely (console, GUI, web, test)

❌ **Don't extract when:**
- Pure logic classes with no external state (e.g., `CommandParser`)
- DTOs or data models (e.g., `Player`, `Enemy`)
- Internal domain logic (e.g., `CombatEngine` — test with real instance)

## Refactoring Strategy

1. **Extract interface** from existing concrete class
2. **Rename concrete class** to implementation-specific name (e.g., `DisplayService` → `ConsoleDisplayService`)
3. **Update all consumers** to depend on interface type (change constructor parameters, field types)
4. **Create test implementation** (e.g., `TestDisplayService`) that captures output
5. **Verify no breaking changes** (production code should work unchanged, just different variable types)

## Anti-Patterns to Avoid

❌ **Interface for every class:**
```csharp
// Don't extract interfaces for pure logic or DTOs
public interface IPlayer { ... } // ❌ Player is a data model, not a service
public interface ICommandParser { ... } // ❌ Pure logic, just test the class directly
```

❌ **Leaky abstractions:**
```csharp
public interface IDisplayService
{
    void WriteToConsole(string msg); // ❌ "Console" leaks implementation detail
}

// Better:
public interface IDisplayService
{
    void ShowMessage(string msg); // ✅ Abstract, implementation-agnostic
}
```

❌ **Overly granular interfaces:**
```csharp
public interface IMessageDisplayer { void ShowMessage(string msg); }
public interface ICombatDisplayer { void ShowCombat(string msg); }
public interface IInventoryDisplayer { void ShowInventory(Player p); }

// Better: Single cohesive interface
public interface IDisplayService
{
    void ShowMessage(string msg);
    void ShowCombat(string msg);
    void ShowInventory(Player player);
}
```

## Related Patterns

- **Dependency injection:** Inject `IDisplayService` via constructor (enables testability)
- **Test doubles:** Create `TestDisplayService` that captures output for assertion
- **Moq framework:** Use `Mock<IDisplayService>` for verification-based testing
