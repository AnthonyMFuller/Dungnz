# Tier 1 Architecture Improvements

**Date:** 2026-02-20  
**Architect:** Coulson  
**Issues:** #755 (HP Encapsulation), #773 (Structured Logging)  
**PRs:** #771, #776

---

## Decision 1: Enforce HP Encapsulation with Private Setter

### Context
Player.HP had a public setter allowing direct bypasses of the TakeDamage/Heal event system. This led to bypass bugs being fixed three times with no architectural enforcement:
- Direct HP assignment bypasses OnHealthChanged event
- Bypasses validation (negative amount checks, min/max clamping)
- Makes HP changes unauditable for debugging

### Decision
Changed Player.HP to use a private setter: `public int HP { get; private set; }`

Added internal helper method:
```csharp
internal void SetHPDirect(int value)
{
    var oldHP = HP;
    HP = Math.Clamp(value, 0, MaxHP);
    if (HP != oldHP)
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
}
```

### Rationale
- **Compile-time enforcement:** Private setter prevents accidental bypasses
- **Event system mandatory:** All HP changes now fire OnHealthChanged
- **Test support:** SetHPDirect provides escape hatch for test setup without exposing public setter
- **Special mechanics:** Resurrection and initialization can use SetHPDirect for direct setting

### Implementation Details
- CombatEngine: Soul Harvest uses `Heal(5)`, shrine uses `FortifyMaxHP(5)`
- IntroSequence: Class selection uses `SetHPDirect(MaxHP)` for initialization
- PassiveEffectProcessor: Aegis and Phoenix use `SetHPDirect` for revival
- Tests: All test HP assignments replaced with `SetHPDirect`

### Alternatives Considered
- **Public SetHP(int value) method:** Rejected because it's too easy to misuse and doesn't enforce event system
- **Reflection for tests:** Rejected because it's brittle and hides intent
- **Test-only constructor:** Rejected because it complicates factory patterns

### Impact
- ✅ Eliminates HP bypass bug class entirely
- ✅ Makes HP changes auditable
- ✅ Enforces event system architecture
- ✅ No public API changes (internal architecture only)
- ⚠️ Breaking change for tests (requires SetHPDirect adoption)

---

## Decision 2: Implement Structured Logging with Microsoft.Extensions.Logging

### Context
Application had zero logging infrastructure:
- Crashes had no paper trail for debugging
- HP bypass bugs had no audit trail
- No visibility into production behavior
- No performance monitoring capability

### Decision
Implement structured logging using Microsoft.Extensions.Logging with Serilog file backend:
- Log directory: `%APPDATA%/Dungnz/Logs/`
- File pattern: `dungnz-YYYYMMDD.log` (daily rolling)
- Injection pattern: `ILogger<T>` via constructor
- Optional logging: NullLogger fallback for backward compatibility

### Rationale
- **Microsoft.Extensions.Logging:** Industry-standard abstraction, swappable backends
- **Serilog:** Battle-tested file sink with rolling support
- **Structured logs:** Queryable properties (e.g., `{HP}`, `{EnemyName}`) for analysis
- **Daily rolling:** Automatic log management without manual cleanup
- **Optional injection:** Doesn't break existing code, gradual adoption

### Implementation Details

**Logging Levels:**
- Debug: Room navigation, low-importance events
- Information: Combat events, save/load operations, significant state changes
- Warning: Critical player states (HP < 20%), unusual conditions
- Error: Exceptions, load failures, system errors

**Logged Events:**
- Room navigation: `LogDebug("Player entered room at {RoomId}", ...)`
- Combat lifecycle: Start, end with result
- Low HP warnings: `LogWarning("Player HP critically low: {HP}/{MaxHP}", ...)`
- Save/load operations: Success and failure paths
- Exception catches: Full exception details with context

**Configuration (Program.cs):**
```csharp
var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dungnz", "Logs");
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(Path.Combine(logDir, "dungnz-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger<GameLoop>();
```

