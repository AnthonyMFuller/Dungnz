# Display Layer Input Validation

**Pattern:** Encapsulate input validation and re-prompting logic within display layer methods that return domain types, not raw strings.

**Confidence:** low  
**Source:** earned  
**Observed in:** Dungnz TextGame v3 intro sequence architecture design (2026-02-22)

---

## Problem

When building interactive console prompts (difficulty selection, class selection, yes/no confirmations), you must decide:
- Who validates user input?
- Who handles retry loops?
- Who returns strongly-typed domain values?

**Anti-pattern: Caller-side validation**
```csharp
// Program.cs (caller)
Console.WriteLine("Choose difficulty: [1] Casual [2] Normal [3] Hard");
var input = Console.ReadLine();
var difficulty = input switch {
    "1" => Difficulty.Casual,
    "3" => Difficulty.Hard,
    _ => Difficulty.Normal  // silent fallback
};
```

**Problems:**
- Input validation scattered across callers
- No retry loop (invalid input silently defaults)
- No user feedback on invalid input
- Caller deals with raw strings instead of domain types

---

## Solution

**Display layer owns validation loops and returns domain types.**

```csharp
// IDisplayService.cs
public interface IDisplayService
{
    Difficulty ShowDifficultySelection();
    PlayerClassDefinition ShowClassSelection(PrestigeData? prestige);
    string ShowNamePrompt();
}
```

```csharp
// ConsoleDisplayService.cs
public Difficulty ShowDifficultySelection()
{
    while (true)
    {
        ShowMessage("Choose difficulty: [1] Casual [2] Normal [3] Hard");
        ShowCommandPrompt();
        var input = Console.ReadLine()?.Trim() ?? "";
        
        var difficulty = input switch {
            "1" => Difficulty.Casual,
            "2" => Difficulty.Normal,
            "3" => Difficulty.Hard,
            _ => (Difficulty?)null
        };
        
        if (difficulty.HasValue)
            return difficulty.Value;
        
        ShowError("Invalid choice. Please enter 1, 2, or 3.");
    }
}
```

```csharp
// Program.cs (caller)
var difficulty = display.ShowDifficultySelection();  // Guaranteed valid
var playerClass = display.ShowClassSelection(prestige);  // Guaranteed valid
var name = display.ShowNamePrompt();  // Always returns non-empty string
```

---

## Benefits

1. **Single Responsibility:** Display layer owns UX loop; caller owns business logic
2. **Type Safety:** Caller receives domain types (Difficulty enum), not raw strings
3. **No Silent Failures:** Invalid input prompts retry, never silently defaults
4. **Consistent UX:** Validation/retry behavior consistent across all prompts
5. **Testability:** Test doubles can return canned domain values without simulating user input

---

## When to Use

✅ **Use for:**
- Enumeration selection (difficulty, class, yes/no)
- Multi-step prompts with validation rules
- Inputs with constrained value sets (1-3, Y/N)

❌ **Don't use for:**
- Free-text inputs with no validation (e.g., player name — allow empty, let caller decide default)
- Inputs requiring domain logic to validate (e.g., "equip sword" — requires inventory check, not display concern)

---

## Implementation Guidelines

**1. Display Method Signature:**
- Return domain type (Difficulty, PlayerClass), not string
- Accept context needed for display (e.g., PrestigeData to show calculated stats)
- Never accept or return Player/GameState for validation — display layer should not know game state

**2. Validation Loop:**
- Infinite loop with explicit `return` on valid input
- Show error message via DisplayService (not Console.Write directly)
- Re-prompt without re-showing full context (avoid screen spam)

**3. Caller Responsibility:**
- Receives guaranteed-valid domain type
- Applies business logic (e.g., apply class bonuses to Player)
- Does NOT re-validate display layer output

---

## Example: Complex Multi-Context Prompt

```csharp
public PlayerClassDefinition ShowClassSelection(PrestigeData? prestige)
{
    // Show full class descriptions with calculated stats
    ShowClassDescriptions(prestige);  // Helper: render full screen
    
    while (true)
    {
        ShowCommandPrompt();
        var input = Console.ReadLine()?.Trim() ?? "";
        
        var classChoice = input switch {
            "1" => PlayerClassDefinition.Warrior,
            "2" => PlayerClassDefinition.Mage,
            "3" => PlayerClassDefinition.Rogue,
            _ => null
        };
        
        if (classChoice != null)
            return classChoice;
        
        // Don't re-render full screen, just show error inline
        ShowError("Invalid choice. Please enter 1, 2, or 3.");
    }
}

private void ShowClassDescriptions(PrestigeData? prestige)
{
    // Render full class selection screen with stats
    Console.WriteLine("╠═══════════════════════════════════════════╣");
    Console.WriteLine("║  Choose your class:                       ║");
    Console.WriteLine("║  [1] Warrior - High HP and defense        ║");
    // ... etc
}
```

---

## Trade-offs

**Advantages:**
- Callers always receive valid domain objects
- UX consistency enforced at layer boundary
- Display layer fully testable with mock input

**Disadvantages:**
- Display layer becomes slightly more complex (validation logic)
- Display layer must know about domain enums (Difficulty, PlayerClass)
- Can't easily extract prompts to external config (validation is code)

**Mitigation:**
- Keep validation simple (enum mapping only, no business rules)
- If domain logic needed (e.g., "can player afford item?"), return raw input and let caller validate

---

## Anti-patterns to Avoid

❌ **Returning raw strings with caller validation:**
```csharp
public string ShowDifficultyPrompt() { ... }  // Returns "1", "2", "3"
// Caller must validate and map to enum
```

❌ **Silent defaults on invalid input:**
```csharp
var difficulty = input == "1" ? Difficulty.Casual : Difficulty.Normal;
// User typed "x" but gets Normal without feedback
```

❌ **Display layer calling domain logic:**
```csharp
public PlayerClassDefinition ShowClassSelection(Player player)
{
    if (player.HasCompletedRun()) { ... }  // NO: display should not query game state
}
```

---

## Related Patterns

- **Interface Extraction:** IDisplayService enables test doubles that return canned domain values
- **Result Enums Over Exceptions:** Validation failures loop, don't throw
- **Dependency Injection:** Display layer injected into Program.cs, owns UX loop

---

**Next Steps:** If this pattern proves valuable across multiple projects, promote to medium confidence. If domain logic creeps into display validation, consider refactoring to return Result<T> with caller-side validation.
