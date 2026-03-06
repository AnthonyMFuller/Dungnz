# Directive: Local master must always stay in sync with origin/master

**Source:** User (standing directive)  
**Date:** 2025-01-23

## Decision

After every PR merge going forward, the workflow **must** end with:

```bash
git checkout master && git pull origin master
```

Then verify `git log --oneline -1` matches `origin/master` before considering the task complete.

Work is not considered done until it has successfully reached `origin/master` and local `master` is checked out and in sync.

## Rationale

An unpublished local scribe branch (`scribe/log-tui-border-layout`) was discovered that had never been pushed or merged, raising concern that work was lost. While that branch turned out to be at the same commit as `origin/master`, the user wants a standing process guarantee.

## Scope

All team members. All PRs. Every session.
