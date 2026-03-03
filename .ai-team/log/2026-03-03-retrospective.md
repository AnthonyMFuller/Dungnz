# Retrospective — 2026-03-03
**Facilitator:** Coulson
**Participants:** Coulson, Hill, Barton, Romanoff, Fury, Fitz
**Context:** Post-phase retrospective covering emoji/icon work, startup menu, inspect/compare, skill tree, help crash fix, test suite restoration

---

## What Went Well

### Test Suite & Quality
- **Romanoff:** 1420 tests green after inheriting 6 failures. Architecture tests now gate `JsonDerivedType` registration and layer violations on every CI run — the kind of silent save-corruption bug that would have hit production is now permanently blocked.
- **Fitz:** CI is green and *meaningful* — the 80% coverage threshold enforced in both CI and locally via `scripts/coverage.sh` keeps discipline without manual review.
- **Coulson:** The test infrastructure investment from earlier sprints paid dividends. We fixed 6 failures across architecture violations, JSON serialization gaps, and test isolation races without touching feature code.

### Architecture & Interface Design
- **Hill:** The `IDisplayService` / `SpectreDisplayService` separation is proven. Startup menu, skill tree, inspect/compare, and interactive inventory selection all shipped without touching test stub core behavior.
- **Barton:** The Skill Tree menu hooks into the existing `SkillTree` system without model changes — interface segregation paid off.
- **Coulson:** Contract-first design continues to prevent integration issues during parallel development.

### Display & Polish
- **Fury:** Emoji/icon standardization eliminated a silent immersion-killer. Misaligned characters broke the "polished dungeon crawler" feel before players read a single word.
- **Hill:** The EAW-compatible alignment work is fit-and-finish detail that makes a console game feel considered rather than hacked together.
- **Romanoff:** Alignment regression tests (`AlignmentRegressionTests`, `ShowEquipmentComparisonAlignmentTests`) strip ANSI codes and verify visual width — surgical precision over broad snapshots.

### Feature Delivery
- **Hill:** Startup flow cohesion — `StartupOrchestrator`, `StartupMenuOption`, `StartupResult` are clean, named types. Seed entry integrating with save selection was well-scoped.
- **Fury:** Inspect & Compare finally surfaces item descriptions to players. Content investment is getting its ROI.
- **Barton:** Save/startup integration is solid — non-trivial feature surface that shipped working.

### DevOps & Pipeline
- **Fitz:** Release pipeline auto-publishes self-contained binaries for `linux-x64` and `win-x64` with auto-generated release notes. Zero-friction shipping. Defence-in-depth security with CodeQL, RNG audit, JSON schema validation, and Dependabot.

---

## What Could Be Improved

### Code Structure
- **Hill:** `GameLoop.cs` is a 1,635-line God Class. Every new command gets bolted on as another private method. No structure — dungeon events, save/load, combat dispatch, shop logic all in one file. **Biggest maintenance liability.**
- **Hill:** `IDisplayService` violates Interface Segregation — 40+ methods, test stubs must implement everything even when testing one feature.
- **Barton:** `CombatEngine.cs` is 1200+ lines: damage calculation, ability dispatch, narration, XP, level-up, passive triggers, cooldown lifecycle, flee logic — all one file. Merge conflict factory.

### Incomplete Systems
- **Barton:** 5 affix properties are defined but completely unwired: `EnemyDefReduction`, `HolyDamageVsUndead`, `BlockChanceBonus`, `ReviveCooldownBonus`, `PeriodicDmgBonus`. Players can roll items with these affixes and get nothing. Silent gameplay bug.
- **Barton:** Class passives are scattered across three places (`PassiveEffectProcessor`, `SoulHarvestPassive`, `SkillTree`). `UndyingWill` TODO has been sitting since Phase 4 — Warrior class ships with a documented hole.
- **Barton:** Only 2 enemies out of 29+ have custom `IEnemyAI` implementations. Boss phases are inconsistent. Enemy count and behavior depth are out of proportion.

### Test Coverage Gaps
- **Romanoff:** `DisplayService.cs` at 39.6% line coverage — the display layer where alignment, markup, and emoji bugs live is less than 40% covered.
- **Romanoff:** `GameLoop.cs` at 50% coverage. Central command dispatch is half dark.
- **Romanoff:** The HELP crash fix has zero regression test. If someone touches that code path again, CI won't catch it.
- **Romanoff:** `ConsoleMenuNavigator.cs` at 0% coverage. Spectre.Console menu driver is completely untested.

### Content Gaps
- **Fury:** All 31 enemies are missing `Lore` field in data layer. One Bestiary or INSPECT-enemy feature away from shipping empty descriptions.
- **Fury:** Skill tooltips read like patch notes (`"+15% damage on attacks"`). Players see these in Skill Tree menu — they're reading a spreadsheet, not a game.
- **Fury:** "Dungeon Boss" is a placeholder name masquerading as a final encounter.
- **Fury:** Merchant has no personality — functionally anonymous despite `MerchantNarration.cs` existing.

### DevOps Gaps
- **Fitz:** Date-based versioning means if two PRs merge same day, second release is silently skipped. No error, no notice.
- **Fitz:** No macOS artifact in release pipeline.
- **Fitz:** Mutation testing threshold at 50% is too forgiving with 1420 tests and 80% line coverage.
- **Fitz:** Coverage results aren't surfaced on PRs — developers can't see which lines are uncovered without running locally.