### Alternatives Considered
- **Console logging only:** Rejected because it's not persistent and clutters gameplay output
- **Custom logger implementation:** Rejected because it reinvents the wheel, adds maintenance burden
- **Static Log.Logger:** Rejected because it's not testable, violates DI pattern
- **NLog/Serilog directly:** Chose Microsoft.Extensions.Logging abstraction for flexibility

### Impact
- ✅ Full audit trail for debugging
- ✅ Production monitoring capability
- ✅ Performance analysis via log analysis
- ✅ Future bypass bugs have event history
- ✅ Backward compatible (optional ILogger)
- ⚠️ Adds file I/O overhead (minimal, async by default)
- ⚠️ Log file growth requires monitoring (daily rolling mitigates)

---

## Cross-Cutting Architectural Patterns Established

### Pattern 1: Encapsulation-First for Critical State
**Rule:** Domain model properties with business rules MUST use private setters + public methods.  
**Applied to:** Player.HP (TakeDamage/Heal), Player.ComboPoints (AddComboPoints/SpendComboPoints)  
**Extends to:** Future work on Player.MaxHP, Player.Mana, Player.Gold (all need private setters + methods)

### Pattern 2: Internal Escape Hatches for Framework Needs
**Rule:** When tests or special mechanics need direct property access, provide internal helper method with clear documentation.  
**Applied to:** Player.SetHPDirect (test setup, resurrection)  
**Pattern:** `internal void SetXDirect(T value)` with event firing included  
**Extends to:** Future SetManaDirect, SetGoldDirect if needed

### Pattern 3: Optional Dependency Injection
**Rule:** New dependencies injected via constructor with nullable parameter + NullLogger fallback for backward compatibility.  
**Applied to:** ILogger<GameLoop> in GameLoop constructor  
**Pattern:** `ILogger<T>? logger = null` → `_logger = logger ?? NullLogger<T>.Instance`  
**Extends to:** Future IMetrics, ITelemetry, IAnalytics dependencies

### Pattern 4: Structured Logging Properties
**Rule:** Log messages use structured properties for queryability, not string interpolation.  
**Anti-pattern:** `_logger.LogInformation($"Player HP: {player.HP}")`  
**Correct:** `_logger.LogInformation("Player HP: {HP}", player.HP)`  
**Benefit:** Log aggregation tools can query on HP value, not parse strings

---

## Future Recommendations

### Extend HP Encapsulation Pattern
- **Player.MaxHP:** Should use private setter, accessed via `FortifyMaxHP(int)` only
- **Player.Mana:** Should use private setter, accessed via `RestoreMana(int)` / `SpendMana(int)` only
- **Player.Gold:** Should use private setter, accessed via `AddGold(int)` / `SpendGold(int)` only
- **Player.XP:** Already has `AddXP(int)` method, should make setter private

### Extend Structured Logging Coverage
- **CombatEngine:** Log ability usage, status effect applications, critical hits, boss phase transitions
- **SaveSystem:** Log save/load performance metrics, data size, migration events
- **StatusEffectManager:** Log effect applications, expirations, stacking logic
- **EquipmentManager:** Log equipment changes, stat bonuses applied/removed

### Add Log Levels Configuration
- **appsettings.json:** Allow runtime log level changes (Debug in dev, Warning in prod)
- **IConfiguration integration:** Bind Serilog MinimumLevel from config
- **Per-namespace levels:** Fine-tune verbosity (e.g., Debug for CombatEngine, Warning for SaveSystem)

### Performance Monitoring via Logging
- **Combat duration:** Log combat start/end timestamps for performance analysis
- **Save/load times:** Log operation durations to identify SaveSystem bottlenecks
- **Memory usage:** Periodic log of working set size for leak detection

---

## Team Sign-off

**Coulson (Architect):** ✅ Approved — both patterns align with v3 architecture goals  
**Hill (Implementation):** ✅ Patterns followed in HP encapsulation work  
**Barton (Systems):** ✅ Structured logging unblocks debugging bypass bugs  
**Romanoff (Testing):** ✅ SetHPDirect pattern simplifies test setup  

**Merge Status:** Awaiting PR review (#771, #776)
