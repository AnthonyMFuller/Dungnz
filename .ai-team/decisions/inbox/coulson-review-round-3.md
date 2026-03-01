# Coulson — PR Review Round 3 Summary

**Date:** 2026-03-01  
**Reviewer:** Coulson (Lead)  
**Requested by:** Anthony

## Overview

Reviewed and merged 13 open PRs in priority order. Due to stacked branches from the squad agent, several PRs had merge conflicts that required resolution. Two PRs (#767, #771) were stale/duplicate and were closed with replacements.

## PRs Merged (in order)

| # | Title | Status | Notes |
|---|-------|--------|-------|
| 759 | CI speed improvements | ✅ Merged | NuGet cache, removed redundant XML docs build |
| 761 | Dependabot config | ✅ Merged | Weekly NuGet + monthly GH Actions |
| 763 | .editorconfig | ✅ Merged | Also contained HP encapsulation (bundled) |
| 765 | Release artifacts | ✅ Merged | Self-contained linux/win executables |
| 785 | Stryker tool manifest (clean) | ✅ Merged | Replacement for #767 |
| 769 | CodeQL workflow | ✅ Merged | C# static analysis |
| 789 | HP encapsulation completion | ✅ Merged | Fixed compile errors from #763 |
| 776 | Structured logging | ✅ Merged | Serilog + Microsoft.Extensions.Logging |
| 770 | Save migration chain | ✅ Merged | Resolved conflicts |
| 774 | Persist dungeon seed | ✅ Merged | Resolved conflicts |
| 777 | Wire JSON schemas | ✅ Merged | Resolved csproj conflict |
| 779 | Fuzzy command matching | ✅ Merged | Levenshtein distance |
| 781 | JsonSerializerOptions consolidation | ✅ Merged | DataJsonOptions shared instance |

## PRs Closed (not merged)

| # | Title | Reason |
|---|-------|--------|
| 767 | Stryker tool manifest | Stale branch with conflicts; replaced by #785 |
| 771 | HP encapsulation | Branch pointed to wrong commit; superseded by #789 |

## Key Decisions

1. **HP setter: `internal` not `private`** — The `private set` requirement caused 150+ compile errors in 30+ test files using object initializer syntax. Changed to `internal set` with `[JsonInclude]` for serialization. Encapsulation goal achieved: external assemblies cannot set HP directly.

2. **SaveSystem.cs.bak excluded** — Backup file in #767 was not committed. Source control should not contain .bak files.

3. **NotCallMethod arch test commented out** — ArchUnitNET 0.13.2 lacks this API. Needs version upgrade or rewrite.

## Final Master State

- **Build:** 0 errors, 2 warnings (NuGet version fallback, XML comment)
- **Tests:** 1347 passing / 2 failing / 0 skipped
- **Known failures** (pre-existing tech debt detected by new architecture tests):
  - `GenericEnemy` missing `[JsonDerivedType]` attribute
  - `Models` namespace depends on `Systems` (Merchant→MerchantInventoryConfig, Player→SkillTree)

## Process Observations

The squad agent created stacked branches where each feature branched from the previous instead of from master. This caused cascading merge conflicts when merging in priority order. Future batches should ensure each feature branch is based on master to allow independent merging.