### Data Quality
- **Hill:** `JsonDerivedType` discriminator casing is inconsistent (PascalCase vs lowercase). Latent save-file bug.
- **Hill:** `__TAKE_ALL__` sentinel string in `IDisplayService` is an implementation detail leaking into public interface.
- **Barton:** `PassiveEffectId` is a raw string — one typo wires to nothing with no diagnostic.

---

## Top Improvement Picks (per member)

| Member | Role | Their #1 Improvement Pick |
|--------|------|--------------------------|
| Coulson | Lead | **Extract command handlers from GameLoop** — introduce `ICommandHandler` pattern to decompose the 1,635-line God Class. Each command becomes independently testable. Unblocks architecture enforcement. |
| Hill | C# Dev | **Decompose GameLoop using command handler pattern** — `ICommandHandler` interface, extract `Handle*` methods into separate classes, register in `Dictionary<CommandType, ICommandHandler>`. Refactor, not rewrite. |
| Barton | Systems Dev | **Unify class passives into `IPassiveEffect` registry and close `UndyingWill` TODO** — define interface with `PassiveEffectId`, `Trigger` enum, `Apply(CombatContext)`. Replace raw strings. Close the Warrior class hole. |
| Romanoff | Tester | **Add Spectre markup rendering smoke test layer for DisplayService** — capture `AnsiConsole` output, assert no `MarkupException`, assert no unescaped brackets. Would have caught HELP crash. Lifts coverage from 39.6%. |
| Fury | Content Writer | **Add `Lore` fields to all 31 enemies in enemy-stats.json** — two-sentence entries that transform stat checks into story beats. Architecture trivial, impact immediate. |
| Fitz | DevOps | **Fix date-based versioning to support multiple releases per day** — append short commit SHA to tag: `v$(date +%Y.%m.%d)-$(git rev-parse --short HEAD)`. Every merge reliably produces a release. |

---

## Action Items

| Owner | Action | Priority |
|-------|--------|----------|
| Hill + Coulson | Create ticket: Decompose `GameLoop` into `ICommandHandler` implementations | P0 |
| Barton | Create `IPassiveEffect` interface and registry, close `UndyingWill` TODO | P0 |
| Romanoff | Add Spectre markup crash regression test for HELP command | P0 |
| Fury | Add `Lore` field to all 31 enemies in `enemy-stats.json` | P0 |
| Fitz | Fix `squad-release.yml` tag to include commit SHA suffix | P0 |
| Barton | Audit and wire/remove 5 unwired affix properties | P1 |
| Hill | Fix `JsonDerivedType` discriminator casing inconsistency, add round-trip test | P1 |
| Romanoff | Create `DisplayService` smoke test suite using `AnsiConsoleOutput` capture | P1 |
| Fitz | Add `osx-x64` publish step to release pipeline | P1 |
| Fitz | Raise Stryker `--threshold-break` from 50 to 65 | P1 |
| Romanoff | Add coverage floor assertion to CI (78% threshold with headroom) | P1 |
| Fury | Write player-facing tooltip strings for all skills in Skill Tree | P2 |
| Fury | Give Dungeon Boss a real name and dedicated lore | P2 |
| Hill | Create proper return type for `ShowTakeMenuAndSelect`, remove `__TAKE_ALL__` sentinel | P2 |
| Barton | Add `IEnemyAI` implementations for top-tier bosses (Infernal Dragon, Lich) | P2 |
| Fitz | Upload coverage XML as CI artifact, add HTML summary generation | P2 |
| Romanoff | Propose test file naming convention, migrate during normal churn | P2 |

---

## Notes

### Recurring Themes
1. **God Classes are the #1 technical debt** — both Hill and Barton independently identified their respective domains (`GameLoop.cs`, `CombatEngine.cs`) as unsustainable. The team consensus is clear: decomposition is overdue.

2. **Content exists but isn't surfaced** — Fury noted that descriptions, lore, and tooltips are written but invisible to players. The Inspect & Compare feature is a model for how to fix this.

3. **Test coverage gaps cluster in display/UI code** — Romanoff's analysis shows the hardest-to-test code (Spectre.Console integration) is also the least tested. The team needs a strategy for rendering tests.

4. **Incomplete systems create silent bugs** — unwired affixes, missing passives, placeholder boss names — features that *look* done but have gaps players will eventually find.

### Risks
- **P0 items are all foundational** — if we don't decompose GameLoop soon, every new command makes the problem worse.
- **The HELP crash pattern could recur** — any Spectre markup rendering bug will ship to players with current test coverage.
- **Date-versioning gap** means active sprint days could silently skip releases.

### Team Dynamics
- Hill and Coulson are aligned on command handler pattern as the path forward.
- Barton owns combat and passive systems and is ready to consolidate them.
- Romanoff has clear visibility into coverage gaps and practical solutions.
- Fury's content work is mature; the gap is surfacing it to players.
- Fitz has defense-in-depth DevOps but needs small fixes for reliability edge cases.

### Decision Candidates for Inbox
1. **Command Handler Pattern** — adopt `ICommandHandler` as standard pattern for new commands
2. **Passive Effect Registry** — standardize `IPassiveEffect` interface for all passive behaviors
3. **Display Smoke Tests** — require Spectre rendering coverage for display methods
