# Initialization Bundle Pattern

**Confidence:** medium  
**Source:** earned  
**Domain:** architecture, initialization  

## Pattern

When a setup/initialization service needs to return multiple related configuration values, return an immutable record (or readonly struct) containing all values as a single bundle rather than using out parameters or mutable state.

```csharp
// Setup service that orchestrates complex initialization
public class GameSetupService
{
    private readonly IDisplayService _display;

    public GameSetupService(IDisplayService display)
    {
        _display = display;
    }

    public GameSetup RunIntroSequence()
    {
        var seed = PromptForSeed();
        var difficulty = PromptForDifficulty();
        var player = CreatePlayer();
        
        return new GameSetup(player, seed, difficulty);
    }
}

// Immutable bundle of related initialization values
public record GameSetup(Player Player, int Seed, DifficultySettings Difficulty);
```

## Usage

**Entry point orchestration:**
```csharp
var setupService = new GameSetupService(display);
var setup = setupService.RunIntroSequence();

// Use bundled values to initialize other systems
var generator = new DungeonGenerator(setup.Seed);
var gameLoop = new GameLoop(display, combat, setup.Seed, setup.Difficulty);
gameLoop.Run(setup.Player, startRoom);
```

## Rationale

- **Immutability:** Records are immutable by default, preventing accidental modification
- **Type safety:** Compile-time guarantee all values are present (no nulls)
- **Clarity:** Single return type documents what the setup process produces
- **Refactoring:** Adding new initialization values doesn't break existing code structure
- **Testing:** Easy to create test bundles: `new GameSetup(testPlayer, 12345, normalDifficulty)`

## When to Use

✅ **Use when:**
- Setup process produces 3+ related configuration values
- Values are logically grouped (e.g., all game start state)
- Caller needs all values to proceed (not optional)
- Values don't change after initialization

❌ **Don't use when:**
- Only 1-2 return values (just return them directly)
- Values are optional or conditional (use Optional<T> or nullable)
- Values need to be updated after creation (use mutable class)
- Setup is interactive with branching flows (use builder pattern)

## Anti-Patterns to Avoid

❌ **Mutable class:**
```csharp
public class GameSetup
{
    public Player Player { get; set; } // Can be modified after creation
    public int Seed { get; set; }
}
```

❌ **Out parameters:**
```csharp
public Player RunIntroSequence(out int seed, out DifficultySettings difficulty)
{
    seed = 12345;
    difficulty = DifficultySettings.Normal;
    return player;
}
```

❌ **Tuple with unclear names:**
```csharp
public (Player, int, DifficultySettings) RunIntroSequence() // What's the int?
```

## Alternative: Positional Record Syntax

For simple bundles with self-documenting types:
```csharp
public record GameSetup(Player Player, int Seed, DifficultySettings Difficulty);
```

For complex bundles requiring validation:
```csharp
public record GameSetup
{
    public Player Player { get; init; }
    public int Seed { get; init; }
    public DifficultySettings Difficulty { get; init; }

    public GameSetup(Player player, int seed, DifficultySettings difficulty)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Seed = seed;
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
    }
}
```

## Related Patterns

- **Service initialization:** Services receive dependencies via constructor, return bundles from methods
- **Builder pattern:** For complex setup with optional steps (more ceremony, more flexibility)
- **Options pattern:** For configuration values loaded from external sources (appsettings.json)
