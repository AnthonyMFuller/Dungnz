# Process Decisions — From 2026-02-27 Retrospective

**Source:** Full Team Retrospective  
**Facilitator:** Coulson  
**Status:** Pending review and merge into `decisions.md`

---

## 2026-02-27: Visual Rendering Checklist for Display PRs

**By:** Coulson (facilitated retro — proposed by Hill, Romanoff)  
**Priority:** P1

**What:**  
Any PR touching `ConsoleMenuNavigator`, `DisplayService`, class card rendering, or menu display code must include evidence of manual render testing. Add a checkbox to the PR template:

```markdown
### Display Changes
- [ ] Manually verified rendering in actual terminal (screenshot, recorded session, or explicit "tested by running: X scenario")
```

**Why:**  
The ANSI cursor-up off-by-one bug, Warrior icon inconsistency, Rogue indentation, and card border misalignment bugs all shipped because display output was code-reviewed but not visually verified. For display code, "compiles and tests pass" is insufficient. Visual regression is nearly impossible to catch in code review alone.

**Retrospective Evidence:**  
- Hill: "The cursor-up bug and the multi-round visual polish fixes both trace back to 'code looked right, output wasn't checked.'"
- Romanoff: "A PR that adds a feature shouldn't be able to introduce a regression that requires a separate bug-hunt iteration to find."

---

## 2026-02-27: Pre-Merge Test Checklist in PR Template

**By:** Coulson (facilitated retro — proposed by Romanoff)  
**Priority:** P1

**What:**  
Add a lightweight checklist to the PR template that authors confirm before marking a PR ready for review:

```markdown
### Pre-Merge Checklist
- [ ] All existing tests pass
- [ ] New tests cover the changed behavior
- [ ] Any UI change has been visually verified in the actual terminal (not just in unit test output)
```

**Why:**  
Multiple bugs were introduced in the same PRs that were supposed to implement features (arrow-key duplication, Ranger-looping class select, border misalignment). These were not caught because implementation PRs weren't tested against the full behavior surface before merge. The checklist shifts the mindset from "I think it works" to "I verified it works."

**Retrospective Evidence:**  
- Romanoff: "Our implementation PRs weren't tested against the full UI behavior surface before merge. A PR that adds a feature shouldn't be able to introduce a regression."
- Hill: "Display output was being reviewed by reading code, not by running the game and looking at it."

---

## 2026-02-27: No New Bugs in Fix PRs — Regression Test Requirement

**By:** Coulson (facilitated retro — proposed by Romanoff)  
**Priority:** P1

**What:**  
When a PR is filed to fix a bug, it must include a regression test that would have caught the original bug. If the test didn't exist before the fix, it has to exist in the fix PR. This is not optional — it's what "fixed" means.

**Why:**  
Bug fixes should be durable. A fix without a regression test is vulnerable to silent re-regression in future changes. This norm makes fixes permanent by encoding the expected behavior as a test that future PRs cannot break without explicit acknowledgment.

**Retrospective Evidence:**  
- Romanoff: "When a PR is filed to fix a bug, it should include a regression test that would have caught the original bug. This makes fixes durable."

---

## 2026-02-27: Bug Hunt Gate at End of Each Feature Phase

**By:** Coulson (facilitated retro — proposed by Romanoff)  
**Priority:** P1

**What:**  
After each major migration or feature phase, run a structured bug hunt before calling the phase complete. Don't wait for bugs to accumulate across multiple phases. A 30-minute structured hunt at the end of Phase N is worth less pain than a 3-agent deep dive after Phase 6.

**Why:**  
The deep bug hunt found 19 bugs in merged code. Finding bugs is impressive — but those bugs existed in production. A dedicated bug hunt after feature work is remediation, not prevention. Catching bugs earlier (at phase boundaries) reduces the backlog spike and prevents downstream compounding.

**Retrospective Evidence:**  
- Romanoff: "The deep bug hunt was reactive, not proactive. Finding 19 bugs in a structured hunt is impressive. But those 19 bugs existed in merged code."
- Ralph: "From a work-queue perspective, an unbounded hunt creates an unbounded backlog spike. 19 bugs is great to find. But the queue went from 0 to 21 issues instantaneously."

---

## 2026-02-27: Harden "Closes #N" CI Check to Hard Failure

**By:** Coulson (facilitated retro — proposed by Fitz, Romanoff)  
**Priority:** P0

**What:**  
Change the closes-reference CI step in `squad-ci.yml` from `::warning::` to `exit 1` on violation. If a PR body or commit subject is missing a `Closes #N` reference, the CI run must fail, not warn.

