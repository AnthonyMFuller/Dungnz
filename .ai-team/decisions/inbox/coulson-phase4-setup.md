# Phase 4 Setup: Team Expansion & GitHub Issue Decomposition

**Date:** 2026-02-24  
**Lead:** Coulson  
**Status:** ✅ Complete

## Decision Summary

Added Fury (Content Writer) and Fitz (DevOps) to the team roster and created 16 GitHub issues for Phase 4 work across 4 categories: Narration, Items Cohesion, Gameplay Expansion, and Code Quality.

## Fury — Content Writer

### Rationale
Narration and flavor text represent a distinct domain requiring specialist focus. Separating content authorship from code implementation improves quality and reduces bottlenecks on Hill/Barton for feature integration.

### Charter Design
- **Ownership:** All narrative systems, flavor pools, story writing
- **Delegation:** C# system integration to Hill/Barton
- **Files:** Systems/NarrationService.cs and all narration-specific classes
- **Scope:** Room state narration, merchant banter, shrine descriptions, item flavor, floor transitions, enemy dialogue

### Key Principle
"Fury designs narrative content; Hill/Barton integrate it into the game loop."

## Fitz — DevOps

### Rationale
CI/CD pipeline and build infrastructure deserve dedicated ownership. GitHub Actions workflows, test runners, and build automation are critical infrastructure that impact the entire team's velocity.

### Charter Design
- **Ownership:** GitHub Actions workflows, build scripts, test infrastructure
- **Delegation:** Game code implementation to Hill/Barton, game tests to Romanoff
- **Files:** `.github/workflows/`, `scripts/`
- **Known Issue:** `squad-release.yml` currently runs `node --test` on .NET project (should be `dotnet test`)

### Key Principle
"Fitz ensures the build pipeline is fast, reliable, and automated."

## Phase 4: 16 GitHub Issues

### Decomposition Strategy

**Narration (A1-A5): Flavor Integration**
- #324: Room state tracking (foundational for all room narration)
- #325: Merchant banter (NPC flavor)
- #326: Shrine descriptions (location flavor)
- #327: Item interaction flavor (action flavor) — depends: #324
- #328: Floor transitions (transition ceremony flavor)

**Items Cohesion (B1-B6): Data & Mechanics**
- #329: Merchant inventory JSON (data-driven design)
- #330: Crafting recipes JSON (data-driven design) — depends: ItemId system (#338)
- #331: Expand loot pools from 62-item catalog (loot system health)
- #332: Tier-based floor drops (progressive challenge) — depends: #331
- #333: Accessory effects (unfinished mechanics)
- #334: Mana restoration logic (analyze & complete or remove)

**Gameplay Expansion (C1-C3): Content Scale**
- #335: Merchant-exclusive items (merchant value) — depends: #329
- #336: 10+ crafting recipes (player choice) — depends: #330
- #337: Enemy-specific drops (thematic loot) — depends: #331

**Code Quality (D1-D2): Foundations**
- #338: ItemId system (replaces fragile string.Contains())
- #339: Weight field standardization (uniform item properties)

### Issue Numbering
- A1-A5 (Narration): #324-#328
- B1-B6 (Items): #329-#334
- C1-C3 (Gameplay): #335-#337
- D1-D2 (Code Quality): #338-#339

**Total: 16 issues**

### Owner Assignments

**Fury:** #324 (lead), #325, #326, #327 (co-lead), #328, #335 (co-lead), #336 (co-lead), #337 (co-lead)  
**Hill:** #333, #334, #338, #339 + integration support for #324, #327  
**Barton:** #329 (co-lead), #330 (co-lead), #331 (lead), #332, #335 (co-lead), #336 (co-lead), #337 (co-lead)  
**Romanoff:** (None in Phase 4 decomposition; will own test coverage as features complete)

## Dependencies

- #330 depends on #338 (ItemId system must exist first)
- #332 depends on #331 (loot pools must be expanded first)
- #327 depends on #324 (room state narration is a prerequisite)
- #335 depends on #329 (merchant inventory JSON must exist)
- #336 depends on #330 (crafting recipes JSON must exist)
- #337 depends on #331 (expanded loot catalog must exist)

## Boundary Clarifications

### Fury's Boundaries
- ✅ Designs narrative content, flavor pools, storytelling
- ✅ Works with Hill/Barton on integration points
- ❌ Does NOT write C# game logic
- ❌ Does NOT write tests (Romanoff's domain)

### Fitz's Boundaries
- ✅ Owns GitHub Actions workflows, build scripts, CI/CD
- ✅ Coordinates with Romanoff on test infrastructure needs
- ❌ Does NOT own game code (Hill/Barton's domain)
- ❌ Does NOT write game tests (Romanoff's domain)

## Routing

Added to `.ai-team/routing.md`:
- **Fury (Narrative content, flavor text, story writing):** "Write merchant banter", "Design room descriptions", "Create item flavor text"
- **Fitz (CI/CD, build tooling, test infrastructure):** "Set up the build pipeline", "Fix the test workflow", "Optimize build time"

## Files Updated

1. `.ai-team/team.md` — Added Fury and Fitz to Members table
2. `.ai-team/routing.md` — Added routing entries for both agents
3. `.ai-team/casting/registry.json` — Registered both agents as active MCU members
4. `.ai-team/agents/fury/charter.md` — Created with role, responsibilities, boundaries
5. `.ai-team/agents/fury/history.md` — Created with project context
6. `.ai-team/agents/fitz/charter.md` — Created with role, responsibilities, known issues
7. `.ai-team/agents/fitz/history.md` — Created with project context
8. `.ai-team/agents/coulson/history.md` — Appended Phase 4 setup notes

## Next Actions

1. **Fitz (Priority):** Fix `squad-release.yml` workflow (node → dotnet test)
2. **Hill:** Start #338 (ItemId system) — blocks #330
3. **Fury:** Design narration pools for #324 (room state descriptions)
4. **Barton:** Design #331 (loot pool expansion) — blocks #332, #337
5. **Romanoff:** Prepare test coverage plan for Phase 4 features as they complete

## Approval

✅ Coulson (Lead) — 2026-02-24
