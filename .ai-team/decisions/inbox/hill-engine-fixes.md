# Decision: Enemy AI Registry + CommandHandlerBase Infrastructure

**Date:** 2026-03-08  
**Author:** Hill (C# Dev — P1 Gameplay Focus)  
**PR:** #1260  
**Branch:** `squad/1225-1226-engine-fixes`  
**Issues:** #1225, #1226  

## Context

Two P1 engine bugs were identified:
1. **#1226**: EnemyAIRegistry only registered 2 of 29 enemy types, causing 27 enemies to return null from `GetAI()` (no AI behavior)
2. **#1225**: No base class existed to enforce the ShowRoom() convention established during the Retro D2 menu audit

## Decisions Made

### 1. Register All Enemy Types with Default AI Fallback

**What:** Created `DefaultEnemyAI` and registered all 38 enemy types in `EnemyAIRegistry`.

**Why:**
- 27 enemy types had no AI behavior (GetAI returned null)
- Not every enemy needs specialized tactics — many can share a simple "attack every turn" strategy
- Avoids null reference exceptions and simplifies CombatEngine logic

**How:**
- Created `DefaultEnemyAI`: simple IEnemyAI implementation that returns `Attack` action every turn
- Registered 38 types:
  - 2 with specialized AI (Goblin, Skeleton)
  - 24 regular enemies with DefaultEnemyAI
  - 12 boss variants with DefaultEnemyAI
- Used singleton instances to avoid allocating 38 AI objects

**Trade-offs:**
- ✅ All enemies now have AI behavior
- ✅ No null checks needed in CombatEngine
- ✅ Easy to migrate specific enemies to specialized AI later (just change registry entry)
- ⚠️ Bosses currently use DefaultEnemyAI (simple attack) — specialized boss AI is future work

### 2. Created CommandHandlerBase with Template Method Pattern

**What:** Abstract base class that enforces ShowRoom() call after command execution.

**Why:**
- March 2026 menu audit established convention: "every handler that sets content panel must call ShowRoom()"
- 19+ handlers manually call ShowRoom() — easy to forget, inconsistent
- Need infrastructure to enforce this convention without explicit calls

**How:**
- `CommandHandlerBase` implements `ICommandHandler.Handle()`
- Template method: calls `HandleCore()` (abstract), then conditionally calls `ShowRoom()`
- `ShouldRefreshRoom()` virtual method (default true) — override to skip ShowRoom for informational commands

**Proof of Concept:**
- Migrated 3 handlers: StatsCommandHandler, MapCommandHandler, HelpCommandHandler
- Removed explicit ShowRoom() calls from those handlers
- Full migration of remaining 16 handlers is a follow-up task

**Trade-offs:**
- ✅ Enforces ShowRoom convention via type system
- ✅ Reduces boilerplate in handlers
- ✅ Opt-out pattern (ShouldRefreshRoom) handles edge cases (SAVE, LEADERBOARD)
- ⚠️ Breaking change for existing handlers (requires migration)
- ⚠️ Deferred full migration to keep PR scope small

## Rationale

### Why DefaultEnemyAI vs Individual AI Classes?

- **Time:** Creating 24+ specialized AI classes was out of scope for this bug fix
- **Simplicity:** Most enemies can use simple attack strategy as placeholder
- **Incremental:** Easy to add specialized AI later without changing registry pattern

### Why Template Method vs Event/Hook Pattern?

- **Simplicity:** Template method is straightforward — subclass implements HandleCore(), base class handles ShowRoom()
- **Type Safety:** Compile-time enforcement (abstract method must be implemented)
- **Consistent:** Matches existing C# patterns in codebase (e.g., Enemy base class with virtual properties)

### Why Partial Migration?

- **Risk:** Full migration of 19 handlers in one PR increases risk of breaking existing behavior
- **Scope:** Issue #1225 asks for infrastructure, not full migration
- **Testing:** 3 handlers provide proof of concept — remaining handlers migrate in follow-up PR with full test coverage

## Future Work

1. **Boss AI Specialization**: Create specialized AI for bosses (phase abilities, enrage, summons)
2. **Full Handler Migration**: Migrate remaining 16 handlers to CommandHandlerBase
3. **Enemy AI Diversity**: Add specialized AI for enemies with unique mechanics (e.g., VampireLord lifesteal, ManaLeech mana drain)

## Files Changed

- `Dungnz.Engine/DefaultEnemyAI.cs` (new)
- `Dungnz.Engine/EnemyAIRegistry.cs` (38 entries, was 2)
- `Dungnz.Engine/Commands/CommandHandlerBase.cs` (new)
- `Dungnz.Engine/Commands/StatsCommandHandler.cs` (migrated)
- `Dungnz.Engine/Commands/MapCommandHandler.cs` (migrated)
- `Dungnz.Engine/Commands/HelpCommandHandler.cs` (migrated)
