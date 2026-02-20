# Romanoff — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Learnings

### 2026-02-20: WI-10 Code Review (Feature-Complete Quality Pass)
**Reviewed:** All 23 source files (Program.cs, Models, Engine, Systems, Display)
**Scope:** Bug hunting, logic errors, architectural violations, edge cases

**Issues Found (7 total):**
1. Program.cs:8-9 — Direct Console.Write/ReadLine violating DisplayService-only architecture
2. GameLoop.cs:30 — Direct Console.Write("> ") prompt violation
3. CombatEngine.cs:22 — Direct Console.Write("[A]ttack or [F]lee? ") prompt violation
4. GameLoop.cs:123-130 — Dead enemies not cleared from room after combat (HP=0 but Enemy!=null persists)
5. GameLoop.cs:109 — Win condition checked dead boss via HP<=0 instead of Enemy==null
6. Architectural violation: Multiple classes bypassing DisplayService for output
7. Logic bug: Dead enemy objects remain in rooms, could cause re-combat or confusion

**Fixes Applied (all 7 fixed):**
- Added `ShowCommandPrompt()` to DisplayService
- Added `ShowCombatPrompt()` to DisplayService  
- Added `ReadPlayerName()` to DisplayService
- Updated Program.cs to use DisplayService.ReadPlayerName()
- Updated GameLoop to use DisplayService.ShowCommandPrompt()
- Updated CombatEngine to use DisplayService.ShowCombatPrompt()
- Fixed GameLoop to set `_currentRoom.Enemy = null` after `CombatResult.Won`
- Simplified win condition to check `_currentRoom.Enemy == null` (no dead enemy object edge case)

**Patterns to Watch:**
- Console I/O violations: Always route through DisplayService
- Dead enemy cleanup: Any system that mutates enemy state must handle lifecycle
- Null vs zero HP: Prefer explicit null for "defeated" state over HP<=0 checks
- Edge case: What happens if player flees combat then returns? (Currently enemy HP persists — acceptable behavior)

**Verdict:** APPROVED
- All architectural violations corrected
- Logic bugs fixed (dead enemy cleanup)
- No null safety issues found (LootTable = null! is safe — always initialized in enemy constructors)
- Edge cases handled: empty inventory USE, GO with no arg, TAKE with no items, combat with dead enemy
- Code is ready to ship

### 2026-02-20: Retrospective Ceremony & v2 Planning Decisions

**Team Update:** Retrospective ceremony identified 3 refactoring decisions for v2:

1. **DisplayService Interface Extraction** — Extract IDisplayService interface to enable mock test implementations. Romanoff will build test harness on this interface.

2. **Player Encapsulation Refactor** — Hill refactoring Player model with validation. Improves state safety and enables future extensions (save/load, analytics).

3. **Test Infrastructure Required** — Romanoff leads test infrastructure buildout: xUnit/NUnit harness, injected Random for deterministic combat/loot testing, integration with CI/CD. Blocks v2 feature work. Effort: 1-2 sprints.

**Participants:** Coulson (facilitator), Hill, Barton, Romanoff

**Impact:** Romanoff owns test infrastructure as blocking work for v2. High priority: regression risk without tests is unacceptable for future changes.
