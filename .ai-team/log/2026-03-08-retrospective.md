# Retrospective — 2026-03-08

**Facilitator:** Coulson (Lead)
**Participants:** Coulson, Hill, Barton, Romanoff, Fury, Fitz
**Phase:** Architecture refactor / multi-project class library split
**Focus:** Future recommendations — what would most advance the game?

---

## What Went Well (Themes)

The multi-project split was universally praised. Every team member called it the right move — and for different reasons, which confirms it was genuinely cross-cutting:

- **Hill:** Architecture tests can now *enforce* project boundaries instead of relying on social convention. The ICommandHandler decomposition survived the split intact — proof the original granularity was correct.
- **Barton:** Systems can no longer accidentally couple to Display. The HP encapsulation work and IEnemyAI refactor from Sprint 3 both landed cleanly. The seam for per-enemy decision-making is real.
- **Romanoff:** The split made testing *possible* — IDisplayService extraction, Random injection, and proper DI gave us a harness. We went from zero tests to 1,734 passing. PR review gate held on every merge.
- **Fury:** `Dungnz.Systems` became a clear home for all narration work. Schema-first content process (enemy lore PR #887) was clean and repeatable.
- **Fitz:** Five commits, zero CI failures. NuGet caching, CodeQL integration, and `InternalsVisibleTo` wiring all survived the split without pipeline regressions.

**Summary:** The architecture refactor was the most successful structural change in the project's history. Build stayed green. Test count held. Boundaries are now enforced by the compiler, not convention.

---

## What Could Be Improved (Themes)

### P1 Gameplay Bugs Keep Slipping
Hill flagged it directly: SetBonusManager, loot scaling, HP clamping have been known for multiple sessions. We keep accepting feature work around them. Set bonuses are literally dead code — players equip a full set and get nothing. **Recommendation:** Block feature PRs when P1 gameplay bugs are open. Romanoff has the authority.

### ShowRoom() Root Fix Still Missing
Both Barton and Romanoff called this out. We documented 14+ bugs from the same root cause, agreed on a CommandHandlerBase or post-command hook fix in the design review, and still haven't built it. Individual callsites are being patched instead. The next command handler written without the base class will repeat the pattern.

### Coverage Gate Discipline
Fitz and Romanoff both flagged coverage: we set 80%, dropped to ~71%, and accommodated the drop instead of gating on it. The threshold shouldn't flex for feature adds. SpectreLayoutDisplayService remains the largest untested surface.

### Fury Gets Looped In Too Late
Fury needs early consult on player-facing text, not late PR review. Narration strings are still leaking into Engine (hazard arrays in GameLoop/GoCommandHandler). Rule should be enforced: all flavor text lives in `Dungnz.Systems`.

### Stale Branches Keep Burning Us
Barton raised it again: stacked branches caused PR #798 to ship empty, PRs #767 and #771 to be thrown away. Feature branches from master, no exceptions — agreed in Round 3, violated again in Round 4.

---

## Individual Recommendations — What Would Most Advance the Game?

### Hill (C# Dev — P1 Gameplay Focus): **Room State Persistence + Backtracking**

> Right now the dungeon is a one-way chute. You descend, you never go back. Rooms don't remember anything — clear a room, come back, the enemies are gone but there's no indication you were ever there.

Hill wants full room state persistence: `IsVisited`, `IsCleared` flags on Room, DungeonMap holding the full floor grid in memory wired to GameState, MovePlayer allowing backtracking via already-parsed directions, and the ASCII map rendering visited/cleared/unvisited rooms with distinct symbols. Enemies don't respawn, loot stays gone, shrines remember usage.

**Why it matters:** Once the P1 bugs are fixed (this sprint), the question becomes "does exploring the dungeon feel good?" Right now: no. You march forward. Backtracking lets players make decisions — go back to the shrine you skipped, check the room you rushed past. It transforms a corridor into a dungeon. Entirely within Hill's domain, no Display or Systems changes needed.

---

### Barton (Systems Dev / Display Specialist): **Enemy AI Behaviors — Give Enemies Actual Tactics**

> Combat against a Wraith feels identical to combat against a Goblin, which means the dungeon's visual variety doesn't translate into gameplay variety. That's the gap.

Barton wants to wire real IEnemyAI implementations into combat. The Troll regenerates unless Poisoned. The Vampire Lord heals on hit unless Weakened. The GoblinShaman opens with Poison Dart, heals at 40% HP. The DarkKnight opens with Fortify, goes aggressive when enraged. The Stone Golem has dodge bypassed by AoE.

**Why it matters:** The IEnemyAI interface exists. Status effects exist. The mana/ability system exists. The multi-project split means new AI files go in `Dungnz.Engine/` without touching CombatEngine's 1,700-line god class. It's additive, testable, and the difference between a stat check and a game where the player has to think. This is the first sprint where the architecture actually supports it.

---

### Romanoff (QA Engineer): **SoulHarvest Integration Tests — Unblock EventBus**

> The GameEventBus is designed and sitting idle. Everyone wants to wire it. The moment it's wired, SoulHarvest becomes a double-heal bug in production.

Romanoff wants to write `CombatEngine.SoulHarvestIntegration.Tests.cs` — tests asserting SoulHarvest fires exactly once per kill, including a test with EventBus wired alongside the inline logic to catch the dual-path regression explicitly.

**Why it matters:** The team agreed EventBus wiring is blocked until SoulHarvest tests pass. These tests are the critical path to one of the most significant architectural upgrades in the game. Every quest trigger, ambient event, and achievement hook that will eventually live on EventBus depends on this foundation being correct. This isn't just testing — it's building the gate that lets the next phase ship safely.

---

### Fury (Content Writer): **Room State Narration — Make Rooms Feel Alive**

> A player looks at the map, sees a shrine symbol, moves there, and gets generic room text. The visual and narrative layers are disconnected. That gap is the biggest remaining immersion hole in the game.

Fury wants room descriptions that reflect actual room state: fresh rooms with enemies get tension flavor, cleared rooms get aftermath flavor, shrine rooms get reverence flavor, merchant rooms get arrival flavor. 4–6 variants per state per floor theme. The `RoomStateNarration.cs` and `NarrationService.cs` plumbing exists — one call to `NarrationService.GetRoomEntryFlavor(room, floor)` in `GoCommandHandler`.

**Why it matters:** Room entry is the most frequent player interaction in the game — it fires on every move. Every other narration feature fires occasionally. Getting this right makes the whole game feel richer for essentially zero engineering cost (small wire-up, big payoff). Content pools can be written this sprint.

---

### Fitz (DevOps): **Release Binary Smoke Test**

> We could ship a release where `dotnet build` is green, all 1,734 tests pass, and the binary crashes on line 1 of Program.cs due to a runtime reflection issue with the new project boundaries.

Fitz wants a smoke test in `squad-release.yml`: pipe `echo "q"` into the published binary, assert clean exit before `gh release create`. 10 lines of YAML.

**Why it matters:** The multi-project split introduced runtime risks that unit tests don't cover — `EnemyTypeRegistry.CreateOptions()` loading, `SaveSystem` finding JSON options, the new assembly graph resolving at runtime. The TUI work added a second startup code path (`--tui`). Neither path is exercised end-to-end by the test suite. This is the cheapest insurance the project can buy. It gates the entire release on a real process startup.

---

### Coulson (Lead / Facilitator): **CombatEngine Decomposition**

My own pick: **decompose CombatEngine.cs from a 1,709-line god class into focused components.**

We've talked about this since the v2 architecture plan. `PerformEnemyTurn` is ~460 lines. `PerformPlayerAttack` is ~220 lines. SoulHarvest has a dual implementation because the class is too large for anyone to see the duplication. Boss phase abilities skip DamageTaken tracking because they're buried in method chains nobody can follow.

What it looks like: extract `AttackResolver` (damage calculation, crit, dodge), `AbilityProcessor` (player and enemy ability execution), `StatusEffectApplicator` (apply/tick/expire effects), and `CombatLogger` (all combat display calls). CombatEngine becomes an orchestrator — 200-300 lines calling focused services.

**Why it matters:** CombatEngine is where Barton's enemy AI behaviors need to plug in. It's where Romanoff's SoulHarvest tests need to assert. It's where Hill's boss loot scaling fix needs to land. Every recommendation in this room touches CombatEngine, and right now it's too large and tangled for any of those changes to land cleanly. Decomposing it is the force multiplier that makes everyone else's top priority easier.

I'll write the architecture proposal this sprint. Execution in the sprint after, once Romanoff's SoulHarvest tests are in place.

---

## Cross-Cutting Patterns

Looking across all six recommendations, I see three threads:

1. **"Make the dungeon feel real"** — Hill's backtracking, Fury's room state narration, and Barton's enemy AI all serve the same goal: the dungeon should feel like a place with memory, atmosphere, and danger — not a sequence of identical stat checks. These three features are complementary and could ship in the same phase.

2. **"Unblock the next architecture layer"** — Romanoff's SoulHarvest tests gate EventBus. My CombatEngine decomposition gates clean AI integration. Fitz's smoke test gates safe releases. These are infrastructure investments that make everything above possible.

3. **"Close the quality gap before adding features"** — The ShowRoom root fix, the coverage gate discipline, and the P1 gameplay bugs (SetBonusManager, loot scaling, HP clamping) are all debt that accrues interest every sprint. The team consensus is: fix these before the next feature phase.

---

## Action Items

| # | Action | Owner | Priority | Blocked By |
|---|--------|-------|----------|------------|
| 1 | Fix P1 gameplay bugs (SetBonusManager, loot scaling, HP clamping, FinalFloor constant) | Hill | P0 | — |
| 2 | Implement CommandHandlerBase or post-command hook for ShowRoom restoration | Hill + Barton | P0 | — |
| 3 | Write SoulHarvest integration tests | Romanoff | P0 | — |
| 4 | Add release binary smoke test to squad-release.yml | Fitz | P1 | — |
| 5 | Write CombatEngine decomposition proposal | Coulson | P1 | — |
| 6 | Expand room state narration content pools | Fury | P1 | — |
| 7 | Wire NarrationService.GetRoomEntryFlavor in GoCommandHandler | Hill or Barton | P1 | #6 |
| 8 | Implement IEnemyAI behaviors for all enemy types | Barton | P2 | #5 (decomposition proposal) |
| 9 | Implement room state persistence + backtracking | Hill | P2 | #1 |
| 10 | Restore 80% coverage gate | Romanoff + Fitz | P1 | #3 |

---

*Ceremony complete. Decisions written to `.ai-team/decisions/inbox/coulson-retro-2026-03-08.md`.*
