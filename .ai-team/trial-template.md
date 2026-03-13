# Agent Trial Template

Use this template when assigning an agent to a new role on a time-limited trial basis.

---

## Trial: [Agent Name] — [New Role]

| Field | Value |
|-------|-------|
| **Agent** | [Name] |
| **Trial Role** | [Role description] |
| **Duration** | [N weeks] |
| **Start Date** | [YYYY-MM-DD] |
| **End Date** | [YYYY-MM-DD] |
| **Evaluator** | Coulson |

## Trigger

[Why is this trial being run? What gap or opportunity prompted it?]

## Scope

[Exactly what files, domains, or responsibilities does this trial cover? Be specific — ambiguous scope leads to ambiguous verdicts.]

## Success Criteria

These must ALL be met for the trial to pass:

- [ ] [Quantitative metric 1] — e.g., "Close 8+ issues in the trial domain"
- [ ] [Quantitative metric 2] — e.g., "Zero regressions reported by Romanoff during trial period"
- [ ] [Quantitative metric 3] — e.g., "Deliver [specific artifact] by end of week 2"

## Disqualifying Conditions (automatic fail)

- [ ] [Condition that immediately fails the trial regardless of other metrics] — e.g., "Any P0 regression in trial domain that ships to master"

## Mid-Trial Checkpoint (Week [N/2])

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| [Metric 1] | [Target] | [TBD] | ⏳ |
| [Metric 2] | [Target] | [TBD] | ⏳ |

**Checkpoint verdict:** ⏳ On track / ⚠️ At risk / ❌ Failing

**Notes:** [Any adjustments needed? Scope changes? Issues with boundaries?]

## Final Evaluation

**Date:** [YYYY-MM-DD]

| Criterion | Met? | Evidence |
|-----------|------|----------|
| [Criterion 1] | ✅ / ❌ | [Link to issues, PRs, or observations] |
| [Criterion 2] | ✅ / ❌ | [Evidence] |
| [Criterion 3] | ✅ / ❌ | [Evidence] |

**Verdict:** ✅ CONFIRM / ⚠️ MODIFY / ❌ REVERT

**Rationale:** [1-2 sentences explaining the decision]

**If CONFIRM:** Update charter, routing.md, and team.md. Remove "Trial" from role name.  
**If MODIFY:** Specify exact scope changes. Update charter accordingly.  
**If REVERT:** Restore previous charter. Note what didn't work to inform future trials.
