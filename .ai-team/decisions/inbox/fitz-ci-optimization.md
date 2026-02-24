# CI Optimization Implementation

**Agent:** Fitz (DevOps)  
**Date:** 2026-02-24  
**Status:** Implemented

## Decision

Implemented the approved GitHub Actions optimization plan to reduce CI usage by approximately 40%.

## Changes Made

### 1. Squad Heartbeat (Ralph) — Removed Cron Schedule
- **File:** `.github/workflows/squad-heartbeat.yml`
- **Change:** Removed `schedule: cron: '*/30 * * * *'` trigger
- **Rationale:** Ralph was polling every 30 minutes unnecessarily. Event-driven triggers (issues, PRs) and manual dispatch are sufficient.
- **Impact:** Saves ~48 workflow runs per day

### 2. CI Consolidation — Merged ci.yml into squad-ci.yml
- **Files:** `.github/workflows/squad-ci.yml` (enhanced), `.github/workflows/ci.yml` (deleted)
- **Changes:**
  - Added `push: branches: [main, master]` to squad-ci.yml
  - Added build step: `dotnet build --no-restore`
  - Added XML doc enforcement: `dotnet build Dungnz.csproj --no-restore`
  - Added coverage threshold to test step: `/p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Threshold=70 /p:ThresholdType=line`
  - Deleted redundant ci.yml
- **Rationale:** Two workflows doing similar work. Consolidation reduces duplication and maintenance burden.
- **Impact:** Eliminates duplicate runs on main/master pushes

### 3. Label Sync — Manual Only
- **File:** `.github/workflows/sync-squad-labels.yml`
- **Change:** Removed `push: paths: ['.ai-team/team.md']` trigger, kept only `workflow_dispatch`
- **Rationale:** Team roster changes are infrequent; manual sync is sufficient
- **Impact:** Eliminates automated runs on team.md changes

### 4. README Check — Local Pre-Push Hook
- **Files:** `scripts/pre-push` (enhanced), `.github/workflows/readme-check.yml` (deleted)
- **Changes:**
  - Added README check logic to existing pre-push hook (which already protected master branch)
  - Checks if Engine/, Systems/, Models/, Data/, or Program.cs changed without README.md update
  - Warns and blocks push if check fails (can override with --no-verify)
  - Deleted readme-check.yml workflow
- **Rationale:** Local hooks provide faster feedback and save CI minutes. Quality gate moves left in development cycle.
- **Impact:** Eliminates GitHub Actions runs for README checks on all PRs

## Technical Notes

- Pre-push hook is in `scripts/pre-push` and requires `git config core.hooksPath scripts` to activate
- Hook combines two checks: master branch protection (existing) + README enforcement (new)
- Coverage threshold set to 70% line coverage using Coverlet
- XML documentation enforcement uses CS1591 error treatment (already configured in .csproj)

## Verification

All changes tested locally. Workflows remain functional with reduced trigger frequency. Quality gates preserved.
