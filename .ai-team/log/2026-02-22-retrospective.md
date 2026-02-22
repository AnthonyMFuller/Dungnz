# Retrospective — 2026-02-22
**Facilitator:** Coulson  
**Participants:** Coulson, Hill, Barton, Romanoff  
**Context:** Content expansion plan — all 4 phases complete

## What Went Well

**Architecture decisions paid off immediately:**
- ItemConfig validation pattern made the 40-item expansion mechanical and safe
- TruncateName prevented an entire class of display bugs upfront
- Accessory equip logic isolation meant combat changes didn't ripple into inventory
- Room-type color coding separation made the legend implementation trivial

**Test coverage caught issues before they shipped:**
- Romanoff's combat balance suite caught the Lich King imbalance (100% win rate at Lvl 12) before production
- 416 tests total (up from 359) across three new suites: loot distribution, UI regression, combat balance
- Balance fix was a two-line change because tests gave a precise failure signal

**Map UI/UX overhaul landed cleanly:**
- Fog of war, corridor connectors, room-type color coding, and legend all integrated without regressions
- Structural rendering decisions (separate color coding pass) made feature work straightforward

**Team velocity was strong:**
- 40 new items, 8 new enemies, 3 new test suites, and a full map overhaul across 4 phases
- Clean seams between phases meant work could be parallelized effectively

## What Could Be Improved

**Testing came too late in the pipeline:**
- Balance tests were Phase 3, but enemy content was Phase 2 — 8 enemies tuned blind before coverage existed
- ItemConfig hardening happened before test coverage, not test-first
- This is a recurring pattern: implementation before validation

**Systems specs should precede content specs:**
- Map UI overhaul happened in Phase 4, after enemies/items were finalized in Phase 2
- Barton spent time retrofitting corridor connectors around hardcoded room population logic
- Content should fill the box; it shouldn't define the box

**Process gaps enabled avoidable mistakes:**
- Two direct master commits occurred before pre-push hook enforcement
- Guardrails were added reactively, not proactively
- This was a setup problem, not a discipline problem

**Agent stalls were not escalated quickly enough:**
- gemini-3-pro-preview stalled twice with no immediate handoff
- Each stall burned time that could have been reallocated
- No defined policy on when to escalate vs. retry

**Data contracts arrived mid-implementation:**
- Hill received item/enemy specs during feature work, forcing retrofit validation
- Romanoff needed enemy stat sheets before phase handoff to write meaningful boundary assertions
- Late contracts create rework

## Action Items

| Owner | Action |
|-------|--------|
| Coulson | Define a balance budget upfront (damage ranges, HP tiers per zone) for future enemy content — tests assert specs, not discover them |
| Coulson | Establish stall policy: 20-minute diff timeout triggers immediate escalation and reassignment |
| Coulson | Enforce "systems spec before content spec" for future phases — lock map primitives, render layers, and combat stat ranges before content creation |
| Coulson | Add repo setup checklist: pre-push hooks, linting enforcement, branch protection — guardrails before agent work begins |
| Hill | Require schema validation at data load time for all future config (items, enemies, rooms) — validation is a precondition, not a hardening phase |
| Barton | Future map/UI changes must complete before content expansion phases — rendering primitives are dependencies, not retrofits |
| Romanoff | Establish policy: no enemy ships without at least a win-rate boundary test at target level range — balance coverage gates content |
| Romanoff | Flag agent stalls to test lead immediately for smoke test insertion — don't wait for phase completion to validate output |
| All | Data contracts (item specs, enemy stat sheets) must be delivered before implementation begins, not during — spec first, code second |

## Notes

**The Lich King fix is a success story, not just a bug:**
The balance test suite worked exactly as designed. The fix (HP 120→170, ATK 28→38) was clean and confident because the test gave precise failure criteria. This is the model for future balance work.

**Pre-push hook enforcement resolved the master commit issue:**
The hook now prevents direct master commits. This is working as intended. The lesson is timing: hooks should be in place before agent work begins, not after violations.

**gemini-3-pro-preview stalls led to Coulson model switch:**
After two stalls, Coulson was switched to claude-sonnet-4.5 for the remainder of the session. This worked, but the escalation was reactive. Future stalls should trigger immediate reassignment per the 20-minute policy above.

**Shift testing left:**
Unanimous feedback from all three implementers: tests must precede or accompany implementation, not follow it. Balance tests in particular should gate content addition, not validate it after the fact.

**Risk flagged — content-first sequencing:**
Phase 2 (content) before Phase 4 (map systems) created retrofit work. Future roadmaps should sequence primitives → content → polish, not content → systems → retrofit.

**Overall assessment:**
Strong execution velocity, clean architecture decisions, effective test coverage when present. Primary improvement area: sequencing. Tests, specs, and systems must precede implementation, not follow it.
