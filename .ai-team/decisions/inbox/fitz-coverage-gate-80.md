### 2026-02-28: CI coverage gate raised to 80%
**By:** Fitz
**What:** Updated squad-ci.yml to enforce minimum 80% line coverage. Previous threshold was 62%. Added `scripts/coverage.sh` for running coverage checks locally. Coverlet (coverlet.msbuild + coverlet.collector v8.0.0) was already present in Dungnz.Tests.csproj — no package changes required.
**Why:** Anthony directive — minimum 80% line coverage required at all times. PR #630 created.
