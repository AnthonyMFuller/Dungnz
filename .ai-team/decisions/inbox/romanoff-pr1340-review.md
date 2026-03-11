# Decision: PR #1340 Review — Branch Contamination Handling

**By:** Romanoff  
**Date:** 2026-03-11  
**Issues:** #1336 (bracket sweep), #1337 (content authoring spec)  
**PRs:** #1340 (closed — conflicting), #1345 (merged ✅)

---

## What Happened

PR #1340 (`squad/1336-bracket-markup-sweep`) was submitted as a docs-only PR covering:
- Fury's content authoring spec (`docs/content-authoring-spec.md`)
- Barton's markup bracket sweep results

The branch was contaminated by Hill's FinalFloor commit (`9c9ddce`) and a retrospective commit that had landed independently on master via separate PRs. Anthony pre-briefed that the contamination would "self-heal via squash merge."

## Finding: Self-Heal Did Not Apply

GitHub reported `mergeable: CONFLICTING` on PR #1340. The squash could not proceed.

**Why:** The contamination commit on the branch was an **older version** of Hill's FinalFloor refactor. Master had merged that work in a **newer form** (with full `GameConstants.FinalFloor` usage). The two versions diverged on the same lines in the same files, producing genuine merge conflicts — not just duplicate no-op additions.

The "self-heal" assumption only holds when the contamination commit is **byte-for-byte identical** on both sides. In this case it was not.

## Decision: Extract and Merge Clean

Rather than attempting to rebase the contaminated branch (too many conflict vectors across many files), Romanoff:

1. Identified the genuinely unique content in the branch:
   - `.ai-team/agents/barton/history.md` additions (markup sweep session)
   - `.ai-team/decisions/inbox/barton-markup-escape-complete.md` (new file)
   - `docs/content-authoring-spec.md` was already on master, branch's version had a typo (escaped backticks)

2. Created `squad/1340-clean-docs-sweep` from master HEAD

3. Applied only the unique content (Barton history + decision inbox file)

4. Fixed one spec inaccuracy found during review: Map panel height ~5 lines → ~8 lines (MapPanelHeight = 8 in LayoutConstants.cs)

5. Closed #1340, merged #1345

## Outcomes

- ✅ Bracket sweep finding documented in decision inbox
- ✅ Barton history updated with sweep session
- ✅ Content authoring spec Map panel height corrected
- ✅ Issues #1336 and #1337 both closed
- ✅ Build: clean
- ✅ Tests: 1909 passing, 0 failed

## Process Improvement

**Rule added:** When Anthony says "squash self-heals contamination," always verify with `gh pr view --json mergeable,mergeStateStatus` before attempting to merge. If CONFLICTING, extract unique content manually rather than trying to resolve the contaminated branch.
