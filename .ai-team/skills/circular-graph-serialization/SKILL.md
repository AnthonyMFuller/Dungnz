# Skill: Circular Object Graph Serialization

**Confidence:** low  
**Source:** earned  
**Date:** 2026-02-20  
**Author:** Hill

## Problem

Serializing object graphs with circular references (e.g., Room A → Room B → Room A) breaks standard serializers. Common in dungeon/map systems, bidirectional relationships, graph structures.

## Pattern: Hydration/Dehydration with Temporary IDs

### Strategy

1. **Dehydration (Serialize):** Traverse graph, assign temporary Guid to each node, replace object references with IDs
2. **Serialization:** Serialize DTOs with IDs (no circular refs)
3. **Deserialization:** Deserialize DTOs
4. **Hydration (Deserialize):** Rebuild object graph from IDs in two passes

### Implementation (C# / System.Text.Json)

```csharp
// Runtime model (has circular references)
public class Room
{
    public Dictionary<Direction, Room> Exits { get; init; } = new();
    public string Description { get; set; } = string.Empty;
}

// Serialization DTO (no circular references, uses IDs)
public class RoomSaveData
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public Dictionary<Direction, Guid> ExitIds { get; init; } = []; // IDs not objects
}

// Dehydration: Runtime → SaveData
public static Dictionary<Guid, RoomSaveData> DehydrateGraph(Room start)
{
    // BFS traversal to assign GUIDs
    var roomMap = new Dictionary<Room, Guid>();
    var queue = new Queue<Room>();
    queue.Enqueue(start);
    roomMap[start] = Guid.NewGuid();
    
    while (queue.Count > 0)
    {
        var room = queue.Dequeue();
        foreach (var exit in room.Exits.Values)
        {
            if (!roomMap.ContainsKey(exit))
            {
                roomMap[exit] = Guid.NewGuid();
                queue.Enqueue(exit);
            }
        }
    }
    
    // Convert to SaveData
    return roomMap.ToDictionary(
        kvp => kvp.Value, // Guid
        kvp => new RoomSaveData
        {
            Id = kvp.Value,
            Description = kvp.Key.Description,
            ExitIds = kvp.Key.Exits.ToDictionary(
                e => e.Key,
                e => roomMap[e.Value] // Replace Room ref with Guid
            )
        }
    );
}

// Hydration: SaveData → Runtime
public static Room HydrateGraph(Dictionary<Guid, RoomSaveData> saveData, Guid startId)
{
    var roomMap = new Dictionary<Guid, Room>();
    
    // First pass: create all Room instances
    foreach (var (id, data) in saveData)
    {
        roomMap[id] = new Room
        {
            Description = data.Description
        };
    }
    
    // Second pass: wire up exits (now all Rooms exist)
    foreach (var (id, data) in saveData)
    {
        var room = roomMap[id];
        foreach (var (direction, targetId) in data.ExitIds)
        {
            room.Exits[direction] = roomMap[targetId];
        }
    }
    
    return roomMap[startId];
}

// Usage
var saveData = DehydrateGraph(startRoom);
var json = JsonSerializer.Serialize(saveData);
// ... save to file ...

// ... load from file ...
var loaded = JsonSerializer.Deserialize<Dictionary<Guid, RoomSaveData>>(json);
var reconstructed = HydrateGraph(loaded, startRoomId);
```

## Key Techniques

- **Temporary IDs:** Guid only exists during serialization, not in runtime model
- **Two-pass hydration:** Create nodes first, wire edges second
- **Graph traversal:** BFS to discover all reachable nodes
- **DTO pattern:** Separate SaveData classes keep runtime models clean

## Applicability

- Dungeon/map systems with bidirectional connections
- Entity relationship graphs (parent/child, friend networks)
- State machines with transition graphs
- Scene graphs in games
- Any system where `A → B → A` patterns exist

## Alternatives Considered

- **ReferenceHandler.Preserve:** JSON serializer can preserve refs, but bloats output and is fragile
- **[JsonIgnore] on back-references:** Loses information (can't reconstruct bidirectional links)
- **Store IDs permanently:** Pollutes runtime model with serialization concerns

## Trade-offs

- **Pro:** Clean separation of runtime vs persistence concerns
- **Pro:** Works with any serializer (JSON, binary, XML)
- **Con:** Requires custom hydration/dehydration code
- **Con:** Two-pass hydration has O(N) space overhead (dictionary)

## Related Patterns

- Memento pattern (for save/restore)
- Builder pattern (for complex hydration)
- Repository pattern (for save/load abstraction)

## Tags

serialization, graph, circular-references, persistence, c#, json
