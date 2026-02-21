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

## Updated: 2026-02-20 — Full Player Implementation with Event Pattern

**Confidence:** high (validated through production implementation in Dungnz)  
**Implementation:** Models/Player.cs (PR #26)  
**Updated:** 2026-02-20 — Bug audit revealed inconsistent application: Player strong, Enemy/Room weak

### Complete Implementation Example

```csharp
public class Player
{
    public string Name { get; set; } = string.Empty;
    public int HP { get; private set; } = 100;
    public int MaxHP { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    public int Defense { get; private set; } = 5;
    public int Gold { get; private set; }
    public int XP { get; private set; }
    public int Level { get; private set; } = 1;
    public List<Item> Inventory { get; private set; } = new();

    public event EventHandler<HealthChangedEventArgs>? OnHealthChanged;

    public void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));

        var oldHP = HP;
        HP = Math.Max(0, HP - amount);
        
        if (HP != oldHP)
            OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

    public void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Heal amount cannot be negative.", nameof(amount));

        var oldHP = HP;
        HP = Math.Min(MaxHP, HP + amount);
        
        if (HP != oldHP)
            OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Gold amount cannot be negative.", nameof(amount));
        Gold += amount;
    }

    public void AddXP(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("XP amount cannot be negative.", nameof(amount));
        XP += amount;
    }

    public void ModifyAttack(int delta)
    {
        Attack = Math.Max(1, Attack + delta);
    }

    public void ModifyDefense(int delta)
    {
        Defense = Math.Max(0, Defense + delta);
    }

    public void LevelUp()
    {
        Level++;
        ModifyAttack(2);
        ModifyDefense(1);
        MaxHP += 10;
        var oldHP = HP;
        HP = MaxHP;
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }
}

public class HealthChangedEventArgs : EventArgs
{
    public int OldHP { get; }
    public int NewHP { get; }
    public int Delta => NewHP - OldHP;

    public HealthChangedEventArgs(int oldHP, int newHP)
    {
        OldHP = oldHP;
        NewHP = newHP;
    }
}
```

### Key Refinements from Production

1. **Event-driven health changes:** OnHealthChanged fires only when HP actually changes (guards with `if (HP != oldHP)`)
2. **Custom EventArgs:** HealthChangedEventArgs provides OldHP, NewHP, and computed Delta for subscribers
3. **Min guards on stats:** Attack clamped to minimum 1 (never 0), Defense to minimum 0
4. **Composition in LevelUp:** Calls ModifyAttack/ModifyDefense to reuse clamping logic
5. **Validation everywhere:** All mutation methods validate negative inputs (fail-fast)

### Caller Migration Examples

**Before (GameLoop):**
```csharp
var healedAmount = Math.Min(item.HealAmount, _player.MaxHP - _player.HP);
_player.HP += healedAmount;
_player.Inventory.Remove(item);
_display.ShowMessage($"You use {item.Name} and restore {healedAmount} HP.");
```

**After (GameLoop):**
```csharp
var oldHP = _player.HP;
_player.Heal(item.HealAmount);
var healedAmount = _player.HP - oldHP;
_player.Inventory.Remove(item);
_display.ShowMessage($"You use {item.Name} and restore {healedAmount} HP.");
```

**Before (CombatEngine):**
```csharp
player.Level = newLevel;
player.Attack += 2;
player.Defense += 1;
player.MaxHP += 10;
player.HP = player.MaxHP;
_display.ShowMessage($"LEVEL UP! You are now level {player.Level}!");
```

**After (CombatEngine):**
```csharp
player.LevelUp();
_display.ShowMessage($"LEVEL UP! You are now level {player.Level}!");
```

### Production Metrics
- 4 files changed, 94 insertions(+), 22 deletions(-)
- 7 caller sites updated (4 in CombatEngine, 3 in GameLoop)
- Zero build warnings
- Clean upgrade path from anemic model

### Future Extensions Enabled
- **Analytics:** Subscribe to OnHealthChanged for damage tracking
- **Achievements:** Detect "defeat boss at 1 HP" via events
- **Save/Load:** All state changes go through validated methods
- **UI Updates:** Health bar reactively updates from event

## Updated: 2026-02-20 — Inconsistent Application Identified

**Context:** Pre-v3 bug audit revealed partial pattern adoption across codebase.

### Pattern Adoption Status

**Player Model (COMPLETE):**
- ✅ Private setters on all stats
- ✅ Validation methods (TakeDamage, Heal, ModifyAttack, ModifyDefense, LevelUp)
- ✅ Event-driven (OnHealthChanged)
- ✅ Guards (Math.Clamp, ArgumentException)

**Enemy Model (INCOMPLETE):**
- ❌ Public setters on HP, Attack, Defense, MaxHP
- ❌ Direct HP mutations in 5+ locations (CombatEngine:255, AbilityManager:143, StatusEffectManager:57/61/65)
- ❌ No TakeDamage/Heal methods, no validation, allows negative HP
- ❌ IsElite/IsAmbush public setters enable runtime exploits

**Room Model (INCOMPLETE):**
- ❌ Public setters on Visited, Looted, ShrineUsed
- ❌ Direct property mutations (6+ call sites in GameLoop)
- ❌ No encapsulation methods (MarkVisited, MarkLooted)

**Inventory (HYBRID):**
- ⚠️ List<Item> with private setter BUT external code calls .Add/.Remove directly
- ⚠️ Bypasses future validation (weight, capacity, quest triggers)

### Lessons from Inconsistent Application

1. **Partial adoption creates confusion:** New contributors see Player pattern but Enemy allows direct mutation.
2. **Refactoring cost compounds:** Adding Enemy.TakeDamage now requires migrating 5+ call sites.
3. **Missed event opportunities:** No Enemy.OnDeath, Room.OnVisited events for analytics/achievements.
4. **Validation gaps persist:** Enemy allows negative HP, Room allows visited=false after entry.

### Recommendation: Standardize Universally

Apply Player pattern to ALL domain models before v3:
1. Enemy.TakeDamage(int amount) — Replace 5+ direct HP mutations
2. Room.MarkVisited() / MarkLooted() — Replace direct setters
3. Player.AddItem(Item) / RemoveItem(Item) — Replace Inventory.Add/Remove
4. IsElite/IsAmbush as init-only — Prevent runtime mutation

**Effort:** 4-6 hours (Enemy 2h, Room 1h, Inventory 1h, testing 2h)  
**Impact:** Enables v3 class/trait systems, equipment sets, elite variants
