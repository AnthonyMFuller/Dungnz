# Dungnz v2 Architecture Plan

**Author:** Coulson (Lead)  
**Date:** 2026-02-20  
**Status:** Approved for Execution

---

## Executive Summary

This plan addresses critical technical debt identified in the v1 retrospective and establishes the foundation for sustainable v2 feature development. The current codebase suffers from three architectural blockers:

1. **Zero test coverage** — High regression risk, refactoring paralysis
2. **Tight coupling to Console** — Blocks automated testing and alternative UIs
3. **Leaky encapsulation** — Player model allows invalid state mutations

**Bottom line:** v2 features require a stable, testable foundation. This plan delivers that in 4 phases over ~78 engineering hours.

---

## Phase 0: Critical Refactoring (14.5 hours)

**Must complete before any v2 feature work.** These changes eliminate architectural violations and enable testability.

### Objectives
- Extract display interface for testability
- Encapsulate Player model to prevent invalid state
- Inject Random for deterministic testing
- Complete architectural violation fixes from retrospective

### Work Items

| ID | Title | Owner | Hours | Dependencies |
|----|-------|-------|-------|--------------|
| R1 | Extract IDisplayService Interface | Hill | 2.0 | None |
| R2 | Implement TestDisplayService | Hill | 1.5 | R1 |
| R3 | Refactor Player Encapsulation | Hill | 3.0 | None |
| R4 | Make Random Injectable | Barton | 2.5 | None |
| R5 | Fix CombatEngine Input Coupling | Barton | 1.0 | R1 |
| R6 | Update GameLoop for Player Encapsulation | Hill | 1.5 | R3 |
| R7 | Update InventoryManager for Player Encapsulation | Barton | 1.0 | R3 |
| R8 | Update CombatEngine for Player Encapsulation | Barton | 2.0 | R3,R4,R5 |

### Key Decisions

#### R1: IDisplayService Interface Extraction
**Current Problem:** DisplayService is concrete, tightly coupled to Console. Blocks mocking in tests.

**Solution:**
```csharp
public interface IDisplayService
{
    void ShowTitle();
    void ShowRoom(Room room);
    void ShowCombatStatus(Player player, Enemy enemy);
    void ShowMessage(string message);
    string ReadPlayerName();
    string ReadCombatInput(); // NEW - moved from CombatEngine
    // ... all existing methods
}

public class ConsoleDisplayService : IDisplayService
{
    // Existing DisplayService implementation
}

public class TestDisplayService : IDisplayService
{
    public List<string> Messages { get; } = new();
    public void ShowMessage(string message) => Messages.Add(message);
    // ... capture all output for test assertions
}
```

**Impact:** All systems (GameLoop, CombatEngine, InventoryManager) depend on `IDisplayService` interface, not concrete class.

**Why This Matters:** Enables mocking in unit tests. Foundation for alternative UIs (GUI, web, Discord bot).

---

#### R3: Player Model Encapsulation
**Current Problem:** Public setters allow invalid state:
```csharp
player.HP = -100;          // Should cap at 0
player.HP = 999;           // Should cap at MaxHP
player.MaxHP = 0;          // Should prevent
player.Attack = -50;       // Nonsensical
```

**Solution:**
```csharp
public class Player
{
    public string Name { get; init; } = string.Empty;
    public int HP { get; private set; } = 100;
    public int MaxHP { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    public int Defense { get; private set; } = 5;
    public int Gold { get; private set; }
    public int XP { get; private set; }
    public int Level { get; private set; } = 1;
    public List<Item> Inventory { get; } = new();

    // Encapsulated mutations with validation
    public void TakeDamage(int amount)
    {
        HP = Math.Max(0, HP - Math.Max(0, amount));
    }

    public void Heal(int amount)
    {
        HP = Math.Min(MaxHP, HP + Math.Max(0, amount));
    }

    public void AddGold(int amount) => Gold = Math.Max(0, Gold + amount);
    
    public void AddXP(int amount) => XP = Math.Max(0, XP + amount);

    public void ModifyAttack(int delta) => Attack = Math.Max(0, Attack + delta);
    
    public void ModifyDefense(int delta) => Defense = Math.Max(0, Defense + delta);

    public void SetLevel(int level, int maxHPBonus, int attackBonus, int defenseBonus)
    {
        Level = level;
        MaxHP += maxHPBonus;
        Attack += attackBonus;
        Defense += defenseBonus;
        HP = MaxHP; // Full heal on level up
    }
}
```

