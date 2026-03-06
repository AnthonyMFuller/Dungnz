# Forward Planning Session — 2026-03-06
**Facilitator:** Coulson
**Participants:** Coulson, Hill, Barton, Romanoff, Fury, Fitz
**Context:** Game stable, 0 open issues, 1,734 tests passing. Planning next sprint.

---

## Member Recommendations

### Hill — C# Dev (P1 Gameplay Focus)
**Recommendation: Fix `ShowSkillTreeMenu` — it returns `null` unconditionally.**

The Spectre and TUI paths both have real implementations now, but the classic display service just prints a text list and bails — players literally cannot learn a single skill no matter how many skill points they've earned. It's a one-line prompt missing, the exact same `AnsiConsole.Prompt` pattern already proven elsewhere. This is Barton's file (`Display/`), not mine — but it's the highest-impact unblocked bug on the board right now, and it should take 20 minutes to close.

**File:** `Display/Spectre/SpectreLayoutDisplayService.Input.cs` (ShowSkillTreeMenu, ~line 430)
**Effort:** Low. **Player Impact:** High — entire skill progression system is inaccessible.

---

### Barton — Systems Dev / Display Specialist
**Recommendation: Wire `TuiColorMapper.cs` into `SpectreLayoutDisplayService.cs`.**

`ShowColoredMessage`, `ShowColoredCombatMessage`, and `ShowColoredStat` all call plain-text fallbacks while `TuiColorMapper` already has every mapping ready to go. Color is the primary sensory layer in a TUI dungeon crawler — combat hits, stat changes, and skill feedback are all communicated through text, and right now they're visually identical noise. Wiring in the mapper turns "you took damage / you dealt damage / your HP is low" into something a player can read at a glance under pressure. The infrastructure is already built; it's just not connected.

**Files:** `Display/Spectre/SpectreLayoutDisplayService.cs`, `Display/TuiColorMapper.cs`
**Effort:** Low-Medium. **Player Impact:** High — transforms monochrome TUI into visually expressive, scannable combat feedback.

---

### Romanoff — QA Engineer
**Recommendation: Write `CombatEngine.SoulHarvestIntegration.Tests.cs` — guard the SoulHarvest dual-implementation before EventBus gets wired.**

The `OnEnemyKilled` event exists but is unpublished — both SoulHarvest paths currently execute via direct calls. There are zero tests asserting that a Necromancer kill produces exactly `+5 HP` and `+2 MaxMana` — no more, no less. When EventBus gets wired (and it will), the bus subscription could fire alongside the direct call, producing a silent double-heal that corrupts combat balance invisibly. Players just assume they got lucky; boss tuning becomes meaningless. This test is cheap insurance that must land before any EventBus work touches the codebase.

**Files:** New `Dungnz.Tests/CombatEngine.SoulHarvestIntegration.Tests.cs`, targeting `CombatEngine.cs` lines 699–704 and 1360–1365.
**Effort:** Low (pure test writing). **Player Impact:** Medium preventative — blocks a silent difficulty regression.

---

### Fury — Content Writer
**Recommendation: Expand `RoomDescriptions.cs` late-floor pools — floors 6–8 have only 4 descriptions each.**

Players hitting the late game see the same room text every few steps, which kills immersion exactly when the stakes should feel highest. The fix: expand those thin pools to 16+ entries each, and add context-aware descriptions that react to what just happened in the room (enemy cleared, shrine visited, merchant left) — so the dungeon feels *alive* instead of copy-pasted. This costs zero engineering time, requires no system work, and directly fixes the moment-to-moment experience for any player who makes it past floor 5.

**Files:** `Systems/NarrationService.cs` (or wherever room description pools live), `Data/` content files.
**Effort:** Low (content writing). **Player Impact:** High for late-game players — single biggest immersion gain available at zero engineering cost.

---

### Fitz — DevOps
**Recommendation: Add a binary smoke test to `.github/workflows/squad-release.yml` before the GitHub Release step.**

We run 1,734 unit tests but never verify the actual binary boots and renders a frame. Right now `gh release create` can ship a binary that crashes on launch and CI won't notice. The fix: after `Publish linux-x64`, pipe `printf 'q\n'` into the published binary, assert clean exit, and gate the release on it. This matters to players because it's the only check between `dotnet publish` and a GitHub Release artifact landing in their hands — and it matters to team velocity because a bad merge to `main` can go live before anyone notices.

**File:** `.github/workflows/squad-release.yml`
**Effort:** Low-Medium. **Player Impact:** Medium (protective) — ensures no broken release reaches players.

---

## Prioritized Sprint Proposal

### P0 — Do Now

| # | Item | Owner | Effort | Player Impact | Unlocks |
|---|------|-------|--------|---------------|---------|
| 1 | **Fix `ShowSkillTreeMenu`** — stub out the ReadKey menu so players can actually learn skills | Barton | 2–4h / ~40 LOC | 🔴 HIGH | Skill progression playable for first time; unblocks skill balance work |
| 2 | **Wire `TuiColorMapper.cs`** — connect `ShowColoredMessage`, `ShowColoredCombatMessage`, `ShowColoredStat` to mapper instead of plain-text fallbacks | Barton | 3–5h / ~30 LOC call-site changes | 🔴 HIGH | Visual combat clarity; reveals broken color logic for further polish |
| 3 | **SoulHarvest regression tests** — `CombatEngine.SoulHarvestIntegration.Tests.cs` asserting single kill = single effect | Romanoff | 2–3h / ~60 LOC | 🟡 MEDIUM (preventative) | Clears the landmine before any EventBus wiring; safe to proceed to EventBus |
| 4 | **Expand room description pools, floors 6–8** — 16+ entries each + context-aware cleared/shrine/merchant variants | Fury | 3–5h (writing) | 🔴 HIGH (late-game) | Late-game immersion; demonstrates Fury content pipeline active |

