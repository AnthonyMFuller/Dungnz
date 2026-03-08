# Retrospective Ceremony — 2026-03-08

**Requested by:** Boss (Anthony)  
**Run by:** Coulson (Lead)  
**Attendees:** Hill, Barton, Romanoff, Fury, Fitz, Coulson (all 6 squad members)  
**Date:** 2026-03-08

---

## Summary

Full-team retrospective review. Focus: what went well, pain points, and team priorities for next phase.

---

## What Went Well

- **Multi-project architecture proposal** (Coulson) — Comprehensive split plan with circular dependency analysis
- **P1 bug identification clarity** — Team consensus on four critical gameplay bugs that block feature work
- **Content scaling success** — Fury's narrative framework supports expanding room descriptions contextually
- **DevOps maturity** — CI/CD infrastructure stable enough to add smoke tests reliably

---

## Pain Points

- **ShowRoom state management** — Multiple command handlers patching individual cases (root cause: no unified reset hook)
- **SoulHarvest dual implementation** — Risk of double-healing when EventBus is wired; no integration tests to gate the work
- **Stale branch practices** — PRs #767, #771, #798 discarded or shipped empty due to branching from non-master
- **Coverage threshold instability** — Ad-hoc changes without tracking rationale

---

## Team Member #1 Recommendations

Each squad member pitched their top priority for future work:

| Member | Role | Recommendation | Rationale |
|--------|------|---|---|
| **Hill** | C# Dev | Room state persistence + backtracking | Enables save/load and player rewind within dungeon runs; supports immersion |
| **Barton** | Systems Dev / Display | Enemy AI behaviors via IEnemyAI | All enemy types currently use trivial attack patterns; AI brings combat depth |
| **Romanoff** | QA Engineer | SoulHarvest integration tests gate EventBus | Prevents double-healing bug; unblocks EventBus wiring without risk |
| **Fury** | Content Writer | Room state narration — context-aware descriptions | Complements Hill's persistence work; makes dungeon feel alive through flavor text |
| **Fitz** | DevOps | Release binary smoke test in CI | Catches runtime assembly resolution failures before release; low effort, high safety |
| **Coulson** | Lead | CombatEngine decomposition into focused components | 1,709-LOC monolith is high-risk during feature work; refactor after SoulHarvest tests land |

---

## Decisions Made

### D1: P1 Gameplay Bugs Are Sprint Gate
- SetBonusManager dead code
- Boss loot scaling
- HP clamping
- FinalFloor constant (magic number)

**Decision:** These four bugs must close before any new feature work begins. Hill owns all four fixes. Romanoff blocks feature PRs while P1 bugs are open.

### D2: ShowRoom Root Fix Required — No More Callsite Patches
**Decision:** Implement a root fix (CommandHandlerBase `finally` block or GameLoop post-command hook) that unconditionally restores room view. Hill designs, Barton implements, Romanoff validates. Stop patching individual command handlers.

### D3: SoulHarvest Integration Tests Gate EventBus
**Decision:** Romanoff writes `CombatEngine.SoulHarvestIntegration.Tests.cs` asserting SoulHarvest fires exactly once per kill, including dual-path regression test. EventBus wiring remains blocked until these tests pass. **No exceptions.**

### D4: CombatEngine Decomposition Proposal This Sprint
**Decision:** Coulson writes architecture proposal to extract AttackResolver, AbilityProcessor, StatusEffectApplicator, CombatLogger from CombatEngine.cs (1,709 LOC → ~5 focused components). Execution deferred to following sprint after SoulHarvest tests land.

### D5: Release Binary Smoke Test
**Decision:** Fitz adds smoke test step to `squad-release.yml`: pipe `quit` command into published binary, assert clean exit before `gh release create`. 10-line addition. Covers runtime assembly resolution risks introduced by multi-project split.

### D6: Room State Narration Content Pipeline
**Decision:** Fury produces room state narration pools (4-6 variants per state per floor theme: fresh, cleared, shrine, merchant, boss antechamber). Wire-up via `NarrationService.GetRoomEntryFlavor(room, floor)` in GoCommandHandler — assigned to Hill or Barton after content is ready.

### D7: Fury Early Consult on Player-Facing Text
**Decision:** When any feature includes player-facing strings, consult Fury before implementation — not during PR review. This prevents late-stage rewrites and keeps narration consistent with floor themes.

### D8: Feature Branches From Master Only
**Decision:** Hard rule, not a team norm. Every feature branch starts from master. No stacked branches. Stale-branch issues caused PR #798 to ship empty and PRs #767/#771 to be discarded. Romanoff should reject PRs branched from non-master.

### D9: Coverage Gate Restoration
**Decision:** Restore 80% line coverage threshold in CI. Any future threshold change requires a structured annotation: reason + tracking issue for restoration. Romanoff and Fitz co-own.

---

## Team Consensus on Phase N+1 Priorities

After individual recommendations were shared, the team identified three complementary features that all serve "make the dungeon feel real":

1. **Enemy AI behaviors** (Barton) — Implement IEnemyAI for all enemy types
2. **Room state persistence + backtracking** (Hill) — Save/load dungeon state, enable rewind
3. **Room state narration** (Fury + Hill/Barton) — Context-aware room descriptions, wire-up after content ready

These three can ship in the same phase and reinforce each other.

---

## Phase N+2 (Deferred)

- **CombatEngine decomposition execution** (Coulson) — After proposal + SoulHarvest tests land
- **EventBus wiring** (Team) — After decomposition + SoulHarvest gate satisfied

---

## Open Follow-Ups for Anthony

1. **Multi-project split scope:** Should architectural extraction (issues #1187–#1196) start this sprint or next?
2. **Barton's role:** Confirm whether Barton is permanent Display Specialist or still in trial period
3. **Skill tree content:** If ShowSkillTreeMenu is unblocked, does skill tree need content expansion?

---

## Notes

- All 6 squad members participated actively
- Strong alignment on P1 bug gate and process improvements (D8)
- Architecture proposal (Coulson) and narrative framework (Fury) position team for scaling
- Coverage gate restoration (D9) restores quality guardrails post-migration
