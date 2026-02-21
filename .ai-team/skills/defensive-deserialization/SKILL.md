# Skill: Defensive Deserialization Pattern

**Confidence:** medium  
**Source:** earned  
**Date:** 2026-02-20  
**Author:** Romanoff  

## Problem

JSON deserialization trusts external data implicitly, bypassing domain model encapsulation. Corrupted files, manual edits, or version mismatches can load invalid state: HP > MaxHP, negative stats, null required fields. Leads to runtime bugs, exploits, and soft-locks.

Common in save systems, config loaders, API clients, any System.Text.Json or Newtonsoft deserialization.

## Pattern: Validate After Deserialize

### Anti-Pattern (Trusting Deserialization)

```csharp
public static Player LoadPlayer(string path)
{
    var json = File.ReadAllText(path);
    var player = JsonSerializer.Deserialize<Player>(json);
    return player; // ❌ No validation — trusts JSON implicitly
}

// Corrupted JSON: { "HP": 999, "MaxHP": 100, "Level": -1 }
// Result: Player with HP > MaxHP, negative level — breaks game logic
```

### Defensive Pattern (Validation Layer)

```csharp
public static Player LoadPlayer(string path)
{
    var json = File.ReadAllText(path);
    var player = JsonSerializer.Deserialize<Player>(json);
    
    if (player == null)
        throw new InvalidDataException("Player data is null or empty");
    
    // Validate invariants
    if (player.HP < 0 || player.HP > player.MaxHP)
        throw new InvalidDataException($"Invalid HP: {player.HP}/{player.MaxHP}");
    
    if (player.MaxHP <= 0 || player.Attack <= 0 || player.Defense < 0)
        throw new InvalidDataException("Invalid stats: MaxHP, Attack, or Defense out of bounds");
    
    if (player.Level < 1)
        throw new InvalidDataException($"Invalid level: {player.Level}");
    
    if (player.Mana < 0 || player.Mana > player.MaxMana || player.MaxMana < 0)
        throw new InvalidDataException($"Invalid mana: {player.Mana}/{player.MaxMana}");
    
    if (player.Gold < 0 || player.XP < 0)
        throw new InvalidDataException("Gold or XP cannot be negative");
    
    return player;
}
```

## Key Techniques

### 1. Null Checks

```csharp
if (player == null)
    throw new InvalidDataException("Deserialized object is null");
```

JSON can deserialize to `null` if file is empty or malformed.

### 2. Bounds Validation

```csharp
if (player.HP < 0 || player.HP > player.MaxHP)
    throw new InvalidDataException($"HP out of bounds: {player.HP}/{player.MaxHP}");
```

Enforce domain invariants: current value ≤ max value, non-negative stats.

### 3. Required Field Validation

```csharp
if (string.IsNullOrWhiteSpace(player.Name))
    throw new InvalidDataException("Player name is required");
```

Ensure required fields are present and non-empty.

### 4. Cross-Field Validation

```csharp
if (player.Gold < 0)
    throw new InvalidDataException("Gold cannot be negative");

if (player.XP < ExpRequiredForLevel(player.Level))
    throw new InvalidDataException($"XP too low for Level {player.Level}");
```

Validate relationships between fields (XP thresholds, equipment level requirements).

### 5. Collection Validation

```csharp
if (player.Inventory == null)
    throw new InvalidDataException("Inventory cannot be null");

foreach (var item in player.Inventory)
{
    if (item == null)
        throw new InvalidDataException("Inventory contains null item");
    if (string.IsNullOrWhiteSpace(item.Name))
        throw new InvalidDataException("Item missing name");
}
```

Validate collections aren't null and elements are valid.

## Example: SaveSystem with Validation