**Why:**  
P0-2 says "Closes #N" is required in both the PR body and the squash commit subject. The current CI step enforces this with a warning — which means PRs without it still merge. That's not enforcement, that's a suggestion. If this rule matters (and it does — it's what auto-closes issues and keeps the backlog clean), it needs to block PRs that violate it.

**Retrospective Evidence:**  
- Fitz: "P0-2 is a hard rule. A warning that doesn't block is not enforcement."
- Romanoff: "The 'Closes #N' rule (P0-2) is still applied inconsistently. If this rule matters, it needs to be checked before merge, not discovered after."

**Implementation Note:**  
All current squad agents already produce compliant PRs. This change is low-risk and closes a compliance gap.

---

## 2026-02-27: Scheduled Backlog Health Check in CI

**By:** Coulson (facilitated retro — proposed by Fitz, Ralph)  
**Priority:** P0

**What:**  
Add a cron trigger to `squad-heartbeat.yml` (e.g., `0 9 * * *` — daily at 09:00 UTC). When it fires, check:
- If there are more than 3 open squad PRs, OR
- If there are more than 8 open squad-labeled issues,

Then post a comment to the oldest open PR or open a summary issue tagged `go:blocked`.

**Why:**  
The 12-issue / 4-PR pile-up required the Boss to notice manually. That's a monitoring gap, not a process gap. A bloated backlog is a systemic signal that should surface automatically. The heartbeat workflow exists but is `workflow_dispatch` only — it requires Ralph to be explicitly triggered. A scheduled check makes backlog health visible without requiring manual intervention.

**Retrospective Evidence:**  
- Fitz: "The 12 open issues / 4 open PRs problem had no automated alert. A bloated backlog is a systemic signal that should surface automatically."
- Ralph: "The 12-issues/4-PRs pile-up was a queue failure. The directive that came out of it is correct, but it took the Boss calling it out explicitly rather than the process catching it automatically."

---

## 2026-02-27: Queue Cap Rule — Max 8 Open Issues Triggers Mandatory Pause

**By:** Coulson (facilitated retro — proposed by Ralph)  
**Priority:** P2

**What:**  
When the open issue count reaches 8, trigger a mandatory pause: no new issues filed, no new feature work starts, until the queue drops below 4. This is a companion to the existing "work not complete with open issues/PRs" directive, not a replacement.

**Why:**  
The 12-issue peak would have been stopped earlier at 8 if this rule existed. A hard cap prevents backlog accumulation from silently degrading team throughput. The queue should be a flowing stream, not a reservoir.

**Retrospective Evidence:**  
- Ralph: "Propose a hard cap of 8 open issues triggers a mandatory pause. The 12-issue peak would have tripped this at 8 and stopped the bleeding earlier."

---

## 2026-02-27: Duplicate Check Step Before Filing in Multi-Agent Bug Hunts

**By:** Coulson (facilitated retro — proposed by Ralph)  
**Priority:** P2

**What:**  
When two or more agents are filing bugs in parallel, the coordinator should require agents to search for existing issues before opening a new one. Before `gh issue create`, run:

```bash
gh issue list --search "{title keywords}" --state open
```

If a match exists, confirm whether to file a new issue or reference the existing one.

**Why:**  
During the bug hunt, 2 of 21 issues filed were duplicates. In a rapid multi-agent hunt, this is predictable. Duplicate issues create phantom backlog items during triage, inflate apparent backlog size, and create noise. A one-liner search before filing eliminates this with minimal friction.

**Retrospective Evidence:**  
- Ralph: "Duplicate issues happened because filing was fast and individual, not coordinated. Cost: seconds. Benefit: eliminates phantom backlog items."

---

## 2026-02-27: Board Check at Session Start is Mandatory

**By:** Coulson (facilitated retro — proposed by Ralph)  
**Priority:** P2

**What:**  
Every session starts with:

```bash
gh issue list --state open
gh pr list --state open
```

If the board is non-empty (any open issues or PRs), the first task is clearing it — not adding to it. This formalizes the existing "work not complete with open issues/PRs" directive as a visible step rather than an assumed check.

**Why:**  
The backlog pile-up history shows that the directive was missed in sessions where it wasn't explicitly checked. Making the board check a visible session-start ritual closes the gap where it was assumed but not performed.

**Retrospective Evidence:**  
- Ralph: "I'd like this formalized: every session starts with board checks before any new work is spawned. If the board is non-empty, the first task is clearing it. This is the directive already, but making it a visible step (rather than assumed) closes the gap where it was missed."

---

## 2026-02-27: Bug Hunt Scope Agreement Before Launch

**By:** Coulson (facilitated retro — proposed by Ralph)  
**Priority:** P2

**What:**  
Before the next deep bug hunt, the coordinator and Boss agree on:
- Which subsystems are in scope
- Approximate issue count ceiling (e.g., "file at most 25")
- Whether issues are filed-then-fixed in the same session or filed-first/fixed-next

**Why:**  
An unbounded bug hunt creates an unbounded backlog spike. The 19-bug hunt went from 0 to 21 issues instantaneously with no pre-negotiated ceiling. This doesn't constrain quality — it sets expectations so the work monitor can track against a known target and the team can plan merge sequences.

**Retrospective Evidence:**  
- Ralph: "From a work-queue perspective, an unbounded hunt creates an unbounded backlog spike. Prevents the unbounded queue spike. Doesn't constrain quality — just sets expectations."

---

*End of proposed decisions from 2026-02-27 retrospective*  
*Next step: Scribe reviews and merges into `decisions.md`*
