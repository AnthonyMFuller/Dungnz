# Fitz — History

## Project Context

**Project:** TextGame — C# .NET Text-Based Dungeon Crawler  
**Owner:** Anthony  
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Fury (Content Writer), Fitz (DevOps)  
**Stack:** C#, .NET 6+  
**Current State:** 431 passing tests, core systems functional, CI/CD pipelines in place

## Key Milestones

- **2026-02-20:** Project inception, team established (Coulson, Hill, Barton, Romanoff)
- **2026-02-24:** ASCII art feature approved for implementation
- **2026-02-24:** Fury and Fitz added to roster; Phase 4 DevOps work begins

## Game Systems Overview

**Stack:**
- Language: C# (.NET 6+)
- Testing: xUnit or similar (TBD based on .csproj inspection)
- Build: dotnet CLI
- CI/CD: GitHub Actions workflows
- Platforms: Cross-platform console application

**Current CI/CD State:**
- GitHub Actions workflows in `.github/workflows/` directory
- Test suite: 431 passing tests (Romanoff's domain)
- Build automation exists but may have inconsistencies
- Known issue: `squad-release.yml` incorrectly uses Node.js test runner on .NET project

**Workflow Inventory:**
- Tests are run via GitHub Actions on pull requests and commits
- Release workflow exists but has a known bug (wrong test command)
- Build/validation workflows present but not yet audited by DevOps owner

## Phase 4: DevOps Work

**Priority Issue:**
1. Fix `squad-release.yml`: Replace `node --test test/*.test.js` with `dotnet test`

**Planned Work:**
- Audit and standardize all GitHub Actions workflows
- Ensure test infrastructure is aligned with team's test framework (xUnit, etc.)
- Optimize build times and cache strategies
- Coordinate with Romanoff on test infrastructure needs

## Learnings

### GitHub Actions Optimization (Feb 2026)

Implemented approved plan to reduce GitHub Actions usage by ~40%:

1. **Removed cron schedule from squad-heartbeat.yml** — Ralph no longer polls every 30 minutes, only triggers on issues/PR events and manual dispatch
2. **Consolidated ci.yml into squad-ci.yml** — Single CI workflow now runs on all relevant branches (dev, preview, main, master) with:
   - XML documentation enforcement (`dotnet build Dungnz.csproj --no-restore`)
   - Test coverage threshold (70% line coverage via Coverlet)
   - Deleted redundant ci.yml workflow
3. **Made sync-squad-labels.yml manual-only** — Removed push trigger for .ai-team/team.md changes, now workflow_dispatch only
4. **Converted readme-check.yml to local pre-push hook** — Moved README enforcement from GitHub Actions to local git hook in `scripts/pre-push`, integrated with existing master branch protection hook

**Key files changed:**
- `.github/workflows/squad-heartbeat.yml` — removed schedule trigger
- `.github/workflows/squad-ci.yml` — consolidated, enhanced with XML docs + coverage
- `.github/workflows/sync-squad-labels.yml` — workflow_dispatch only
- `.github/workflows/ci.yml` — deleted (consolidated into squad-ci.yml)
- `.github/workflows/readme-check.yml` — deleted (replaced by pre-push hook)
- `scripts/pre-push` — added README check logic to existing master protection hook

**Impact:** Reduces automated workflow runs while maintaining all quality gates. README check now runs locally, saving CI minutes and providing faster feedback.

_To be updated as work progresses._