```csharp
public static GameState LoadGame(string saveName)
{
    if (string.IsNullOrWhiteSpace(saveName))
        throw new ArgumentException("Save name cannot be empty");

    var fileName = Path.Combine(SaveDirectory, $"{saveName}.json");
    
    if (!File.Exists(fileName))
        throw new FileNotFoundException($"Save file '{saveName}' not found");

    try
    {
        var json = File.ReadAllText(fileName);
        var saveData = JsonSerializer.Deserialize<SaveData>(json);
        
        if (saveData == null)
            throw new InvalidDataException("Save file is corrupt or empty");

        // ✅ Validate player state after deserialization
        ValidatePlayer(saveData.Player);
        
        // ✅ Validate room graph
        if (saveData.Rooms == null || saveData.Rooms.Count == 0)
            throw new InvalidDataException("Save file has no rooms");
        
        // Reconstruct room graph...
        var roomDict = ReconstructRooms(saveData.Rooms);
        var currentRoom = roomDict[saveData.CurrentRoomId];
        
        return new GameState(saveData.Player, currentRoom);
    }
    catch (JsonException ex)
    {
        throw new InvalidDataException($"Failed to load save file '{saveName}': corrupt data", ex);
    }
}

private static void ValidatePlayer(Player player)
{
    if (player == null)
        throw new InvalidDataException("Player data is missing");
    
    if (player.HP < 0 || player.HP > player.MaxHP)
        throw new InvalidDataException($"Invalid HP: {player.HP}/{player.MaxHP}");
    
    if (player.MaxHP <= 0)
        throw new InvalidDataException($"MaxHP must be positive: {player.MaxHP}");
    
    if (player.Attack <= 0 || player.Defense < 0)
        throw new InvalidDataException($"Invalid stats: Attack={player.Attack}, Defense={player.Defense}");
    
    if (player.Level < 1)
        throw new InvalidDataException($"Invalid level: {player.Level}");
    
    if (player.Mana < 0 || player.Mana > player.MaxMana || player.MaxMana < 0)
        throw new InvalidDataException($"Invalid mana: {player.Mana}/{player.MaxMana}");
    
    if (player.Gold < 0 || player.XP < 0)
        throw new InvalidDataException("Gold and XP must be non-negative");
}
```

## Benefits

1. **Security:** Prevents exploit editing (God-mode HP, infinite gold)
2. **Robustness:** Fails fast on corruption (not silent undefined behavior)
3. **Version Safety:** Detects schema mismatches (missing fields, wrong types)
4. **Clear Errors:** User-friendly error messages vs cryptic NullReferenceException
5. **Auditability:** Validation layer is single source of truth for constraints

## Example: Config Loader with Validation

```csharp
public static List<ItemStats> LoadItems(string path)
{
    if (!File.Exists(path))
        throw new FileNotFoundException($"Item config not found: {path}");

    try
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<ItemConfigData>(json);

        if (config == null || config.Items.Count == 0)
            throw new InvalidDataException($"Item config is empty or invalid: {path}");

        // ✅ Validate each item
        foreach (var item in config.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidDataException("Item missing required field: Name");
            
            if (string.IsNullOrWhiteSpace(item.Type))
                throw new InvalidDataException($"Item '{item.Name}' missing Type");
            
            if (!Enum.TryParse<ItemType>(item.Type, ignoreCase: true, out _))
                throw new InvalidDataException($"Item '{item.Name}' has invalid Type: {item.Type}");
            
            if (item.HealAmount < 0 || item.AttackBonus < 0 || item.DefenseBonus < 0)
                throw new InvalidDataException($"Item '{item.Name}' has negative stat values");
        }

        return config.Items;
    }
    catch (JsonException ex)
    {
        throw new InvalidDataException($"Failed to parse config '{path}': {ex.Message}", ex);
    }
}
```

## Applicability

- **Save systems:** Player state, game world, progress
- **Config loaders:** Enemy stats, item definitions, game balance
- **API clients:** REST responses, webhooks, external data
- **File imports:** CSV, XML, JSON from user uploads
- **Any deserialization of untrusted or mutable data**

