# Dependency Injection for Testability Pattern

**Confidence:** low  
**Source:** earned  
**Domain:** testing, architecture  

## Pattern

When building systems that interact with external dependencies (randomness, I/O, time, file system), inject those dependencies via constructor parameters with optional defaults for production use.

```csharp
public class CombatEngine
{
    private readonly IDisplayService _display;
    private readonly Random _rng;
    
    // Constructor accepts dependencies with optional production defaults
    public CombatEngine(IDisplayService display, Random? rng = null)
    {
        _display = display;
        _rng = rng ?? new Random(); // Fallback to new instance if not provided
    }
}
```

## Usage

**Production code:**
```csharp
var display = new ConsoleDisplayService();
var combat = new CombatEngine(display); // Uses new Random() internally
```

**Test code:**
```csharp
var mockDisplay = new Mock<IDisplayService>();
var deterministicRng = new Random(42); // Seeded for reproducibility
var combat = new CombatEngine(mockDisplay.Object, deterministicRng);
```

## Rationale

- **Testability:** Enables deterministic testing of systems with randomness or external state
- **No production impact:** Optional parameter pattern means production code needs no changes
- **Single responsibility:** Production code doesn't need to know about test seeding
- **Future flexibility:** Easy to inject alternative implementations (e.g., cryptographic random, weighted random, replay random from saved game)

## Common Dependencies to Inject

- **Random/RNG:** Combat systems, loot drops, procedural generation
- **I/O services:** `IDisplayService`, `IFileSystem`, `INetworkClient`
- **Time:** `IClock` or `Func<DateTime>` for time-dependent mechanics
- **Configuration:** Game balance parameters, feature flags

## Anti-Patterns to Avoid

❌ **Hardcoded dependencies:**
```csharp
public class CombatEngine
{
    private readonly Random _rng = new Random(); // Can't control in tests
}
```

❌ **Global static state:**
```csharp
public static class GameRandom
{
    public static Random Instance { get; } = new Random(); // Shared state breaks test isolation
}
```

❌ **Service locator inside class:**
```csharp
public class CombatEngine
{
    private readonly Random _rng = ServiceLocator.Get<Random>(); // Hidden dependency
}
```

## Related Patterns

- **Interface extraction:** Extract interfaces for mockable dependencies (IDisplayService, IFileSystem)
- **Builder pattern:** Create test data factories that use injected dependencies
- **Arrange-Act-Assert:** Structure tests to inject dependencies in Arrange phase
