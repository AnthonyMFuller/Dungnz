# Skill: Domain Model Encapsulation Pattern

**Confidence:** medium  
**Source:** earned  
**Date:** 2026-02-20  
**Author:** Hill  
**Updated:** 2026-02-20 — Validated by Coulson in v2 architecture plan. Identified as critical refactor (R3) blocking save/load, equipment slots, and state integrity.

## Problem

Domain models with public setters allow invalid state mutations (negative HP, MaxHP = 0, stats < 0). Difficult to add validation, event hooks, or analytics later. Common in game entities, financial models, domain-driven design.

## Pattern: Private Setters + State Transition Methods

### Before (Anemic Model)

```csharp
public class Player
{
    public int HP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
    public int Attack { get; set; } = 10;
}

// Callers can corrupt state
player.HP = -50; // Invalid!
player.MaxHP = 0; // Broken!
player.HP = 999; // Exceeds MaxHP
```

### After (Encapsulated Model)

```csharp
public class Player
{
    private int _hp = 100;
    
    public int HP 
    { 
        get => _hp; 
        private set => _hp = Math.Clamp(value, 0, MaxHP); // Auto-validate
    }
    public int MaxHP { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    
    // State transitions with validation
    public void TakeDamage(int amount)
    {
        if (amount < 0) 
            throw new ArgumentException("Damage cannot be negative", nameof(amount));
        HP -= amount;
    }
    
    public void Heal(int amount)
    {
        if (amount < 0) 
            throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
        HP = Math.Min(HP + amount, MaxHP); // Cap at MaxHP
    }
    
    public void ModifyAttack(int delta)
    {
        Attack = Math.Max(0, Attack + delta); // Can't go negative
    }
    
    public void LevelUp(int newMaxHP, int attackBonus)
    {
        MaxHP = newMaxHP;
        Attack += attackBonus;
        HP = MaxHP; // Full heal on level up
    }
}

// Usage
player.TakeDamage(30); // Safe, validated
player.Heal(999); // Capped at MaxHP automatically
player.ModifyAttack(-50); // Clamped to 0
```

## Key Techniques

### 1. Private Setters

```csharp
public int HP { get; private set; }
```

External code can read but not mutate directly.

### 2. Validation in Setter

```csharp
private int _hp;
public int HP 
{ 
    get => _hp; 
    private set => _hp = Math.Clamp(value, 0, MaxHP); 
}
```

Enforce invariants at assignment (never exceeds MaxHP, never below 0).

### 3. State Transition Methods

```csharp
public void TakeDamage(int amount) { /* validation + mutation */ }
public void Heal(int amount) { /* validation + capping */ }
```

Named operations with business logic, not raw property setters.

### 4. Immutable Defaults (C# 9+)

```csharp
public string Name { get; init; } = string.Empty;
```

Set during construction, immutable thereafter.

### 5. Read-Only Collections

```csharp
private List<Item> _inventory = [];
public IReadOnlyList<Item> Inventory => _inventory.AsReadOnly();

public void AddItem(Item item) => _inventory.Add(item);
public bool RemoveItem(Item item) => _inventory.Remove(item);
```

Expose collections as read-only, mutate through explicit methods.

## Benefits

1. **Prevents invalid state bugs** — Impossible to set HP < 0 or > MaxHP
2. **Centralized validation** — All business rules in one place
3. **Extensibility** — Add logging, events, analytics in transition methods
4. **Serialization-friendly** — Private setters don't block JSON deserialization
5. **Intent clarity** — `TakeDamage(30)` clearer than `HP -= 30`

## Example: Event Hooks

```csharp
public event EventHandler<int>? DamageTaken;
public event EventHandler? Died;

public void TakeDamage(int amount)
{
    if (amount < 0) throw new ArgumentException(...);
    
    var oldHP = HP;
    HP -= amount;
    
    DamageTaken?.Invoke(this, amount);
    if (HP == 0 && oldHP > 0)
        Died?.Invoke(this, EventArgs.Empty);
}
```

Future-proofs for achievements, UI updates, analytics.

## Applicability

- Game entities (player, enemy, NPC stats)
- Financial models (account balance, transactions)
- Domain-driven design (aggregates with invariants)
- Any model where state integrity matters

## Migration Strategy

1. Change public setters to private
2. Add state transition methods
3. Update all call sites: `player.HP = X` → `player.TakeDamage(Y)`
4. Add validation in transition methods
5. Optionally add events/logging

## Trade-offs

- **Pro:** Prevents bugs, enforces invariants, extensible
- **Con:** More verbose (methods vs direct setters)
- **Con:** Breaking change (existing callers must refactor)

## C# Features Used

- Private setters (C# 3+)
- Init-only setters (C# 9)
- Expression-bodied properties (C# 6)
- Null-coalescing assignment (C# 8)
- Math.Clamp (C# 8)

## Related Patterns

- Tell, Don't Ask (OOP principle)
- Command pattern (state transitions as commands)
- Observer pattern (for event hooks)
- Value Object (immutable data with validation)

## Tags

encapsulation, domain-model, validation, oop, c#, state-management
