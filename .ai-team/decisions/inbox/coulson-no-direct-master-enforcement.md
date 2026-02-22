### 2026-02-22: Structural enforcement of no-direct-master rule
**By:** Coulson (Lead)
**What:** Added scripts/pre-push git hook that blocks all pushes to master. git config core.hooksPath=scripts activates it. Scribe charter updated to explicitly require branch+PR workflow for .ai-team/ commits.
**Why:** Direct commits to master occurred twice (commits 402fa5f, 41b7acf from Scribe). Process-only reminders failed. Hook enforces the rule mechanically — a push to master fails with an error regardless of agent instructions.
