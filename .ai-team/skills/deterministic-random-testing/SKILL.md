# Deterministic Random Testing with Injectable RNG

**Confidence:** medium  
**Source:** earned  
**Domain:** testing, randomness, game-testing

**Updated:** 2026-02-20 — Pattern validated in Dungnz v2 architecture. Upgraded to IRandom interface pattern for cleaner test doubles and explicit contract.  

## Pattern

When testing systems with randomness (combat rolls, loot drops, procedural generation), inject a seeded `Random` instance to make tests deterministic and reproducible.

```csharp
public class CombatEngine
{
    private readonly Random _rng;
    
    public CombatEngine(IDisplayService display, Random? rng = null)
    {
        _display = display;
        _rng = rng ?? new Random(); // Production uses unseeded Random
    }
    
    public CombatResult Flee(Player player, Enemy enemy)
    {
        if (_rng.NextDouble() < 0.5) // 50% flee chance
        {
            return CombatResult.Fled;
        }
        // Failed flee, enemy attacks
        player.HP -= CalculateDamage(enemy, player);
        return player.HP <= 0 ? CombatResult.PlayerDied : CombatResult.Fled;
    }
}
```

## Usage

**Production code (non-deterministic):**
```csharp
var combat = new CombatEngine(display); // Uses new Random() internally
var result = combat.Flee(player, enemy); // Unpredictable outcome
```

**Test code (deterministic):**
```csharp
[Fact]
public void Flee_WithSeed42_Succeeds()
{
    // Arrange
    var rng = new Random(42); // Seed 42 → first NextDouble() returns 0.31
    var combat = new CombatEngine(mockDisplay, rng);
    var player = new Player { HP = 50 };
    var enemy = new Enemy { Attack = 10 };

    // Act
    var result = combat.Flee(player, enemy);

    // Assert
    result.Should().Be(CombatResult.Fled);
    player.HP.Should().Be(50); // No damage taken
}

[Fact]
public void Flee_WithSeed99_Fails()
{
    // Arrange
    var rng = new Random(99); // Seed 99 → first NextDouble() returns 0.87
    var combat = new CombatEngine(mockDisplay, rng);
    var player = new Player { HP = 50, Defense = 5 };
    var enemy = new Enemy { Attack = 10 };

    // Act
    var result = combat.Flee(player, enemy);

    // Assert
    result.Should().Be(CombatResult.Fled);
    player.HP.Should().BeLessThan(50); // Took damage from failed flee
}
```

## Rationale

- **Reproducibility:** Same seed → same outcome, every time
- **Test stability:** Tests never flake due to randomness
- **Edge case testing:** Test both success and failure branches by choosing appropriate seeds
- **Debugging:** If test fails, can reproduce exact scenario with same seed
- **CI/CD friendly:** No random test failures in build pipelines

## Finding the Right Seeds

**Strategy 1: Trial and error**
```csharp
// Run this locally to find seeds that produce desired outcomes
for (int seed = 0; seed < 100; seed++)
{
    var rng = new Random(seed);
    var roll = rng.NextDouble();
    if (roll < 0.5)
        Console.WriteLine($"Seed {seed} → {roll:F2} (SUCCESS)");
    else
        Console.WriteLine($"Seed {seed} → {roll:F2} (FAILURE)");
}
```

**Strategy 2: Wrapper class with forced outcomes**
```csharp
public class FakeRandom : Random
{
    private readonly double _forcedValue;
    
    public FakeRandom(double forcedValue) => _forcedValue = forcedValue;
    
    public override double NextDouble() => _forcedValue;
}

// Test usage:
var alwaysSucceed = new FakeRandom(0.2); // Always < 0.5
var alwaysFail = new FakeRandom(0.8);    // Always >= 0.5
```

## Common Use Cases

### Combat Systems
```csharp
[Fact]
public void Attack_WithCriticalHit_DealsBonusDamage()
{
    var rng = new Random(seedThatProducesCrit);
    var combat = new CombatEngine(display, rng);
    
    var result = combat.Attack(player, enemy);
    
    result.Damage.Should().BeGreaterThan(normalDamage);
    result.IsCritical.Should().BeTrue();
}
```

