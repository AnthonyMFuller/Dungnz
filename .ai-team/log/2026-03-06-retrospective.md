# Team Retrospective — 2026-03-06
**Facilitator:** Coulson  
**Participants:** Coulson, Hill, Romanoff  
**Context:** User-requested retro after extended bug-fix period. Also collecting feature suggestions.

---

## What Went Well

**The architectural foundations held up under pressure.**  
Hill's ICommandHandler decomposition reduced GameLoop from 1,635 lines to half that — every feature since (Shop, Sell, Ascend, Leaderboard) dropped into its own handler with zero friction. The IDisplayService abstraction meant the TUI integration required zero changes to GameLoop, CombatEngine, or command handlers. These were correct strategic decisions.

**Test infrastructure reached critical mass.**  
Romanoff built the test suite from zero to 1,674 tests covering combat, loot, inventory, commands, save/load, and more. The FakeDisplayService + ControlledRandom infrastructure makes probabilistic systems deterministic and testable. This is real testing discipline, not theater.

**Deep bug hunts caught production-breaking issues before ship.**  
Romanoff's display layer audits (8 bugs on 2/27, 18 bugs on 3/4) found the P0 InvalidOperationException crash affecting *every in-game menu*. Hill's menu input bug sprint (PRs #1129–#1142) was surgical: six issues, one PR, clean fixes. Pattern documentation (e.g., "no ShowRoom() on cancel path") enabled systematic fixes across 8+ handlers.

**UI/UX polish cycle delivered visible improvements.**  
Merchant menus (#1157–#1159), floor ascension (#1148–#1154), gear panel alignment, TUI layout compactness, message log caps, command panel min-width — these weren't glamorous but they're the difference between "prototype" and "playable."

---

## What Was Frustrating / Didn't Work

**The same structural bugs keep recurring.**  
"ShowRoom() not called on exit" has been caught in 8+ handlers: SellCommandHandler, ShopCommandHandler, inventory, use, compare, skills, take, equip, craft. This isn't bad luck — it means there's no enforcement mechanism. Tests don't catch it because FakeDisplayService doesn't verify state restoration. Code review keeps missing it because it's an omission, not a visible error.

**The display layer is a constant fire.**  
After Gear panel work: 18 display bugs in one PR. After menu input: more restore issues (#1141). After sell/shop: more cancel path failures (#1156–#1160). Every time a panel or menu is added, something else breaks. SpectreLayoutDisplayService is doing too much — input, layout, Live rendering, panel state — all tangled in 2,600+ LOC. It's heading toward the old GameLoop problem.

**P1 gameplay bugs remain open while we ship UI polish.**  
SetBonusManager 2-piece bonuses: `_ = totalDef` literally throws away the computed bonus (dead code). Boss loot scaling doesn't pass floor context. Enemy HP goes negative. These were documented in Coulson's post-TUI assessment. They're still unfixed. Hill is correct: this is a prioritization failure.

**The display layer is effectively untestable.**  
SpectreLayoutDisplayService is marked `[ExcludeFromCodeCoverage]`. Romanoff understands why (visual rendering isn't unit testable), but that's where half the bugs live. The P0 crash, content panel staleness, map not updating after combat — none catchable with current infrastructure. We're doing manual audits as a substitute for automated coverage.

**Test coverage doesn't match where bugs actually are.**  
Deep coverage on CombatEngine and LootTable (relatively stable). Nearly zero coverage on command handler cancel paths (actively broken). CommandHandlerSmokeTests.cs doesn't test cancel/abort flows at all. Romanoff documented this in March; gap still open.

---

## What Should Change Going Forward

### 1. **Gate new features on P1 gameplay bug fixes**  
Hill's recommendation: no new display/feature work until SetBonusManager, boss loot scaling, and HP clamping are fixed. This is correct. We can't keep shipping polish over a foundation with holes. **Action:** File a "P1 Gameplay Debt" milestone. All P1 gameplay bugs must close before next feature branch merges.

### 2. **Enforce ShowRoom() restoration at the architectural level**  
Two options:  
   (a) Make it part of CommandContext contract — every handler that calls SetContent() or opens a menu *must* call ShowRoom() on all exit paths. Enforce via interface or abstract base class.  
   (b) Add smoke tests: "after command X with cancel input → assert ShowRoomCalled == true"  

Current approach (social convention) has failed 8+ times. **Action:** Hill owns architectural enforcement design proposal by end of sprint.

### 3. **Add cancel-path tests to every command handler**  
Pattern is simple: `"inventory", then cancel input → assert `ShowRoomCalled == true`. These tests are quick once FakeDisplayService properly tracks call counts. Romanoff should not be manually auditing handlers and writing tables in history files. **Action:** Romanoff adds cancel-path test template to CommandHandlerSmokeTests.cs.

### 4. **Refactor budget for SpectreLayoutDisplayService**  
Not a full rewrite — extract panel-state management from input handling. Right now a menu cancel can corrupt panel state because both concerns live in the same 2,600-line class. A small structural split would stop the regression cycle. **Action:** Coulson drafts refactor proposal (Option A: split Input.cs from State.cs, Option B: introduce PanelStateManager).

### 5. **Stop marking the display layer as entirely untestable**  
At minimum, add integration-style smoke tests that exercise full command → display → state cycle in headless mode. The Spectre rendering can be excluded, but state transitions — content panel updated, stats refreshed, map updated — should be verifiable. **Action:** Romanoff + Hill pair on display integration test design (1 sprint, not blocking).

---

## Feature Suggestions (each member's top pick)

| Member | Role | Suggestion | Why |
|--------|------|-----------|-----|
| **Coulson** | Lead | **Combat encounter variety system** — Special enemy types with unique behaviors (summoners spawn adds, healers restore allies, berserkers rage at low HP). Current combat is "attack until dead" for every fight. This would make tactical decisions matter and give players reasons to care about ability loadouts. Doesn't require UI overhaul — can display via existing combat messages and status effects. Estimated: 3-4 days (new Enemy archetypes + behavior hooks in CombatEngine). | Most impact-per-LOC for making combat *interesting*, not just functional. Game is playable; making it compelling is next. |
| **Hill** | C# Dev | **Persistent dungeon state between floors** — Rooms don't reset when you ascend. Each floor's Room[] grid gets cached in GameState. When you return, rooms are in the state you left them (looted, cleared, fog-of-war preserved). Makes dungeon feel like a connected *place*, not a random seed each time. Rewards exploration (reason to clear every room before ascending). Foundation for future mechanics (merchant revisits, hidden rooms, lore fragments). Data model mostly supports it already (Room.Visited, Room.Looted, Room.Enemy). Main work: wire GameState to cache floor graphs by index and rehydrate on descent. | Makes the world persistent and exploration meaningful. Currently every floor transition feels like loading a new level. |
| **Romanoff** | Tester | **Persistent "what happened last turn" event log** — Log panel that accumulates combat events, item pickups, hazard damage, loot drops (scrollable, persistent for session). Currently content panel overwrites itself on every action — pickup messages disappear, hazard damage is gone before you can read it, combat messages get erased mid-fight. Players are losing information about what's happening to their character. BUG-13 (ShowCombatStatus wipes messages) and BUG-6 (ShowRoom spams log) are symptoms. Right now log panel just spams "Entered [room name]." Fix that — make it a real event history. | Single change that makes game feel complete. No new mechanic can matter if players can't observe the mechanics that exist. |

---

## Ceremony Notes

**Consensus on frustration:** All three members independently identified the display layer as the pain point. Hill: "constant fire." Romanoff: "effectively untestable." Coulson: architectural tangle. This is the team telling us the same thing three different ways.

**Disagreement on feature priority:** Hill wants persistence, Romanoff wants observability, Coulson wants combat depth. No single "right" answer — these are all valid next steps. Recommendation: implement in sequence. Romanoff's event log is shortest (1-2 days), fixes immediate UX gap. Hill's persistent floors is medium (3-4 days), unlocks exploration loop. Coulson's combat variety is longest (4-5 days), makes endgame engaging. Total: 8-11 days for all three if done sequentially.

**Process insight from Romanoff:** "Tests don't catch it because FakeDisplayService stubs the method unrealistically." This is a testing anti-pattern — the fake is too permissive. Real fix requires making FakeDisplayService enforce contracts (e.g., track call order, verify ShowRoom after SetContent). That's infrastructure work but pays off long-term.

**Hill's assessment is blunt but correct:** "We keep shipping UI polish over a foundation with holes." SetBonusManager dead code, boss loot broken, HP unclamped — these aren't theoretical. They're broken *now*. Hard gate on P1 gameplay bugs is the right call.

---

## Retrospective Meta-Commentary (Coulson)

This is the team's second retrospective (first was 2/20). The 2/20 retro identified test coverage as the gap; we closed that gap (0 → 1,674 tests). This retro identifies *structural enforcement* as the new gap — tests exist but don't catch the recurring patterns. That's progress: we're one layer deeper into the actual problem.

The fact that all three members independently flagged display layer pain means it's real, not just one person's frustration. The refactor budget needs to happen. Not "someday" — this sprint or next.

Feature suggestions are telling: Hill wants the world to matter, Romanoff wants players to understand what's happening, Coulson wants combat to be interesting. These aren't in conflict — they're three legs of the same stool. A dungeon crawler needs all three to be complete.

Final note: this ceremony format worked. Grounded, honest input from the people doing the work. No fluff. Will repeat quarterly.

---

**Next Actions (Summary):**
1. File "P1 Gameplay Debt" milestone — gate new features
2. Hill: architectural enforcement design for ShowRoom()
3. Romanoff: add cancel-path test template
4. Coulson: draft SpectreLayoutDisplayService refactor proposal
5. Romanoff + Hill: pair on display integration test design
6. Feature implementation: event log → persistent floors → combat variety (sequential, 8-11 days total)

**Ceremony complete.**