**P0 Dependencies:**
- Items 1 and 2 are both Barton's — sequence to avoid conflicts in `SpectreLayoutDisplayService.Input.cs`. Recommend: skill tree first (smaller scope), then color wiring.
- Item 3 (Romanoff) is independent — can run in parallel with Barton's work.
- Item 4 (Fury) is fully independent — no code changes, run in parallel.

---

### P1 — Next Sprint

| # | Item | Owner | Effort | Notes |
|---|------|-------|--------|-------|
| 5 | **Smoke test in `squad-release.yml`** — gate GitHub Release on binary boot test | Fitz | 3–4h | Important but doesn't block gameplay improvements |
| 6 | **`FinalFloor` shared constant** — extract magic number duplicated in 4 command handlers into a named constant | Hill | 1h / ~10 LOC | Low risk, eliminates a class of drift bugs |
| 7 | **Boss DamageTaken tracking** — Reinforcements, TentacleBarrage, TidalSlam skip `DamageTaken` in `CombatEngine.cs` | Barton | 3–5h | Correctness issue; Romanoff adds regression test |
| 8 | **ShowRoom() CommandHandlerBase enforcement** — architectural fix (finally block or GameLoop post-command hook) so ShowRoom() is called on all exit paths structurally | Barton + Hill | 4–8h | Closes the recurring ShowRoom() pattern bug class permanently |

---

### P2 — Backlog

| # | Item | Owner | Notes |
|---|------|-------|-------|
| 9 | **CombatEngine decomposition** — 1,709-line god class → AttackResolver, AbilityProcessor, StatusEffectApplicator, CombatLogger. Coulson writes design proposal first. | Hill + Barton | High value long-term; low urgency now (stable). |
| 10 | **GameEventBus wiring** — remove duplicate event systems. Blocked on Item 3 (SoulHarvest tests) and Item 9 (decompose CombatEngine first for clean seam). | Barton | Cannot start until P0 item 3 and P2 item 9 are done. |
| 11 | **Dirty-flag rendering** — replace `RemoveAll()` + `new TextView()` with dirty-flag diffing to reduce GC pressure | Barton | P2 — only matters at high update frequency, not blocking. |
| 12 | **Color polish pass** — `BuildColoredHpBar` uses hardcoded `'█'` instead of computed `barChar`; fix dead code | Barton | Trivial fix, low urgency. |
| 13 | **Crafting recipe expansion** — add 15–20 new recipes to content files | Fury | Content-only, queue after room descriptions land. |
| 14 | **Unique/legendary item content** — expand thin item variety with 5–10 named uniques | Fury | Content-only. |
| 15 | **Integration tests for command handler cancel paths** — Use, Compare, Examine, Craft, Skills all have zero test files | Romanoff | High coverage value; defer until ShowRoom() architectural fix (P1 #8) is in place. |

---

## Coulson's Synthesis Note

The team is aligned on two themes this session: **restore broken player-facing features** and **prevent known landmines from detonating**. That's the right instinct.

**My architectural read:**

The skill tree returning `null` unconditionally is the single most embarrassing bug in the codebase right now. Players can discover the `skills` command, encounter the menu, and receive nothing — a feature that is clearly *intended* to exist is completely dead. That's P0 without debate, and it belongs to Barton who owns Display/ and the skills system. Color wiring is close behind: `TuiColorMapper.cs` represents someone's prior investment that was never connected. In a terminal game, color is the primary visual language — combat is incomprehensible noise without it.

Romanoff's SoulHarvest test call is the right instinct from a risk perspective. The EventBus is a loaded weapon — someone will wire it eventually, and without the regression tests in place, a double-heal will silently corrupt difficulty and nobody will catch it. Two hours of test writing now buys months of safety.

Fury's room description expansion is the lowest-friction, highest-immersion win available in the entire backlog. Zero engineering time. It should have been filed weeks ago. Fury's pipeline needs to stay active.

Fitz's smoke test is correct and important — but it protects the release pipeline, not the gameplay experience. It belongs in P1.

**The longer view:** CombatEngine decomposition (1,709 LOC) remains the biggest architectural liability on the board. It will become genuinely unmaintainable if we add more boss abilities or combat mechanics without decomposing it first. I'll write the design proposal this sprint so the team can execute it in P2 without blocking on architecture decisions.

**Sprint P0 assignment summary:**
- Barton: ShowSkillTreeMenu fix → TuiColorMapper wiring (sequence in this order)
- Romanoff: SoulHarvest tests (parallel, independent)
- Fury: Room descriptions, floors 6–8 (parallel, independent)
- Hill: FinalFloor constant (tiny, can absorb alongside review duties)
- Fitz: squad-release.yml smoke test (P1 — doesn't block gameplay)
