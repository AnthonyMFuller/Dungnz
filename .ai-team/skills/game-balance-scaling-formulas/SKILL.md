# SKILL: Game Balance Scaling Formulas

**Pattern:** Progressive stat scaling using simple linear formulas for game balance

**When to use:**
- Scaling enemy difficulty based on player progression
- Item stat scaling across tiers/levels
- Experience/reward scaling
- Any game mechanic that needs gradual power progression

**Formula Pattern:**
```
scalar = baseValue + (level - startLevel) * coefficient
finalStat = baseStat * scalar
```

**Example (Enemy Scaling):**
```csharp
// 12% increase per level above 1
float scalar = 1.0f + (playerLevel - 1) * 0.12f;
int scaledHP = (int)Math.Round(baseHP * scalar);
int scaledAttack = (int)Math.Round(baseAttack * scalar);
```

**Benefits:**
- Predictable, tunable progression
- Easy to understand and communicate
- Avoids exponential runaway
- Simple to adjust (change coefficient)

**Tuning Guidelines:**
- 10-15% per level: Moderate scaling, good for long-term progression
- 20-30% per level: Aggressive scaling, shorter power curves
- Use Math.Round() for consistent integer conversion
- Test at early (level 1), mid (level 5), and late (level 10) game

**Anti-patterns:**
- Exponential formulas (e.g., `level^2`) — hard to balance, can break game
- Magic numbers without context — document why you chose your coefficient
- Scaling too many stats independently — creates unpredictable power spikes

**Variations:**
- Diminishing returns: `scalar = 1.0f + Math.Log(level) * coefficient`
- Threshold-based: Different coefficients for level ranges
- Cap-based: `scalar = Math.Min(maxScalar, 1.0f + (level - 1) * coefficient)`

**Confidence:** low
**Source:** earned
**Tags:** game-design, balance, scaling, progression