**Breaking Changes:** All direct property mutations must refactor:
- `player.HP -= damage` → `player.TakeDamage(damage)`
- `player.Gold += loot` → `player.AddGold(loot)`
- `player.Attack += bonus` → `player.ModifyAttack(bonus)`

**Why This Matters:** 
- Prevents save/load corruption (invalid state can't be serialized)
- Foundation for multiplayer (server validates all mutations)
- Enables analytics (hook events in mutation methods)
- Required for F2 (equipment slots with stat recalculation)

---

#### R4: Injectable Random
**Current Problem:** Systems create own Random instances:
```csharp
// CombatEngine.cs line 8
private readonly Random _rng = new();

// LootTable.cs line 12
_rng = rng ?? new Random();
```

**Blocks deterministic testing:** Can't control RNG outcomes to test flee success/failure, loot drops, dungeon generation.

**Solution:**
```csharp
public interface IRandom
{
    int Next(int minValue, int maxValue);
    double NextDouble();
}

public class SystemRandom : IRandom
{
    private readonly Random _rng = new();
    public int Next(int min, int max) => _rng.Next(min, max);
    public double NextDouble() => _rng.NextDouble();
}

public class TestRandom : IRandom
{
    private readonly Queue<double> _values = new();
    public void EnqueueDouble(double value) => _values.Enqueue(value);
    public double NextDouble() => _values.Count > 0 ? _values.Dequeue() : 0.5;
    public int Next(int min, int max) => min; // Or enqueue ints
}
```

**Update Constructors:**
```csharp
public CombatEngine(IDisplayService display, IRandom random) { ... }
public LootTable(IRandom random, int minGold, int maxGold) { ... }
public DungeonGenerator(IRandom random) { ... }
```

**Why This Matters:** Enables tests like:
```csharp
var testRng = new TestRandom();
testRng.EnqueueDouble(0.4); // Flee success (< 0.5)
var result = combat.RunCombat(player, enemy);
Assert.Equal(CombatResult.Fled, result);
```

---

### Acceptance Criteria
- [ ] All Console.Write/ReadLine calls routed through IDisplayService
- [ ] Player model has private setters, public methods for all mutations
- [ ] CombatEngine, LootTable, DungeonGenerator accept IRandom in constructor
- [ ] TestDisplayService captures output for assertions
- [ ] TestRandom enables deterministic test scenarios
- [ ] All existing game functionality still works (manual playtest)

---

## Phase 1: Test Infrastructure (16.5 hours)

**Now that systems are testable, build the safety net.**

### Objectives
- Add xUnit test framework
- Achieve >70% code coverage on core systems
- Document testing patterns for team

### Work Items

| ID | Title | Owner | Hours | Dependencies |
|----|-------|-------|-------|--------------|
| T1 | Add xUnit Test Project | Romanoff | 1.0 | R1-R8 |
| T2 | Write Player Model Tests | Romanoff | 3.0 | T1 |
| T3 | Write CombatEngine Tests | Romanoff | 4.0 | T1 |
| T4 | Write InventoryManager Tests | Romanoff | 2.5 | T1 |
| T5 | Write LootTable Tests | Romanoff | 2.0 | T1 |
| T6 | Write DungeonGenerator Tests | Romanoff | 2.5 | T1 |
| T7 | Document Testing Patterns | Romanoff | 1.5 | T2-T6 |

### Test Coverage Priorities

**Critical (must have):**
- Player: TakeDamage, Heal, AddXP (edge cases: negative values, over-capping)
- CombatEngine: Player win, enemy win, flee success/failure, player death during flee
- InventoryManager: TakeItem, UseItem for all item types
- LootTable: Deterministic drops with seeded random

**Important (should have):**
- DungeonGenerator: Grid connectivity, spawn rates
- CommandParser: All command aliases, invalid input

**Nice to have:**
- GameLoop integration tests (full command sequences)
- Enemy subclass specific behavior

### Example Test Structure
```csharp
public class PlayerTests
{
    [Fact]
    public void TakeDamage_CapsAtZero()
    {
        var player = new Player { HP = 10 };
        player.TakeDamage(50);
        Assert.Equal(0, player.HP);
    }

    [Fact]
    public void Heal_CapsAtMaxHP()
    {
        var player = new Player { HP = 50, MaxHP = 100 };
        player.Heal(100);
        Assert.Equal(100, player.HP);
    }
}

public class CombatEngineTests
{
    [Fact]
    public void RunCombat_FleeSuccess_Returns_Fled()
    {
        var display = new TestDisplayService();
        var rng = new TestRandom();
        rng.EnqueueDouble(0.4); // < 0.5 = flee success
        rng.EnqueueString("F"); // Player chooses flee
        
        var combat = new CombatEngine(display, rng);
        var player = new Player();
        var enemy = new Goblin();
        
        var result = combat.RunCombat(player, enemy);
        
        Assert.Equal(CombatResult.Fled, result);
        Assert.Contains("fled successfully", display.Messages);
    }
}
```

### Acceptance Criteria
- [ ] Dungnz.Tests project created with xUnit
- [ ] All Phase 0 refactors have test coverage
- [ ] Tests run in CI/CD (if applicable)
- [ ] TESTING.md documents how to write and run tests

---

## Phase 2: Architecture Improvements (22 hours)

**Optional but recommended.** These changes enable advanced v2 features and improve maintainability.

### Objectives
- Separate game state from presentation logic
- Enable save/load functionality
- Introduce event system for extensibility
- Make systems configurable without recompilation

### Work Items

| ID | Title | Owner | Hours | Dependencies |
|----|-------|-------|-------|--------------|
| A1 | Introduce Game State Model | Hill | 3.0 | R3,R6 |
| A2 | Extract IGamePersistence Interface | Hill | 2.0 | A1 |
| A3 | Implement JsonGamePersistence | Hill | 4.0 | A2 |
| A4 | Add Event System | Barton | 3.0 | R8 |
| A5 | Refactor Enemy Factory Pattern | Barton | 2.5 | R4 |
| A6 | Introduce Configuration System | Hill | 3.5 | None |
| A7 | Separate Engine from UI | Hill | 4.0 | R1,R5 |

### Key Decisions

#### A1: Game State Model
**Current Problem:** GameLoop tightly couples state (Player, Room, dungeon graph) with presentation logic.

**Solution:**
```csharp
public class GameState
{
    public Player Player { get; init; }
    public Room CurrentRoom { get; set; }
    public Dictionary<(int x, int y), Room> DungeonMap { get; init; }
    public int TurnCount { get; set; }
    public DateTime StartedAt { get; init; }
    
    // Serialization support
    public int SchemaVersion { get; init; } = 1;
}
```

**Benefits:**
- Save/load becomes trivial (serialize GameState)
- Replay systems (save state each turn)
- State inspection for debugging
- Multiplayer foundation (sync GameState across clients)

---

#### A4: Event System
**Purpose:** Decouple systems; enable achievements, analytics, UI updates.

```csharp
public static class GameEvents
{
    public static event Action<Player, int>? PlayerDamaged;
    public static event Action<Enemy, Player>? EnemyDefeated;
    public static event Action<Item, Player>? ItemPickedUp;
    public static event Action<Player, int>? LevelUp;
    
    public static void RaisePlayerDamaged(Player p, int dmg) => PlayerDamaged?.Invoke(p, dmg);
    // ...
}
```

**Usage:**
```csharp
// In CombatEngine
player.TakeDamage(damage);
GameEvents.RaisePlayerDamaged(player, damage);

// In future AchievementSystem
GameEvents.EnemyDefeated += (enemy, player) => {
    if (enemy is DungeonBoss) UnlockAchievement("Boss Slayer");
};
```

---

#### A6: Configuration System
**Purpose:** Balance tuning without recompilation.

```csharp
// appsettings.json
{
  "Combat": {
    "FleeChance": 0.5,
    "DamageFormula": "Attacker.Attack - Defender.Defense"
  },
  "Loot": {
    "GoldMultiplier": 1.0,
    "RareDropBonus": 0.1
  },
  "Dungeon": {
    "GridWidth": 5,
    "GridHeight": 4,
    "EnemySpawnRate": 0.6,
    "ItemSpawnRate": 0.3
  }
}
```

**Load in Program.cs:**
```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var gameConfig = config.Get<GameConfig>();
```

---

#### A7: Separate Engine from UI
**Current Problem:** GameLoop directly reads Console input (line 31: `Console.ReadLine()`).

**Solution:**
```csharp
public interface IInputService
{
    string ReadCommand();
    string ReadLine();
}

public class ConsoleInputService : IInputService
{
    public string ReadCommand()
    {
        Console.Write("> ");
        return Console.ReadLine() ?? string.Empty;
    }
}

public class TestInputService : IInputService
{
    private readonly Queue<string> _inputs = new();
    public void EnqueueCommand(string cmd) => _inputs.Enqueue(cmd);
    public string ReadCommand() => _inputs.Count > 0 ? _inputs.Dequeue() : "quit";
}
```

**Update GameLoop:**
```csharp
public GameLoop(IDisplayService display, IInputService input, ICombatEngine combat) { ... }

public void Run(Player player, Room startRoom)
{
    while (true)
    {
        var input = _input.ReadCommand(); // No Console dependency
        var cmd = CommandParser.Parse(input);
        // ...
    }
}
```

**Benefits:**
- GameLoop becomes pure state machine (no I/O)
- Enables headless mode (automated testing, bot integration)
- Foundation for GUI/web UI (inject different input service)

---

### Acceptance Criteria
- [ ] GameState model holds all mutable game data
- [ ] IGamePersistence interface defined, JsonGamePersistence implemented
- [ ] GameEvents fires on key actions (damage, defeat, loot, level)
- [ ] appsettings.json drives game balance parameters
- [ ] GameLoop has no direct Console dependencies

---

## Phase 3: Feature Development (25 hours)

**Post-refactor features enabled by new architecture.**

### Work Items

| ID | Title | Owner | Hours | Dependencies |
|----|-------|-------|-------|--------------|
| F1 | Implement Save/Load Commands | Hill | 3.0 | A1,A2,A3 |
| F2 | Add Equipment Slots | Barton | 4.0 | R3,R7 |
| F3 | Add Status Effects | Barton | 5.0 | R8,A4 |
| F4 | Add Permadeath Mode | Hill | 2.5 | F1 |
| F5 | Add Multi-Floor Dungeons | Hill | 6.0 | A1,A5 |
| F6 | Add Item Crafting | Barton | 4.5 | F2 |

### Feature Highlights

#### F1: Save/Load
```csharp
// New commands
SAVE [filename]  // Saves current GameState to Saves/filename.json
LOAD [filename]  // Loads GameState, resumes game
```

**Technical:** Serialize GameState via JsonGamePersistence. Handle edge cases (file not found, corrupt JSON, version mismatch).

---

#### F2: Equipment Slots
**Current:** Items consumed when equipped, stats permanently increase.  
**New:** Equipment slots (weapon, armor, ring). Items stay in inventory when equipped. Unequip to swap gear.

**Benefits:** Strategic depth (swap gear for situations), loot becomes meaningful (collect set bonuses).

---

#### F3: Status Effects
**Mechanics:**
- Poison: -3 HP per turn for 3 turns
- Burning: -5 HP per turn for 2 turns
- Regenerating: +2 HP per turn for 5 turns
- Stunned: Skip next turn

**Applied by:** Special enemy attacks, consumable items (antidote potion).

---

#### F5: Multi-Floor Dungeons
**Current:** Single 5x4 grid (20 rooms).  
**New:** 5 floors, 4x3 grid per floor (60 rooms total). Stairs down/up between floors. Boss on floor 5.

**Difficulty Scaling:** Each floor increases enemy HP/ATK by 20%.

---

#### F6: Item Crafting
```csharp
// Example recipe
new CraftingRecipe
{
    Inputs = [new Item("Iron Ore", 3), new Item("Wood", 2)],
    Output = new Item("Iron Sword", ItemType.Weapon, attackBonus: 5)
}
```

**Mechanics:** Find CraftingStation in rooms. CRAFT command opens recipe menu. Consume inputs, produce output.

---

### Acceptance Criteria
- [ ] All features have unit tests
- [ ] Features tested in integration (full playthrough)
- [ ] No regressions to v1 functionality

---

## Dependency Graph

```
Phase 0 (Refactoring)
├─ R1: IDisplayService ────┬─────────────────┐
│                          │                 │
├─ R2: TestDisplayService  │                 │
│                          │                 │
├─ R3: Player Encapsulation┼─────┬───────────┤
│                          │     │           │
├─ R4: Injectable Random ──┼─────┤           │
│                          │     │           │
├─ R5: CombatEngine Input ─┘     │           │
│                                │           │
├─ R6: GameLoop Update ──────────┘           │
├─ R7: InventoryManager Update ──────────────┘
└─ R8: CombatEngine Update ──────────────────┐
                                             │
Phase 1 (Testing)                            │
├─ T1: xUnit Project ────────────────────────┘
├─ T2: Player Tests
├─ T3: CombatEngine Tests
├─ T4: InventoryManager Tests
├─ T5: LootTable Tests
├─ T6: DungeonGenerator Tests
└─ T7: Testing Docs
         │
         ├────────────────────────────────────┐
         │                                    │
Phase 2 (Architecture)                       │
├─ A1: GameState Model ──────┐               │
│                            │               │
├─ A2: IGamePersistence ─────┤               │
│                            │               │
├─ A3: JsonGamePersistence ──┤               │
│                            │               │
├─ A4: Event System ─────────┤               │
│                            │               │
├─ A5: Enemy Factory ────────┤               │
│                            │               │
├─ A6: Configuration ────────┤               │
│                            │               │
└─ A7: Separate Engine/UI ───┤               │
                             │               │
Phase 3 (Features)           │               │
├─ F1: Save/Load ────────────┘               │
├─ F2: Equipment Slots ──────────────────────┘
├─ F3: Status Effects
├─ F4: Permadeath Mode
├─ F5: Multi-Floor Dungeons
└─ F6: Item Crafting
```

---

## Work Allocation by Agent

| Agent | Phase 0 | Phase 1 | Phase 2 | Phase 3 | Total Hours |
|-------|---------|---------|---------|---------|-------------|
| Hill | 8.0 | — | 16.5 | 11.5 | 36.0 |
| Barton | 6.5 | — | 5.5 | 13.5 | 25.5 |
| Romanoff | — | 16.5 | — | — | 16.5 |
| **Total** | **14.5** | **16.5** | **22.0** | **25.0** | **78.0** |

---

## Success Metrics

### Phase 0 (Refactoring)
- [ ] Zero architectural violations (all Console I/O through IDisplayService)
- [ ] Player model passes property-based tests (no invalid state reachable)
- [ ] CombatEngine/LootTable testable with deterministic random

### Phase 1 (Testing)
- [ ] ≥70% code coverage on Models, Engine, Systems namespaces
- [ ] All critical paths have test cases (combat win/lose, flee, loot, level-up)
- [ ] CI/CD pipeline runs tests on every commit

### Phase 2 (Architecture)
- [ ] GameState serializable to/from JSON
- [ ] Game balance tunable via appsettings.json (no code changes)
- [ ] GameLoop has zero Console dependencies (pure state machine)

### Phase 3 (Features)
- [ ] Save/load preserves exact game state (verified via test)
- [ ] Equipment slots enable strategic gear swapping
- [ ] Multi-floor dungeons increase playtime 3x

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Player encapsulation breaks existing code | High | High | Comprehensive test suite before refactor. Mechanically replace all mutations. |
| Test framework setup delays Phase 1 | Medium | Medium | Use xUnit (team familiar). Romanoff owns setup. |
| Save/load corrupts GameState | Medium | High | JSON schema versioning. Validate deserialized state. Backup saves before load. |
| Phase 2 features creep into Phase 0 | Medium | Medium | Strict phase gates. No feature work until tests pass. |
| Team capacity (vacations, other projects) | Low | Medium | Phase 0 is critical path; Hill and Barton prioritize. Phase 2/3 can stretch. |

---

## Technical Debt Resolved

This plan eliminates all high-priority debt from the retrospective:

✅ **No automated test coverage** → Phase 1 delivers ≥70% coverage  
✅ **Player lacks encapsulation** → R3 adds private setters + validation  
✅ **RNG not injectable** → R4 introduces IRandom interface  
✅ **DisplayService coupled to Console** → R1 extracts IDisplayService  
✅ **CombatEngine architectural violation** → R5 moves input to IDisplayService  

**New debt introduced:** None (all refactors improve architecture).

---

## Next Steps

1. **Coulson:** Present plan to team, get approval
2. **Hill & Barton:** Review R1-R8 work items, clarify any ambiguities
3. **Romanoff:** Review T1-T7 test strategy, confirm coverage targets
4. **Scribe:** Merge this plan into `.ai-team/plans/` directory
5. **ALL:** Begin Phase 0 work items (Hill: R1, R2, R3, R6 | Barton: R4, R5, R7, R8)

**Ceremony schedule:**
- Day 1: Phase 0 kickoff (design review for R1, R3, R4)
- Day 5: Phase 0 code review + Phase 1 kickoff
- Day 10: Phase 1 review + decision on Phase 2 scope
- Day 15: Phase 2 review + Phase 3 feature prioritization

---

## Appendix: C# Pattern Recommendations

### Dependency Injection
**Use constructor injection for required dependencies:**
```csharp
public CombatEngine(IDisplayService display, IRandom random) { ... }
```

**Avoid service locator pattern.** Makes dependencies implicit, harder to test.

### Interface Design
**Extract interfaces when:**
- Need to mock for testing (IDisplayService, IRandom)
- Multiple implementations likely (IGamePersistence, IInputService)
- Crossing architectural boundaries (Engine → Display, Engine → Systems)

**Don't extract interfaces for:**
- Pure data classes (Player, Enemy, Item, Room)
- Single implementation with no test/extension need (CommandParser)

### Property Patterns
**Use init-only properties for immutable data:**
```csharp
public string Name { get; init; }
public DateTime CreatedAt { get; init; }
```

**Use private setters + methods for mutable state:**
```csharp
public int HP { get; private set; }
public void TakeDamage(int amount) => HP = Math.Max(0, HP - amount);
```

### Error Handling
**Use result enums over exceptions for expected failures:**
```csharp
public enum UseResult { Used, NotFound, NotUsable }
public UseResult UseItem(Player player, string itemName) { ... }
```

**Reserve exceptions for truly exceptional cases:**
- File I/O errors (save/load)
- Deserialization failures
- Invalid configuration

### Testing
**Name tests: MethodName_Scenario_ExpectedOutcome**
```csharp
[Fact]
public void TakeDamage_DamageExceedsHP_CapsAtZero() { ... }
```

**One assertion per test (when feasible).** Makes failures easy to diagnose.

---

**End of Plan**
