# Barton — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Learnings

### Phase 2 - Combat Systems Implementation (WI-6, WI-7, WI-8)

**Files Created:**
- `Engine/ICombatEngine.cs` — interface contract for combat
- `Engine/CombatEngine.cs` — turn-based combat implementation
- `Systems/InventoryManager.cs` — item pickup and use mechanics
- `Models/LootTable.cs` — replaced stub with full probability-based loot system
- `Systems/Enemies/Goblin.cs` — 20 HP, 8 ATK, 2 DEF, 15 XP, drops 2-8 gold
- `Systems/Enemies/Skeleton.cs` — 30 HP, 12 ATK, 5 DEF, 25 XP, drops bone/sword
- `Systems/Enemies/Troll.cs` — 60 HP, 10 ATK, 8 DEF, 40 XP, drops troll hide
- `Systems/Enemies/DarkKnight.cs` — 45 HP, 18 ATK, 12 DEF, 55 XP, drops dark blade/armor
- `Systems/Enemies/DungeonBoss.cs` — 100 HP, 22 ATK, 15 DEF, 100 XP, guaranteed boss key

**CombatEngine Design:**
- Turn-based: player attacks first, then enemy retaliates
- Damage formula: `Math.Max(1, attacker.Attack - defender.Defense)`
- Flee mechanic: 50% success rate; failure results in enemy free hit
- XP/Leveling: 100 XP per level, awards +2 ATK, +1 DEF, +10 MaxHP, full heal
- Loot drops: awarded on enemy death via LootTable.RollDrop()
- Returns CombatResult enum: Won, Fled, PlayerDied

**LootTable Configuration:**
- Each enemy initializes its own LootTable in constructor
- Supports min/max gold ranges
- Item drops use probability (0.0-1.0), first matching drop wins
- Boss drops guaranteed with 1.0 chance

**Inventory System:**
- TakeItem: case-insensitive partial match for item names
- UseItem: handles Consumable (heal), Weapon (ATK boost), Armor (DEF boost)
- Equipment permanently increases stats and is consumed
- Uses DisplayService for all output (no direct Console calls)

**Build Status:**
- Cannot verify build (dotnet not in PATH)
- All files created successfully, committed to git
- Respected Hill's existing Model contracts exactly
- Integrated real enemies into EnemyFactory.cs (replaced stubs)
- Program.cs already wired to use CombatEngine (Hill's work)
