# Session: 2026-03-06 Command Panel Fixes

**Requested by:** Anthony (Copilot)

## Summary
Two Command panel bugs investigated and fixed: redundant HP/mana bars (#1095) and blocked keyboard input (#1096). Hill implemented fixes, PRs opened and merged to master (#1097).

## Fixes Applied
- Tests fixed: FakeDisplayService and TestDisplayService got ReadCommandInput() stubs
- GameLoop fallback to _input.ReadLine() when display returns null

## PRs Merged
- #1097 to master
