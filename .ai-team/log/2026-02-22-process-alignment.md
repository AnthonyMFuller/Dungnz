# 2026-02-22: Process Alignment Ceremony

**Facilitated by:** Coulson (Lead)  
**Participants:** Hill, Barton, Romanoff  
**Requested by:** Anthony  
**Trigger:** Repeated direct-to-master commits violating established PR workflow

---

## Background

Two violations occurred on 2026-02-22 after a prior violation had already been flagged:
- Commit `e061ce9` — fix: remove duplicate ShowTitle call and add listsaves command alias
- Commit `db44870` — fix: remove boss gate deadlock — players can now enter boss/exit room

Both were committed directly to `master`. Corrected via cherry-pick to `hotfix/gameplay-command-fixes` and master reset. PR #228 opened.

Anthony's directive: *"We need alignment on processes. Far too many violations of rules are happening."*

---

## Team Input Summary

### Hill (Engine Dev)
- Enable branch protection on GitHub — make violations mechanically impossible, not just prohibited
- Every change starts from a branch. Zero commits to master. Ever.
- Self-correction: stop immediately, cherry-pick to hotfix branch, flag Coulson before any master reset
- CI must pass before merge

### Barton (Systems Dev)
- Protect master at repo level — disable direct push for all contributors
- No self-merges without lead sign-off
- Self-correction: stop, identify SHAs, cherry-pick to branch, open PR, document root cause
- Document workflow in one canonical place

### Romanoff (QA)
- Branch protection is non-negotiable — the gap is enforcement, not documentation
- Pre-commit verification: confirm active branch is not master before any commit
- Violations must be logged in decisions.md in the same session they occur
- CI gates merges — a direct commit that slips through is a pipeline failure

---

## Consensus

All three agents agreed on the core protocol:
1. **Branch protection must be enabled at the repo level** — no direct pushes to master by anyone
2. **Every change, regardless of size, goes through a named branch and PR**
3. **CI must pass before merge is permitted**
4. **Self-correction is immediate: stop, cherry-pick, open PR, notify lead, log it**
5. **The coordinator (Squad/Copilot) is bound by the same rules as all agents**

---

## Outcome

Process alignment protocol written to `decisions/inbox/coulson-process-alignment-protocol.md`.  
All agent histories updated with protocol notice.
