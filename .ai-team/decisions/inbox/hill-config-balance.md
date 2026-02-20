### 2026-02-20: Config-Driven Game Balance

**By:** Hill  
**What:** Moved all enemy and item stats from hardcoded C# to external JSON config files (Data/enemy-stats.json, Data/item-stats.json) loaded at startup via EnemyConfig/ItemConfig static classes with validation.

**Why:**  
- **Iteration speed:** Game balance tuning (HP, attack values, loot tables) without recompilation
- **Designer-friendly:** JSON is human-readable and editable by non-programmers
- **Version control:** Balance changes tracked in git separately from code
- **Validation:** Load-time checks with descriptive exceptions catch config errors before gameplay

**Pattern:**  
Static loader classes with Load(path) methods returning config DTOs (EnemyStats, ItemStats records). Entity constructors accept nullable config parameters with hardcoded fallbacks. Program.cs loads configs at startup, crashes with clear error if invalid.

**Trade-offs:**  
- Adds file I/O dependency at startup (negligible overhead)
- Config must be copied to output directory (.csproj configuration required)
- Two sources of truth during migration (config + hardcoded defaults)
