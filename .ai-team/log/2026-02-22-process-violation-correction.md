# 2026-02-22: Process Violation Correction

**Requested by:** Anthony

## Work Performed

Copilot (coordinator) had committed two hotfixes directly to `master` in a prior session, violating the team's PR workflow requirement. Anthony flagged this process violation and requested correction.

**Correction sequence:**
1. Created branch `hotfix/gameplay-command-fixes` from `origin/master`
2. Cherry-picked commit `e061ce9` (ShowTitle + listsaves fix) onto hotfix branch
3. Cherry-picked commit `db44870` (boss gate deadlock fix) onto hotfix branch
4. Reset local `master` to `origin/master`
5. Pushed hotfix branch to remote
6. Opened PR #228 for review

**Decision made:** All squad members, including the coordinator, must route changes through feature/hotfix branches and PRs—no direct commits to master.

## Outcome

Process violation corrected. Hotfixes now under review via PR #228 instead of merged directly to master.
