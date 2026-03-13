# Squad Team

## Project Context

| Field | Value |
|-------|-------|
| **Project** | TextGame — C# Text-Based Dungeon Crawler |
| **Stack** | C#, .NET |
| **Description** | A text-based dungeon crawler game with rooms, enemies, combat, items, and player progression |
| **User** | Boss |
| **User Email** | — |
| **Created** | 2026-02-20 |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Coulson | Lead | .ai-team/agents/coulson/charter.md | ✅ Active |
| Hill | Core C# Developer | .ai-team/agents/hill/charter.md | ✅ Active |
| Barton | Display Specialist | .ai-team/agents/barton/charter.md | ✅ Active |
| Romanoff | QA Engineer | .ai-team/agents/romanoff/charter.md | ✅ Active |
| Fury | Content Writer | .ai-team/agents/fury/charter.md | ✅ Active |
| Fitz | DevOps | .ai-team/agents/fitz/charter.md | ✅ Active |
| Scribe | Session Logger | .ai-team/agents/scribe/charter.md | ✅ Active |
| Ralph | Work Monitor | — | 🚫 Inactive |

## Role Updates (2026-03-13)

Post-Phase 4 squad evolution — Coulson's analysis, executed by coordinator.

| Agent | Previous Role | New Role | Change |
|-------|--------------|----------|--------|
| Barton | Systems Dev / Display Specialist (Trial) | Display Specialist | Trial confirmed — permanent Display owner; combat/AI/systems migrate to Hill |
| Hill | C# Dev (P1 Gameplay Focus) | Core C# Developer | P1 constraint removed; scope expanded to engine, dungeon gen, game systems, seam extractions |
| Fury | Content Writer | Content Writer (Proactive Mode) | Added proactive content mandate — monthly Content Audit, self-generates improvement backlog |
| Scribe | Session Logger | — | Re-activated (2026-03-13) — inbox drain + history summarization are real jobs; session logging deprioritized |
| Ralph | Work Monitor | — | Deactivated — zero triggers in Phase 4; board clear = no backlog to monitor |

The following role changes are effective immediately as of the Design Review ceremony:

| Agent | Previous Role | New Role | Change |
|-------|--------------|----------|--------|
| Romanoff | Tester | QA Engineer | Promoted — full PR review mandate, can block merges, owns 80% coverage gate |
| Barton | Systems Dev | Systems Dev + Display Specialist (Trial) | 2-week trial owning `Display/` + all SpectreLayoutDisplayService bugs |
| Hill | C# Dev (general) | C# Dev (P1 Gameplay Focus) | Refocused on P1 bugs; explicitly removed from `Display/` ownership |
| Fury | Content Writer | Content Writer (Pipeline Active) | Content pipeline activated — recipes, items, enemies, narration |
| Fitz | DevOps | DevOps | Assigned to fix known `squad-release.yml` CI/CD issue |

### Process Change
All PRs now require Romanoff review before merge. Direct pushes to master are prohibited.
Flow: **Issue → Branch → PR → Romanoff review → merge**