## Common Validation Rules

| Domain | Validation |
|--------|-----------|
| HP/Mana | `0 <= Current <= Max`, `Max > 0` |
| Stats | `Attack > 0`, `Defense >= 0`, `Level >= 1` |
| Currency | `Gold >= 0`, `XP >= 0` |
| Collections | `!= null`, no null elements, size limits |
| Strings | `!IsNullOrWhiteSpace` for required fields |
| Enums | `Enum.IsDefined()` or `TryParse()` |
| IDs/GUIDs | `!= Guid.Empty`, exists in reference data |

## Trade-offs

- **Pro:** Prevents invalid state, clear error messages, security
- **Con:** More code (validation layer adds LOC)
- **Con:** Duplicate validation if domain model also validates (but deserialization bypasses private setters!)

## Why Domain Model Validation Isn't Enough

```csharp
public class Player
{
    public int HP { get; private set; }
    public int MaxHP { get; private set; }
    
    public void TakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentException(...);
        HP = Math.Max(0, HP - amount); // ✅ Validated
    }
}

// JSON deserialization BYPASSES private setters!
var player = JsonSerializer.Deserialize<Player>(json);
// HP and MaxHP set directly via reflection — validation never runs!
```

**Key Insight:** Deserialization uses reflection to set fields/properties, bypassing validation logic in setters and methods. Must validate explicitly after deserialization.

## Migration Strategy

1. Identify all deserialization call sites (SaveSystem, config loaders, API clients)
2. Add `ValidateX()` helper method for each domain model
3. Call validation immediately after `Deserialize<T>()`
4. Wrap in try-catch for `JsonException` → `InvalidDataException`
5. Add unit tests for corrupt JSON edge cases

## Testing Strategy

```csharp
[Fact]
public void LoadGame_InvalidHP_ThrowsInvalidDataException()
{
    var json = @"{ ""Player"": { ""HP"": 999, ""MaxHP"": 100 }, ... }";
    File.WriteAllText("test.json", json);
    
    var ex = Assert.Throws<InvalidDataException>(() => SaveSystem.LoadGame("test"));
    Assert.Contains("Invalid HP", ex.Message);
}

[Fact]
public void LoadGame_NegativeLevel_ThrowsInvalidDataException()
{
    var json = @"{ ""Player"": { ""Level"": -1, ... }, ... }";
    File.WriteAllText("test.json", json);
    
    var ex = Assert.Throws<InvalidDataException>(() => SaveSystem.LoadGame("test"));
    Assert.Contains("Invalid level", ex.Message);
}
```

Test every validation rule with corrupt JSON fixtures.

## Related Patterns

- **Domain Model Encapsulation:** Defensive deserialization complements private setters (both enforce invariants at different layers)
- **Fail-Fast Principle:** Validate early, fail with clear errors
- **Input Validation:** Same pattern applies to user input, API requests
- **Builder Pattern:** Validation in builder's `Build()` method

## Tags

deserialization, validation, security, robustness, json, save-systems, config-loaders, defensive-coding

## Real-World Bug Example (Dungnz SaveSystem)

**Bug:** SaveSystem.LoadGame() deserializes Player without validation. Manual save file edit: `"HP": 9999, "MaxHP": 100` loads successfully, breaks combat math (player unkillable).

**Fix:** Add `ValidatePlayer()` call after deserialization:
```csharp
var saveData = JsonSerializer.Deserialize<SaveData>(json);
if (saveData == null)
    throw new InvalidDataException("Save file is corrupt or empty");

ValidatePlayer(saveData.Player); // ✅ Added validation layer

var roomDict = ReconstructRooms(saveData.Rooms);
return new GameState(saveData.Player, roomDict[saveData.CurrentRoomId]);
```

**Impact:** Prevents God-mode exploit, achievement cheating (Glass Cannon: win with HP < 10 by setting MaxHP=10000), save corruption soft-locks.
