### 2026-02-20: CI Quality Gate Infrastructure Decision

**By:** Romanoff (Tester)

**What:** Established GitHub Actions CI pipeline with 70% code coverage threshold as mandatory quality gate. Fixed test project framework compatibility (net10.0 → net9.0).

**Why:** 
- **CI automation:** Prevents broken code from merging to master. Every push/PR now runs full build + test suite.
- **Coverage enforcement:** 70% line coverage threshold chosen as pragmatic starting point — high enough to catch untested logic, low enough to be achievable without blocking development velocity.
- **Framework fix:** Test project targeted net10.0 (doesn't exist), breaking CI runners. Downgraded to net9.0 to match main project and available SDKs.
- **Tooling:** Used existing Coverlet packages (already in test project), avoided adding new dependencies.

**Impact:**
- Blocks merges below 70% coverage — developers must write tests before shipping features.
- CI will catch compilation errors, test failures, and coverage regressions before code review.
- Foundation for future quality gates: mutation testing, static analysis, performance benchmarks.

**Decision Points:**
- Line coverage vs branch coverage: **Line** chosen for simplicity (branch coverage can be added later as second-tier gate).
- Threshold value: **70%** balances rigor vs pragmatism. Can be raised to 80-90% once baseline is stable.
- Workflow triggers: **Push to master + all PRs**. Does not run on draft PRs to save CI minutes.

**Files:**
- `.github/workflows/ci.yml` (new)
- `Dungnz.Tests/Dungnz.Tests.csproj` (framework fix)
