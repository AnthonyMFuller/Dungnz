# Retrospective — 2026-03-11

**Facilitator:** Coulson  
**Participants:** Hill, Barton, Romanoff, Fury, Fitz  
**Requested by:** Anthony

---

## What Went Well

### 1. CombatEngine decomposition succeeded
Every participant acknowledged this — the split into `AttackResolver`, `AbilityProcessor`, `StatusEffectApplicator`, `CombatLogger` was clean and immediately useful. Barton had clear seams to cut along for display fixes. Romanoff can now mock individual components instead of standing up the 1700-line god class. Fury finally has a `CombatLogger` hook for per-enemy flavor. Architecture investment paying off.

### 2. Test suite held firm
1902 tests passing, zero failures leaked to master despite the display layer thrashing through multiple fix cycles. The `[Collection("console-output")]` discipline and `FakeDisplayService` pattern kept logic layers stable.

### 3. Display ownership model working
Barton's Display Specialist trial gave a single accountable owner. When Fitz sees CI failures in Display/, he knows who to coordinate with. The Stats → Gear panel reroute demonstrates the diagnosis process can work once we stop guessing and measure.

### 4. Pipeline baseline solid
Squad-CI, NuGet caching, smoke-test.yml all held. Zero CI regressions during the refactor cycles.

---

## What Could Be Improved

### 1. **Verification Gap — The Core Problem**
**Every participant flagged this.** Bugs were claimed "fixed" without runtime proof:
- Hill: "We discovered Spectre's rendering constraints at runtime, not in tests."
- Barton: "I fixed symptoms multiple times because I never asked 'what else hits this parser?'"
- Romanoff: "I reviewed PRs without demanding runtime evidence. I let Display become a test-free zone."
- Fury: "I found out about the Stats → Gear reroute secondhand, after it was fixed."
- Fitz: "My `dotnet test` step ran green while the game was visually broken."

The `[CHARGED]` crash was fixed multiple times across sessions — each time someone changed code without running the game. The Stats panel overflow was invisible for multiple sessions because no test asserts rendered line counts. **This is the highest-priority process failure.**

### 2. **Spectre Markup is a Category of Bug**
The `[CHARGED]` crash isn't one bug — it's a class. Any unescaped bracket in a user-facing string hitting Spectre's parser will crash. Barton: "After the first recurrence, it should have been a grep-and-sweep, not another point fix." No authoritative list of unsafe characters exists for content authors.

### 3. **Panel Height Constraints Undocumented**
The Stats panel holds ~8 rows. The render function generated 14-19 lines. Enemy stats were always below the fold. Nobody knew the constraint. Fury: "If I write a 6-line enemy lore block and it gets swallowed, I'm writing into a void."

### 4. **Content Authors Operating Blind**
Fury doesn't know which panel each content surface targets, what the line limits are, or which characters are unsafe. Display constraint knowledge lives in Barton's head and surfaces only when something breaks.

---

## Single Best Change (per member)

| Member | Recommendation |
|--------|----------------|
| **Hill** | Mandatory integration smoke test — headless `GameLoop` + `FakeDisplayService`, single floor traversal, assert no exception + assert all panel strings render without Spectre markup errors. Would have caught both Stats overflow and CHARGED crash. |
| **Barton** | Smoke test that boots game, enters combat, asserts rendered panel output fits within known terminal bounds. Capture Spectre output to string buffer, count lines per section, assert within height. |
| **Romanoff** | Adversarial markup smoke tests for every `ShowXxx` method — feed content with brackets/special chars, assert no exception. One `ShowCombatStatus_WithChargedStatusEffect_DoesNotThrow()` test would have permanently closed the CHARGED bug class. |
| **Fury** | Content Authoring Spec — one-page living doc: panel → line limit → char width → unsafe characters. Self-validation before handoff. Ends "display bugs silently eat my work." |
| **Fitz** | Headless integration scenario in smoke-test.yml — scripted input through combat, fail if process crashes or stdout contains stack traces. Catches crash class without new test infrastructure. |

**Pattern:** 4/5 participants independently called for some form of **integration/smoke test that exercises the actual rendering pipeline**, not just unit tests of logic. Consensus is strong.

---

## Action Items

| Owner | Action | Priority |
|-------|--------|----------|
| **Romanoff** | Write adversarial markup smoke tests for all `ShowXxx` display methods with bracket/special-char content | P0 |
| **Romanoff** | Gate: No display PR merges without a corresponding `_DoesNotThrow` test covering the changed rendering path | P0 |
| **Romanoff** | Add `RenderLineCount` assertion for `RenderCombatStatsPanel` — assert ≤8 lines | P0 |
| **Barton** | Grep all user-facing strings in Display/ for unescaped `[` patterns — produce fix list this session | P0 |
| **Barton** | Write `PanelHeightRegressionTests` class — parameterised by panel name, asserts rendered line count ≤ configured height | P1 |
| **Barton + Fury** | Produce Content Authoring Spec (limits, unsafe chars per surface) — one Markdown file in `docs/` | P1 |
| **Fitz** | Extend `smoke-test.yml` with scripted input sequence through combat, fail on crash patterns | P1 |
| **Fitz + Romanoff** | Define display-layer integration test that runs in CI without TTY | P1 |
| **Hill** | Centralize `FinalFloor` into `GameConstants.cs` | P1 |
| **Hill** | Hook up headless integration smoke test with `FakeDisplayService` (Romanoff designs) | P2 |
| **Coulson** | Centralize panel height constants into `LayoutConstants.cs` — single source of truth for tests and renderer | P1 |
| **Process** | "Fixed" is not done until CI is green AND a regression test exists. PRs closing display bugs must include new tests. | Immediate |
| **Process** | No Display/ PR merges without "verified in terminal" in PR description | Immediate |
| **Process** | Loop Fury into display constraint changes during the fix, not after | Immediate |

---

## Notes

### Unanimous Diagnosis
All five participants independently identified the same root cause: **verification happens in code, not at runtime**. This wasn't a dispute — it was convergence. The team understands the failure mode clearly.

### Consensus Recommendation
Four of five participants recommended some variant of "integration smoke test that exercises actual rendering." Hill, Barton, Romanoff, and Fitz all want rendered output tested, not just logic. This is the clear top priority.

### Fury's Structural Ask
Fury's request for a Content Authoring Spec is orthogonal but valid. Content authors shouldn't discover panel constraints by crash. A one-page spec with line limits and unsafe characters costs almost nothing to maintain and prevents an entire class of "content silently eaten" bugs.

### Panel Constants Centralization
Barton raised a direct question: should panel height constants be centralized into `LayoutConstants.cs`? **Answer: Yes.** The regression test and the renderer should share a single source of truth. Coulson will own this.

### Test Suite Paradox
1902 tests passed while the game was visually broken. The coverage gate (80%) didn't catch this because coverage measures code paths, not rendered output correctness. The new smoke tests are the missing layer.

---

*Ceremony complete. Decisions written to `.ai-team/decisions/inbox/coulson-retro-2026-03-11.md`.*
