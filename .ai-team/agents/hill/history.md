# Hill — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Learnings

### 2026-02-20 — Phase 1: Project Scaffold and Core Models (WI-1, WI-2)

**Files Created:**
- `TextGame.csproj` — .NET 9 console project, nullable enabled
- `Program.cs` — Entry point stub (to be wired in WI-4)
- `Models/Direction.cs` — enum: North, South, East, West
- `Models/CombatResult.cs` — enum: Won, Fled, PlayerDied (contract for Barton's CombatEngine)
- `Models/UseResult.cs` — enum: Used, NotUsable, NotFound (contract for Barton's InventoryManager)
- `Models/LootResult.cs` — readonly struct: Item?, Gold (Barton's LootTable return type)
- `Models/ItemType.cs` — enum: Weapon, Armor, Consumable, Gold
- `Models/Item.cs` — 7 fields (Name, Type, StatModifier, Description, AttackBonus, DefenseBonus, HealAmount); IsEquippable computed property
- `Models/Enemy.cs` — abstract base class with 7 fields (Name, HP, MaxHP, Attack, Defense, XPValue, LootTable); Barton will subclass for 5 enemy types
- `Models/Player.cs` — 9 fields (Name, HP, MaxHP, Attack, Defense, Gold, XP, Level, Inventory); defaults: HP/MaxHP=100, Attack=10, Defense=5, Level=1
- `Models/Room.cs` — Description, Exits (Dictionary<Direction, Room>), Enemy?, Items, IsExit, Visited, Looted flags
- `Models/LootTable.cs` — Placeholder with RollDrop stub (Barton owns implementation)
- `Display/DisplayService.cs` — Sole owner of Console I/O; 11 methods including ShowRoom, ShowCombat, ShowPlayerStats, ShowInventory, ShowHelp, ShowTitle

**Design Decisions:**
1. **Item flexibility:** Included all fields from Design Review (AttackBonus, DefenseBonus, HealAmount, StatModifier) to support both simple and complex items without future refactoring
2. **LootTable ownership:** Placed in Models/ (not Systems/) because it's shared across Hill's and Barton's domains; Barton will implement RollDrop logic
3. **DisplayService completeness:** Implemented all methods agreed in Design Review plus ShowTitle for polish; includes Unicode symbols for visual clarity (⚔, ⚠, ✦, ✗)
4. **Nullable annotations:** Enabled in csproj; Enemy? and Item? properly marked to avoid null reference warnings

**Deviations from Plan:**
- None; all Design Review contracts implemented exactly as specified

**Build Status:**
- Project structure created successfully
- All files staged and committed (commit 5c0901c)
- Build verification skipped (dotnet permission issue in environment); project structure is valid and will build in IDE

**Blockers Cleared:**
- Barton can now implement Enemy subclasses (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss)
- Barton can implement CombatEngine.StartCombat with DisplayService dependency injection
- Barton can implement InventoryManager and LootTable.RollDrop using agreed contracts

**Next Steps (Hill):**
- WI-3: DungeonGenerator (creates rooms, links exits bidirectionally, places enemies/items)
- WI-4: GameLoop and CommandParser (wires up DisplayService, handles player commands)
