# Retrospective — 2026-02-20
**Facilitator:** Coulson
**Participants:** Coulson, Hill, Barton, Romanoff, Scribe
**Context:** Team retrospective after completing initial TextGame development (v1 shipped, code review passed)

---

## What Went Well

### Process Excellence
- **Design Review ceremony prevented rework** — Pre-build interface contracts (ICombatEngine, DisplayService, InventoryManager) enabled true parallel development between Hill and Barton with zero integration bugs
- **Code review caught issues before shipping** — Romanoff's WI-10 review identified and fixed 7 architectural violations and logic bugs (DisplayService bypasses, dead enemy cleanup)

### Technical Wins
- **Clean architecture with clear boundaries** — Model ownership (Hill: Player/Enemy base/Item/Room; Barton: Enemy subclasses, systems) made parallel work seamless
- **LootTable simplicity** — First-match-wins drop logic kept code simple and loot drops predictable
- **Contract-first design paid dividends** — All agreed interfaces worked on first integration with no mismatches

---

## What Could Be Improved

### Critical Issues (Blocking Future Work)
1. **No automated test coverage** — Zero unit tests, no integration test harness. Regression risk is high for any refactoring or feature additions. Combat logic, inventory edge cases, loot drops, and dungeon generation are all untestable without refactoring for testability.

2. **Player model lacks encapsulation** — All Player properties have public setters (HP, Attack, Defense, etc.). Nothing prevents invalid state (`HP = -100`, `MaxHP = 0`). Blocks save/load, multiplayer, and modding use cases.

3. **RNG not injectable** — CombatEngine and LootTable each create their own `Random` instances. Causes unpredictable test behavior and prevents deterministic testing.

### Moderate Technical Debt
4. **Architectural violation persists** — CombatEngine line 23 calls `Console.ReadLine()` directly instead of routing through DisplayService. Breaks agreed "all I/O through DisplayService" contract.

5. **DisplayService tightly coupled to Console** — No interface for DisplayService. Blocks future GUI, web interface, or automated test runner.

6. **Command parsing doesn't scale** — Switch-based string matching will become unwieldy as commands expand. Consider command registration pattern or dictionary-based dispatch.

7. **Null handling fragile** — LootTable marked `= null!` but relies on constructor discipline. No compile-time safety for missed initialization.

### Quality Process Gaps
8. **Missing defensive null checks** — GameLoop constructor doesn't validate injected dependencies (DisplayService, ICombatEngine) beyond `null!` suppression. Should add `ArgumentNullException.ThrowIfNull()`.

9. **Equipment system incomplete** — Current implementation consumes items on equip with no unequip or slot limits. Feels unfinished, though acceptable for MVP.

10. **Level-up formula rigid** — XP calculation hardcoded (`player.XP / 100 + 1`). Tuning progression curve would require refactoring.

---

## Decisions

### D1: Test Infrastructure Required for v2
**What:** Before any v2 feature work begins, implement test infrastructure: unit test framework, injectable Random for determinism, test harness for combat/inventory/loot systems.
**Why:** Current code has zero test coverage. Regression risk blocks confident refactoring.
**Owner:** TBD (assign during v2 planning)

### D2: Player Encapsulation Refactor
**What:** Refactor Player model to use private setters with public methods (TakeDamage, Heal, ModifyAttack). Add validation to prevent invalid state.
**Why:** Public setters block save/load, multiplayer, and modding. Highest risk for future extensions.
**Owner:** Hill
**When:** Before v2 save/load or stat persistence work

### D3: DisplayService Interface Extraction
**What:** Extract IDisplayService interface from DisplayService class. Inject via constructor in all consumers (GameLoop, CombatEngine, InventoryManager).
**Why:** Enables headless testing, future GUI/web interfaces, automated test runners.
**Owner:** Hill (interface extraction), Barton (CombatEngine update)
**When:** Before v2 testing or alternative UI work

---

## Action Items

| Owner | Action | Priority |
|-------|--------|----------|
| Hill | Add defensive null checks in GameLoop constructor (`ArgumentNullException.ThrowIfNull`) | Immediate |
| Barton | Fix CombatEngine line 23 — replace `Console.ReadLine()` with DisplayService method | Immediate |
| Coulson | Create v2 planning ceremony agenda including test infrastructure, Player encapsulation, DisplayService interface extraction | Next |
| Romanoff | Document edge cases verified manually during WI-10 review as test cases for future automation | Next |
| Hill | Refactor Player model for encapsulation (private setters, validation, public methods) | Before v2 save/load work |
| Hill + Barton | Extract IDisplayService interface and inject via constructors | Before v2 testing work |
| Barton | Refactor CombatEngine and LootTable to accept injected Random instance | Before v2 testing work |

---

## Notes

### Risks Identified
- **Regression risk HIGH** — No automated tests means any refactoring or feature addition could break existing behavior with no safety net
- **State integrity risk** — Player model's public setters allow invalid state mutations
- **Testability blocked** — Tight coupling to Console and non-injectable Random prevent automated testing

### Disagreements
None. All participants aligned on priorities and action items.

### Positive Feedback
Team consensus that Design Review ceremony was the highest-value process decision. Contract-first approach eliminated integration friction and enabled confident parallel development.

### Team Morale
High. Game shipped clean after code review. Team understands technical debt but sees clear path to v2 quality improvements.

---

**Next Steps:**
1. Execute immediate action items (null checks, CombatEngine DisplayService fix)
2. Coulson schedules v2 planning ceremony
3. Romanoff documents test cases for future automation
4. Hill/Barton coordinate on interface extractions before v2 feature work begins
