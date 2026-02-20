### 2026-02-20: Two-Pass Serialization for Circular Object Graphs
**By:** Hill  
**What:** Implemented Guid-based two-pass serialization to handle circular Room.Exits references in save/load system.  
**Why:**  
- Room graph contains circular references (Room.Exits points to other Rooms which point back)
- Standard JSON serialization fails on circular references
- Two-pass approach: serialize Guids instead of object references, then rehydrate object graph from Guids
- BFS traversal ensures all reachable rooms are captured
- Guid.NewGuid() provides unique IDs without central ID management
- System.Text.Json (native) preferred over Newtonsoft.Json (external dependency)

**Pattern:**
1. **Serialize:** BFS collect all rooms → RoomSaveData DTOs replace `Room` refs with `Guid` refs → JSON
2. **Deserialize:** JSON → create all Room objects → wire Exits by resolving Guids through dictionary

**Applicability:** Any domain with circular object graphs needing persistence (e.g., enemy spawn graphs, quest dependency trees, dialogue trees)

### 2026-02-20: AppData Save Location for User Data
**By:** Hill  
**What:** Saves stored in `Environment.GetFolderPath(SpecialFolder.ApplicationData)/Dungnz/saves/`  
**Why:**  
- Follows .NET conventions for user-specific application data
- Cross-platform (Windows: %APPDATA%, Linux: ~/.config, macOS: ~/Library/Application Support)
- Survives application upgrades and re-installs
- No admin privileges required

### 2026-02-20: Specific Exception Handling for User-Facing Errors
**By:** Hill  
**What:** Save/load handlers catch `FileNotFoundException`, `InvalidDataException`, and generic `Exception` separately  
**Why:**  
- Different error types warrant different user messages
- FileNotFoundException → "not found, use LIST"
- InvalidDataException → "corrupt save"  
- Generic Exception → "unexpected error"  
- Prevents cryptic .NET stack traces in game console
- Guides users to resolution (e.g., suggests LIST command for typos)

**Pattern:** Catch specific exceptions first, then generic Exception as fallback, always with user-friendly messages.
