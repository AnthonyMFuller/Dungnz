# Diminishing Returns Formulas for Game Balance

**Confidence:** low  
**Source:** earned  
**Domain:** game-balance, rpg-mechanics, stat-systems

## Pattern

When designing stat-based mechanics (dodge, crit resistance, armor penetration), use diminishing returns formulas to prevent degenerate cases (e.g., 100% dodge, invincibility) while rewarding stat investment.

**Core Formula:** `stat / (stat + constant)`

This creates a curve where:
- Low stat values provide noticeable benefit
- High stat values approach asymptotic limit (never reach 100%)
- The constant determines curve steepness

## Implementation

```csharp
// Dodge chance based on Defense stat
private bool RollDodge(int defense)
{
    var dodgeChance = defense / (double)(defense + 20);
    return _rng.NextDouble() < dodgeChance;
}
```

**Curve characteristics:**
- 0 Defense → 0% dodge (0/20 = 0)
- 5 Defense → 20% dodge (5/25 = 0.2)
- 10 Defense → 33% dodge (10/30 = 0.33)
- 20 Defense → 50% dodge (20/40 = 0.5)
- 40 Defense → 67% dodge (40/60 = 0.67)
- 100 Defense → 83% dodge (100/120 = 0.83)
- ∞ Defense → 100% dodge (asymptotic limit)

## Tuning the Constant

The denominator constant controls curve steepness:

**Lower constant = steeper curve (faster growth):**
```csharp
defense / (defense + 10) // Reaches 50% dodge at 10 DEF
```

**Higher constant = gentler curve (slower growth):**
```csharp
defense / (defense + 50) // Reaches 50% dodge at 50 DEF
```

**Rule of thumb:** The constant equals the stat value where you reach 50% effectiveness.

## Use Cases

### 1. Dodge/Evasion Systems
```csharp
dodgeChance = defense / (defense + 20)
// 20 DEF = 50% dodge, prevents invincibility
```

### 2. Critical Resistance
```csharp
critResist = resilience / (resilience + 30)
baseCritChance *= (1 - critResist)
// 30 Resilience = 50% crit reduction, not immunity
```

### 3. Armor Penetration
```csharp
penetration = armorPen / (armorPen + 40)
effectiveArmor = targetArmor * (1 - penetration)
// 40 Pen = halves enemy armor, not negates it
```

### 4. Status Resistance
```csharp
poisonResist = vitality / (vitality + 25)
poisonDuration *= (1 - poisonResist)
// 25 Vitality = half poison duration
```

### 5. Gold Find / Magic Find
```csharp
bonusGold = goldFind / (goldFind + 100)
totalGold = baseGold * (1 + bonusGold)
// 100 Gold Find = 2x gold, can't reach infinite
```

## Why This Pattern Works

✅ **Prevents degenerate cases:** No 100% dodge, no immunity  
✅ **Smooth curve:** Every stat point provides value  
✅ **Intuitive tuning:** Constant = stat needed for 50% effect  
✅ **Self-balancing:** High investment yields diminishing returns naturally  
✅ **PvP friendly:** No hard counters (X stat beats Y stat always)

## Anti-Patterns to Avoid

❌ **Linear scaling:**
```csharp
dodgeChance = defense * 0.05 // 20 DEF = 100% dodge, broken
```

❌ **Hard caps:**
```csharp
dodgeChance = Math.Min(defense * 0.03, 0.75) // Awkward breakpoint at 25 DEF
```

❌ **Exponential scaling:**
```csharp
dodgeChance = 1 - Math.Pow(0.95, defense) // 90 DEF = 99% dodge, unfun
```

## Testing the Curve

Validate your formula by plotting breakpoints:

```csharp
[Theory]
[InlineData(0, 0.0)]    // No investment = no effect
[InlineData(5, 0.20)]   // Early investment = noticeable
[InlineData(20, 0.50)]  // Medium investment = 50% effect
[InlineData(40, 0.67)]  // High investment = strong but not capped
[InlineData(100, 0.83)] // Max investment = very strong but not 100%
public void DodgeChance_FollowsDiminishingReturns(int defense, double expected)
{
    var actual = defense / (double)(defense + 20);
    actual.Should().BeApproximately(expected, 0.01);
}
```

## Related Patterns

- **Deterministic Random Testing:** Use seeded RNG to test expected dodge rates
- **Game Systems Balance Planning:** Use this formula in feature proposals
- **Stat breakpoint spreadsheets:** Plot X/Y values to visualize curve before implementing

## Real-World Examples

**League of Legends:** Armor formula `100 / (100 + armor)` for damage reduction  
**World of Warcraft:** Diminishing returns on crowd control duration  
**Path of Exile:** Resistance caps and curse effectiveness scaling  
**Diablo series:** Attack speed / cast speed formulas