### Loot Systems
```csharp
[Fact]
public void RollDrop_WithSeed42_DropsRareSword()
{
    var rng = new Random(42);
    var lootTable = new LootTable(rng);
    lootTable.AddDrop(rareSword, chance: 0.01); // 1% drop
    
    var result = lootTable.RollDrop(enemy);
    
    result.Item.Should().Be(rareSword);
}

[Fact]
public void RollDrop_WithSeed99_DropsNothing()
{
    var rng = new Random(99);
    var lootTable = new LootTable(rng);
    lootTable.AddDrop(rareSword, chance: 0.01);
    
    var result = lootTable.RollDrop(enemy);
    
    result.Item.Should().BeNull();
}
```

### Procedural Generation
```csharp
[Fact]
public void GenerateDungeon_WithSeed1337_ProducesSameLayout()
{
    var rng1 = new Random(1337);
    var rng2 = new Random(1337);
    
    var dungeon1 = DungeonGenerator.Generate(rng1);
    var dungeon2 = DungeonGenerator.Generate(rng2);
    
    dungeon1.Should().BeEquivalentTo(dungeon2); // Identical layout
}
```

## When to Use Seeded vs. Unseeded Random

✅ **Seeded (deterministic) for:**
- Unit tests (assert exact outcomes)
- Integration tests (reproducible scenarios)
- Debugging (recreate exact bug conditions)
- Playtesting (reproducible playthroughs for balance testing)

❌ **Unseeded (non-deterministic) for:**
- Production gameplay (players expect randomness)
- Exploratory tests (fuzz testing, chaos testing)
- Load tests (varied inputs expose more edge cases)

## Anti-Patterns to Avoid

❌ **Hardcoded Random inside tested class:**
```csharp
public class CombatEngine
{
    private readonly Random _rng = new Random(); // ❌ Can't control in tests
}
```

❌ **Global static Random:**
```csharp
public static class GameRandom
{
    public static Random Instance = new Random(); // ❌ Shared state breaks isolation
}
```

❌ **Resetting seed mid-test:**
```csharp
var rng = new Random(42);
combat.Attack(player, enemy); // Uses first roll
rng = new Random(42); // ❌ Don't reset, sequence is important
combat.Attack(player, enemy); // This should use second roll, not first again
```

❌ **Over-specifying outcomes:**
```csharp
[Fact]
public void RollDrop_Always_DropsExactly5Gold()
{
    var result = lootTable.RollDrop(enemy);
    result.Gold.Should().Be(5); // ❌ Brittle if gold range is 1-10
}

// Better: Test valid range
[Fact]
public void RollDrop_ReturnsGold_WithinConfiguredRange()
{
    var result = lootTable.RollDrop(enemy);
    result.Gold.Should().BeInRange(minGold, maxGold);
}
```

## Advanced: Sequence Testing

Test multiple rolls with a single seed to verify sequences:

```csharp
[Fact]
public void CombatSequence_WithSeed42_ProducesExpectedFlow()
{
    var rng = new Random(42);
    var combat = new CombatEngine(display, rng);
    
    // Seed 42 produces this sequence: 0.31, 0.87, 0.12, 0.64...
    
    // First attack: roll 0.31 → hit
    combat.Attack(player, enemy).IsHit.Should().BeTrue();
    
    // Second attack: roll 0.87 → miss
    combat.Attack(player, enemy).IsHit.Should().BeFalse();
    
    // Flee attempt: roll 0.12 → success
    combat.Flee(player, enemy).Should().Be(CombatResult.Fled);
}
```

## Related Patterns

- **Dependency injection:** Inject `Random` via constructor (enables seeding)
- **Builder pattern:** Test data factories can use seeded Random for reproducible test data
- **Property-based testing:** Use unseeded Random + many iterations to find edge cases
