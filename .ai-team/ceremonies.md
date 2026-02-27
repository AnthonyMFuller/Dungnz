# Ceremonies

## Design Review

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before |
| **Condition** | multi-agent task involving 2+ agents modifying shared systems or core architecture |
| **Facilitator** | lead |
| **Participants** | all-relevant |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**
1. Review the task and requirements
2. Agree on interfaces and contracts between components
3. Identify risks and edge cases
4. Assign action items

## Retrospective

| Field | Value |
|-------|-------|
| **Trigger** | manual |
| **When** | after |
| **Condition** | user requests retro |
| **Facilitator** | lead |
| **Participants** | all |
| **Time budget** | thorough |
| **Enabled** | ✅ yes |

**Agenda:**
1. What went well?
2. What could be improved?
3. Action items for next iteration

---

## Process Rules (Phase 8 Retro — adopted)

### P0 — Blocking

**1. Pre-merge CI must pass before squash merge.**
Never use `gh pr merge --squash` on a PR whose CI run has not completed successfully. Duplicate member errors (CS0102) and build failures are already caught by CI — enforce the gate. If CI is UNKNOWN/pending, wait.

**2. "Closes #N" must appear in both the PR body AND the squash commit subject.**
GitHub does not auto-close issues from a squash merge PR body. The issue reference must be in the commit message. Use format: `feat(...): description (#issue-closes-number)` and include `Closes #N` in the PR body. CI warns on PRs missing a closes reference.

**3. Session-level duplicate task detection.**
Before launching agents in a new session that continues prior work, check which tasks were already submitted in the prior session wave. Duplicate task ID → require explicit re-submit confirmation. Prevents double-run branches and competing PRs.

### P1 — Strong Guidance

**4. Publish merge sequence for high-touch files upfront.**
When phase planning shows 3+ PRs will touch the same file (e.g. `CombatEngine.cs`), record the merge order in the plan before agents start: A2 → D3 → C2 → B2 → B3.

**5. Design territory reservation.**
When an agent stubs forward-reference fields/methods for a downstream dependency, annotate with `// Phase N-XX pending` and note the owning track. Downstream agents check for existing stubs before declaring new members.

**6. Hard 15-minute agent runtime ceiling.**
Tasks expected to run >15 min must be pre-split into scoped subtasks. A stall at 15 min triggers escalation to Lead — do not restart the same task without re-scoping it.

### P2 — Process Hygiene (carry-forwards)

- **Narration Brief template** required for all content-touching issues (Fury owns)
- **Post-merge issue auto-close job** — belt-and-suspenders for squash edge cases
- **`content:fury` label automation** on PR open for narration-system file changes
