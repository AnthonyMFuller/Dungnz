# Decision: CI Improvements #876 #877 #878

**Date:** 2026-03-xx  
**Author:** Fitz  
**Branch:** squad/876-877-878-ci-improvements

## Stryker Threshold (affects: Romanoff, team)

Raised `--threshold-break` from 50 → 65 in `squad-stryker.yml`.  
Also raised `--threshold-low` from 65 → 75 to maintain proper separation.  

**Risk:** Cannot verify current mutation score without a live run (Stryker is schedule-only, ~30 min runtime). Confidence based on 1,422 tests + 80% line coverage.  
**Action if first Monday run fails:** Dial threshold back to 60 and file an issue for Romanoff to improve mutation coverage.

## Coverage Floor #878 (affects: whole team)

Issue #878 asked for a 78% coverage floor. The floor was **already present at 80%** (Anthony directive from prior session). No threshold change was made — 80% > 78% so it already satisfies the ask. Documented in squad-ci.yml comment.

## osx-x64 Release Artifact (affects: players, release process)

Added osx-x64 as a third publish target in squad-release.yml. Tested by inspection only — cross-compilation of self-contained executables is supported by .NET 10. Note: `PublishReadyToRun` may produce a warning for osx-x64 on ubuntu-latest runner (cross-OS R2R is limited); build won't fail but binary may not have R2R optimization.
