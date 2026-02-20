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

### 2026-02-20: v2 Testing Strategy Proposal

**Authored comprehensive testing strategy for v2:**

**Framework:** xUnit 2.6+ selected over NUnit (better async, cleaner isolation, industry standard)

**Project Structure:** Dungnz.Tests project with Unit/, Integration/, Fixtures/, TestHelpers/ organization

**Mocking Strategy:**
- IDisplayService extraction required (Hill owns) → enables headless testing with Moq
- Random injection into CombatEngine/LootTable → enables deterministic testing with seeded Random
- FluentAssertions for readable test assertions

**Coverage Strategy (Must-Have Tests):**
- CombatEngine unit tests: player kills enemy, enemy kills player, flee success/failure, flee failure death, level-up mid-combat, minimum damage rule, loot distribution
- LootTable unit tests: deterministic drops, first-match-wins, gold ranges, empty loot table
- InventoryManager unit tests: TakeItem, UseItem (consumable/weapon/armor), empty inventory edge cases
- CommandParser unit tests: all commands, shortcuts, case insensitivity, argument parsing
- Player unit tests (post-encapsulation): TakeDamage, Heal, ModifyStat validation, state constraints
- DungeonGenerator integration: 5x4 grid, connectivity (BFS), spawn/exit rooms, boss placement, bidirectional exits
- GameLoop integration: combat triggers, dead enemy cleanup, exit blocking, win/lose conditions

**Manual Testing Only:** DisplayService formatting (visual validation more efficient than brittle assertions), end-to-end gameplay balance

**Edge Case Inventory (v1 Learnings + v2 Risks):**
- Dead enemy cleanup regression test (room.Enemy == null after Won)
- Flee penalty death edge case (CombatResult.PlayerDied if flee damage kills)
- Flee-and-return behavior (enemy HP persists — accepted, but must consider for save/load)
- Item stacking exploit monitoring (infinite weapon/armor equips)
- Save/load JSON deserialization safety (if added): validate HP <= MaxHP, no negative stats
- Player encapsulation validation: TakeDamage/Heal input validation, stat caps enforced

**Quality Gates:**
- Build gates: 0 test failures allowed, 85-100% coverage on high-risk systems (combat/loot/inventory/player)
- Manual gates: Architecture review (no Console.Write, no hardcoded Random, Player encapsulation), edge case validation, playtest validation
- Rejection criteria: Test failures, coverage drops, architectural violations, edge case regressions, null refs, soft-lock bugs

**C# Testing Patterns:**
- Arrange-Act-Assert structure (always)
- Theory + InlineData for parameterized tests (e.g., CommandParser input variations)
- Moq IDisplayService verification for critical messages ("LEVEL UP!", "You defeated")
- System.Text.Json fixtures for save/load testing (if feature added)
- Deterministic Random pattern: `new Random(42)` in tests, `rng ?? new Random()` in production
- Builder pattern for test data factories (PlayerBuilder, EnemyBuilder)
- FluentAssertions for readability: `result.Should().Be(CombatResult.Won)`

**Timeline Estimate:** 1.5-2 sprints
- Phase 1: Infrastructure (IDisplayService extraction, Random injection, test project setup) — 1 week
- Phase 2: Unit tests (CombatEngine, LootTable, InventoryManager, CommandParser) — 1 week
- Phase 3: Integration tests (DungeonGenerator, GameLoop) — 3-4 days
- Phase 4: Regression suite (WI-10 edge cases) — 2 days
- Phase 5: CI/CD integration (GitHub Actions, dotnet test) — 1 day

**Blocking Dependencies:**
- Hill: IDisplayService interface extraction (1-2 hours)
- Barton: CombatEngine/LootTable Random injection (2-3 hours)
- Hill: Player encapsulation refactor (2-3 hours)

**Patterns Identified:**
- Dependency injection for testability: inject Random, IDisplayService, future IFileSystem/IClock
- Interface extraction for external dependencies: Console → IDisplayService, File → IFileSystem
- Optional parameter pattern for production defaults: `Random? rng = null` → `_rng = rng ?? new Random()`

**File:** `.ai-team/decisions/inbox/romanoff-v2-testing-strategy.md` (18KB comprehensive strategy document)

## 📌 Team Update (2026-02-20): Phase Gates and Testing Infrastructure
**From Scribe** — Team decisions merged; phase dependencies confirmed:
- **Phase Structure:** Coulson's phase gates linked to Romanoff's test coverage thresholds. Testing work (Phase 1) unblocks architecture work (Phase 2) which unblocks features (Phase 3).
- **Infrastructure Priority:** IDisplayService extraction (Hill + Coulson) and Random injection (Coulson + Hill) are prerequisites for test harness. Romanoff can begin xUnit setup once these interfaces/injections in place.
- **Coverage Targets:** 70%+ code coverage required Phase 1→2 gate. CombatEngine, LootTable, InventoryManager = 100% coverage must-haves per testing strategy.

**Impact on Romanoff:** Confirmed xUnit + Moq + FluentAssertions stack (vs NUnit). Mocking strategy (IDisplayService + seeded Random) documented and agreed across team.
