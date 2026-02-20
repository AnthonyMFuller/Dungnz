### 2026-02-20: Player Encapsulation Implementation Pattern

**By:** Hill  
**Context:** GitHub Issue #2 — Player encapsulation refactor

**What:**
Implemented complete Player encapsulation with private setters, validated mutation methods, and OnHealthChanged event. All 9 Player properties now use private set. Added 7 public methods (TakeDamage, Heal, AddGold, AddXP, ModifyAttack, ModifyDefense, LevelUp) with input validation and clamping.

**Why:**
1. **Prevent invalid state:** Direct property setters allowed negative HP, exceeding MaxHP, stat underflows
2. **Enable future features:** Controlled state changes support save/load, analytics, achievements
3. **Clean API:** Game systems interact through intent-revealing methods (TakeDamage vs HP -= dmg)
4. **Event-driven:** OnHealthChanged event enables reactive systems without coupling

**Pattern Details:**
```csharp
// Private setters for all mutable state
public int HP { get; private set; } = 100;

// Validated mutation with clamping
public void TakeDamage(int amount)
{
    if (amount < 0)
        throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));
    
    var oldHP = HP;
    HP = Math.Max(0, HP - amount);
    
    if (HP != oldHP)
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
}

// Event for state change notifications
public event EventHandler<HealthChangedEventArgs>? OnHealthChanged;
```

**Caller Impact:**
- CombatEngine: 4 call sites (flee damage, combat damage, gold, XP)
- GameLoop: 3 call sites (heal, equip weapon/armor)
- All direct property assignments replaced with method calls

**Testing:**
- Build passes cleanly
- Existing game loop logic unchanged (same behavior, safer implementation)

**Recommendation:**
- Apply same pattern to Enemy class (TakeDamage, ModifyStats)
- Consider IReadOnlyList<Item> for Inventory exposure (prevent external mutation)

**PR:** #26 (squad/2-player-encapsulation)
